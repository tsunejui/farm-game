// =============================================================================
// GameSession.cs — Manages game persistence (player state, settings, reset)
// =============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FarmGame.Entities;
using FarmGame.Persistence;
using FarmGame.Models;
using FarmGame.Persistence.Entities;
using FarmGame.World;
using FarmGame.World.Effects;
using Serilog;

namespace FarmGame.Core;

public class GameSession
{
    private DatabaseManager _db;
    private string _playerUuid;
    // Single source of truth: driven only by LoadPlayer() result
    public bool HasSavedState { get; private set; }

    public GameSession(DatabaseManager db, string playerUuid, PlayerState savedState)
    {
        _db = db;
        _playerUuid = playerUuid;
        HasSavedState = savedState != null;
    }

    public PlayerState LoadPlayer()
    {
        if (_db == null) return null;
        var result = _db.LoadPlayerState(_playerUuid);
        var state = result.Success ? result.Value : null;
        HasSavedState = state != null;
        return state;
    }

    // Current map state UUID, set when map entities are saved/loaded
    public string CurrentMapStateId { get; set; }

    public void SavePlayer(Player player, string currentMap)
    {
        if (player == null || _db == null) return;

        var state = new PlayerState
        {
            Uuid = _playerUuid,
            PositionX = player.GridPosition.X,
            PositionY = player.GridPosition.Y,
            FacingDirection = player.FacingDirection.ToString(),
            CurrentMap = currentMap,
            CurrentMapStateId = CurrentMapStateId,
            MaxHp = player.MaxHp,
            CurrentHp = player.CurrentHp,
            Strength = player.Strength,
            Dexterity = player.Dexterity,
            WeaponAtk = player.WeaponAtk,
            BuffPercent = player.BuffPercent,
            CritRate = player.CritRate,
            CritDamage = player.CritDamage,
        };

        var result = _db.SavePlayerState(_playerUuid, state, GameConstants.GameTitle);
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
        if (_db == null) return;

        var mapStateId = savedState?.CurrentMapStateId;
        Log.Debug("LoadOrCreateMapState: mapId={MapId}, savedMapStateId={StateId}",
            mapId, mapStateId ?? "null");

        // If no saved map state ID, try to find an existing one by map_id (handles TTL checks)
        if (mapStateId == null)
        {
            var findResult = _db.FindMapStateByMapId(mapId);
            if (findResult.Success)
            {
                mapStateId = findResult.Value.Id;
                Log.Debug("Found existing map state via FindByMapId: {StateId}", mapStateId);
            }
        }

        if (mapStateId != null)
        {
            // Restore object states from DB
            var loadResult = _db.LoadMapObjects(mapStateId);
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

                    // Restore effects from DB
                    RestoreObjectEffects(obj);

                    // Restore saved position (may differ from YAML due to knockback)
                    if (obj.TileX != record.TileX || obj.TileY != record.TileY)
                        map.MoveObject(obj, record.TileX, record.TileY);
                }

                CurrentMapStateId = mapStateId;

                // Reset TTL to 0 — player is now on this map (active)
                _db.SetMapStateTtl(mapStateId, 0);

                Log.Information("Map state restored: {MapId}, stateId={StateId}, objects={Count}",
                    mapId, mapStateId, loadResult.Value.Count);
                return;
            }
        }

        // First visit or expired state: create new map state
        var createResult = _db.CreateMapState(mapId);
        if (createResult.Success)
        {
            CurrentMapStateId = createResult.Value;

            // Assign instance IDs and apply default effects
            foreach (var obj in map.Objects)
            {
                obj.InstanceId = Guid.NewGuid().ToString();
                ApplyDefaultEffects(obj);
            }

            // Persist initial object states + effects
            SaveMapObjects(map);
            Log.Information("Map state created: {MapId}, stateId={StateId}", mapId, CurrentMapStateId);
        }
    }

    /// <summary>
    /// Persist current object states to the map_object table.
    /// </summary>
    public void SaveMapObjects(GameMap map)
    {
        if (_db == null || CurrentMapStateId == null) return;

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

        var result = _db.SaveMapObjects(CurrentMapStateId, records);
        if (result.Success)
        {
            Log.Debug("Map objects saved: stateId={StateId}, count={Count}",
                CurrentMapStateId, records.Count);

            // Save effects for each object
            foreach (var obj in map.Objects)
                SaveObjectEffects(obj);
        }
        else
            Log.Error("Failed to save map objects: {Error}", result.ErrorMessage);
    }

    /// <summary>
    /// Mark the current map state as expiring in 5 minutes (player is leaving this map).
    /// </summary>
    public void SetMapStateTtlOnLeave()
    {
        if (_db == null || CurrentMapStateId == null) return;

        long ttlUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 300; // 5 minutes
        var result = _db.SetMapStateTtl(CurrentMapStateId, ttlUtc);
        if (result.Success)
            Log.Information("Map state TTL set: stateId={StateId}, expiresAt={TtlUtc}",
                CurrentMapStateId, ttlUtc);
        else
            Log.Error("Failed to set map state TTL: {Error}", result.ErrorMessage);
    }

    private void ApplyDefaultEffects(WorldObject obj)
    {
        foreach (var defEffect in obj.Definition.Logic.DefaultEffects)
        {
            var effect = EffectRegistry.Get(defEffect.EffectId);
            if (effect == null)
            {
                Log.Warning("Unknown default effect '{EffectId}' on '{ItemId}'",
                    defEffect.EffectId, obj.ItemId);
                continue;
            }
            obj.AddEffect(new ActiveEffect(effect, defEffect.Ttl));
        }
    }

    private void RestoreObjectEffects(WorldObject obj)
    {
        if (_db == null || string.IsNullOrEmpty(obj.InstanceId)) return;

        var loadResult = _db.LoadObjectEffects(obj.InstanceId);
        if (!loadResult.Success) return;

        foreach (var record in loadResult.Value)
        {
            var effect = EffectRegistry.Get(record.EffectId);
            if (effect == null) continue;

            DateTime appliedAt = DateTime.TryParse(record.UpdatedAt, out var dt)
                ? dt : DateTime.UtcNow;

            var ae = ActiveEffect.FromPersisted(effect, record.Ttl, appliedAt);
            if (!ae.IsExpired)
                obj.AddEffect(ae);
        }
    }

    private void SaveObjectEffects(WorldObject obj)
    {
        if (_db == null || string.IsNullOrEmpty(obj.InstanceId)) return;

        var effectRecords = obj.Effects
            .Where(ae => !ae.IsExpired)
            .Select(ae => new ObjectEffectRecord
            {
                EffectId = ae.EffectId,
                Ttl = ae.TtlSeconds,
            }).ToList();

        _db.SaveObjectEffects(obj.InstanceId, effectRecords);
    }

    public void SaveSetting(string key, string value)
    {
        _db?.SetSetting(key, value);
    }

    public string GetSetting(string key, string defaultValue = null)
    {
        return _db?.GetSetting(key, defaultValue) ?? defaultValue;
    }

    public void ChangeLanguage(string language, string contentDir)
    {
        LocaleManager.Load(contentDir, language);
        SaveSetting("language", language);
        Log.Information("Language changed to: {Language}", language);
    }

    public void DeleteAndReset()
    {
        var dbPath = _db.DatabasePath;
        var currentLanguage = LocaleManager.CurrentLanguage;

        _db.Backup();
        if (File.Exists(dbPath))
            File.Delete(dbPath);

        var freshDb = new DatabaseManager(GameConstants.GameTitle);
        var initResult = freshDb.Initialize();
        if (initResult.Success)
        {
            _db = freshDb;
            _playerUuid = Guid.NewGuid().ToString();
            _db.SetSetting("player_uuid", _playerUuid);
            _db.SetSetting("language", currentLanguage);
        }
        else
        {
            _db = null;
        }

        CurrentMapStateId = null;
        HasSavedState = false;
        Log.Information("Character deleted, database re-initialized");
    }
}
