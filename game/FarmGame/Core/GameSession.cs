// =============================================================================
// GameSession.cs — Manages game persistence (player state, settings, reset)
// =============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FarmGame.Bootstrap;
using FarmGame.Entities;
using FarmGame.Persistence;
using FarmGame.Persistence.Models;
using FarmGame.Persistence.Repositories;
using FarmGame.World;
using Serilog;

namespace FarmGame.Core;

public class GameSession
{
    private PlayerStateRepository _playerStateRepo;
    private MapStateRepository _mapStateRepo;
    private SettingRepository _settings;
    private string _playerUuid;
    // Single source of truth: driven only by LoadPlayer() result
    public bool HasSavedState { get; private set; }

    public GameSession(DatabaseInitResult dbResult)
    {
        _playerStateRepo = dbResult.PlayerStateRepo;
        _mapStateRepo = dbResult.MapStateRepo;
        _settings = dbResult.Settings;
        _playerUuid = dbResult.PlayerUuid;
        HasSavedState = dbResult.SavedState != null;
    }

    public PlayerState LoadPlayer()
    {
        if (_playerStateRepo == null) return null;
        var result = _playerStateRepo.Load(_playerUuid);
        var state = result.Success ? result.Value : null;
        HasSavedState = state != null;
        return state;
    }

    // Current map state UUID, set when map entities are saved/loaded
    public string CurrentMapStateId { get; set; }

    public void SavePlayer(Player player, string currentMap)
    {
        if (player == null || _playerStateRepo == null) return;

        var state = new PlayerState
        {
            Uuid = _playerUuid,
            PositionX = player.GridPosition.X,
            PositionY = player.GridPosition.Y,
            FacingDirection = player.FacingDirection.ToString(),
            CurrentMap = currentMap,
            CurrentMapStateId = CurrentMapStateId,
        };

        var result = _playerStateRepo.Save(_playerUuid, state, GameConstants.GameTitle);
        if (result.Success)
            Log.Information("Player state saved: pos=({X},{Y}), dir={Dir}, mapState={MapState}",
                state.PositionX, state.PositionY, state.FacingDirection, CurrentMapStateId ?? "null");
        else
            Log.Error("Failed to save player state: {Error}", result.ErrorMessage);
    }

    /// <summary>
    /// Load or create map entity state. If CurrentMapStateId is null (first visit),
    /// creates a new map_state + entity records. Otherwise restores entity HP from DB.
    /// </summary>
    public void LoadOrCreateMapState(string mapId, GameMap map, PlayerState savedState)
    {
        if (_mapStateRepo == null) return;

        var mapStateId = savedState?.CurrentMapStateId;
        Log.Debug("LoadOrCreateMapState: mapId={MapId}, savedMapStateId={StateId}",
            mapId, mapStateId ?? "null");

        if (mapStateId != null)
        {
            // Restore object states from DB
            var loadResult = _mapStateRepo.LoadObjects(mapStateId);
            Log.Debug("LoadObjects result: success={Success}, count={Count}",
                loadResult.Success, loadResult.Success ? loadResult.Value.Count : 0);
            if (loadResult.Success && loadResult.Value.Count > 0)
            {
                // Match saved objects to map objects by item_id + creation order
                var savedByItem = new Dictionary<string, List<MapObjectRecord>>();
                foreach (var r in loadResult.Value)
                {
                    if (!savedByItem.ContainsKey(r.ItemId))
                        savedByItem[r.ItemId] = new List<MapObjectRecord>();
                    savedByItem[r.ItemId].Add(r);
                }

                var usedByItem = new Dictionary<string, int>();
                foreach (var obj in map.Objects)
                {
                    if (!savedByItem.TryGetValue(obj.ItemId, out var records))
                        continue;
                    if (!usedByItem.ContainsKey(obj.ItemId))
                        usedByItem[obj.ItemId] = 0;
                    int idx = usedByItem[obj.ItemId];
                    if (idx >= records.Count) continue;

                    var record = records[idx];
                    usedByItem[obj.ItemId] = idx + 1;

                    obj.InstanceId = record.Id;
                    obj.RestoreState(record.Hp);

                    // Restore saved position (may differ from YAML due to knockback)
                    if (obj.TileX != record.TileX || obj.TileY != record.TileY)
                        map.MoveObject(obj, record.TileX, record.TileY);
                }

                CurrentMapStateId = mapStateId;
                Log.Information("Map state restored: {MapId}, stateId={StateId}, objects={Count}",
                    mapId, mapStateId, loadResult.Value.Count);
                return;
            }
        }

        // First visit: create new map state
        var createResult = _mapStateRepo.CreateMapState(mapId);
        if (createResult.Success)
        {
            CurrentMapStateId = createResult.Value;

            // Assign instance IDs to all objects
            foreach (var obj in map.Objects)
                obj.InstanceId = Guid.NewGuid().ToString();

            // Persist initial object states
            SaveMapObjects(map);
            Log.Information("Map state created: {MapId}, stateId={StateId}", mapId, CurrentMapStateId);
        }
    }

    /// <summary>
    /// Persist current object states to the map_object table.
    /// </summary>
    public void SaveMapObjects(GameMap map)
    {
        if (_mapStateRepo == null || CurrentMapStateId == null) return;

        var records = map.Objects.Select(o => new MapObjectRecord
        {
            Id = o.InstanceId ?? Guid.NewGuid().ToString(),
            ItemId = o.ItemId,
            Category = o.Category.ToString().ToLowerInvariant(),
            TileX = o.TileX,
            TileY = o.TileY,
            Hp = o.State.CurrentHp,
            StateJson = "{}",
        }).ToList();

        var result = _mapStateRepo.SaveObjects(CurrentMapStateId, records);
        if (result.Success)
            Log.Debug("Map entities saved: stateId={StateId}, count={Count}",
                CurrentMapStateId, records.Count);
        else
            Log.Error("Failed to save map entities: {Error}", result.ErrorMessage);
    }

    public void SaveSetting(string key, string value)
    {
        _settings?.Set(key, value);
    }

    public string GetSetting(string key, string defaultValue = null)
    {
        return _settings?.Get(key, defaultValue) ?? defaultValue;
    }

    public void ChangeLanguage(string language, string contentDir)
    {
        LocaleManager.Load(contentDir, language);
        SaveSetting("language", language);
        Log.Information("Language changed to: {Language}", language);
    }

    public void DeleteAndReset()
    {
        var dbPath = DatabasePathResolver.GetDatabasePath(GameConstants.GameTitle);
        var currentLanguage = LocaleManager.CurrentLanguage;

        DatabaseBackup.Backup(dbPath);
        if (File.Exists(dbPath))
            File.Delete(dbPath);

        var freshDb = DatabaseInitializer.Run();
        if (freshDb.Success)
        {
            _playerStateRepo = freshDb.PlayerStateRepo;
            _mapStateRepo = freshDb.MapStateRepo;
            _settings = freshDb.Settings;
            _playerUuid = freshDb.PlayerUuid;
            _settings.Set("language", currentLanguage);
        }
        else
        {
            _playerStateRepo = null;
            _mapStateRepo = null;
            _settings = null;
        }

        CurrentMapStateId = null;

        HasSavedState = false;
        Log.Information("Character deleted, database re-initialized");
    }
}
