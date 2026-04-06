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
using FarmGame.Persistence.Repositories;
using FarmGame.World;
using FarmGame.World.Effects;
using Serilog;

namespace FarmGame.Core;

public class GameSession
{
    private DatabaseManager _dbManager;
    private PlayerStateRepository _playerStateRepo;
    private MapStateRepository _mapStateRepo;
    private SettingRepository _settings;
    private string _playerUuid;
    public bool HasSavedState { get; private set; }

    public GameSession(DatabaseManager dbManager, string playerUuid, PlayerState savedState)
    {
        _dbManager = dbManager;
        _playerStateRepo = new PlayerStateRepository(dbManager);
        _mapStateRepo = new MapStateRepository(dbManager);
        _settings = new SettingRepository(dbManager);
        _playerUuid = playerUuid;
        HasSavedState = savedState != null;
    }

    public PlayerState LoadPlayer()
    {
        if (_playerStateRepo == null) return null;
        var result = _playerStateRepo.Load(_playerUuid);
        var state = result.Success ? result.Value : null;
        HasSavedState = state != null;
        return state;
    }

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
            MaxHp = player.MaxHp,
            CurrentHp = player.CurrentHp,
            Strength = player.Strength,
            Dexterity = player.Dexterity,
            WeaponAtk = player.WeaponAtk,
            BuffPercent = player.BuffPercent,
            CritRate = player.CritRate,
            CritDamage = player.CritDamage,
        };

        var result = _playerStateRepo.Save(_playerUuid, state, GameConstants.GameTitle);
        if (result.Success)
            Log.Information("Player state saved: pos=({X},{Y}), dir={Dir}, mapState={MapState}",
                state.PositionX, state.PositionY, state.FacingDirection, CurrentMapStateId ?? "null");
        else
            Log.Error("Failed to save player state: {Error}", result.ErrorMessage);
    }

    public void LoadOrCreateMapState(string mapId, GameMap map, PlayerState savedState)
    {
        if (_mapStateRepo == null) return;

        var mapStateId = savedState?.CurrentMapStateId;

        if (mapStateId == null)
        {
            var findResult = _mapStateRepo.FindByMapId(mapId);
            if (findResult.Success)
                mapStateId = findResult.Value.Id;
        }

        if (mapStateId != null)
        {
            var loadResult = _mapStateRepo.LoadObjects(mapStateId);
            if (loadResult.Success && loadResult.Value.Count > 0)
            {
                var savedByItem = new Dictionary<string, List<MapObjectModel>>();
                foreach (var r in loadResult.Value)
                {
                    if (!savedByItem.ContainsKey(r.ItemId))
                        savedByItem[r.ItemId] = new List<MapObjectModel>();
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
                    RestoreObjectEffects(obj);

                    if (obj.TileX != record.TileX || obj.TileY != record.TileY)
                        map.MoveObject(obj, record.TileX, record.TileY);
                }

                CurrentMapStateId = mapStateId;
                _mapStateRepo.SetTtl(mapStateId, 0);
                return;
            }
        }

        var createResult = _mapStateRepo.CreateMapState(mapId);
        if (createResult.Success)
        {
            CurrentMapStateId = createResult.Value;
            foreach (var obj in map.Objects)
            {
                obj.InstanceId = Guid.NewGuid().ToString();
                ApplyDefaultEffects(obj);
            }
            SaveMapObjects(map);
        }
    }

    public void SaveMapObjects(GameMap map)
    {
        if (_mapStateRepo == null || CurrentMapStateId == null) return;

        var models = map.Objects.Select(o => new MapObjectModel
        {
            Id = o.InstanceId ?? Guid.NewGuid().ToString(),
            ItemId = o.ItemId,
            Category = o.Category.ToString().ToLowerInvariant(),
            TileX = o.TileX,
            TileY = o.TileY,
            Hp = o.State.CurrentHp,
            StateJson = "{}",
        }).ToList();

        var result = _mapStateRepo.SaveObjects(CurrentMapStateId, models);
        if (result.Success)
        {
            foreach (var obj in map.Objects)
                SaveObjectEffects(obj);
        }
        else
            Log.Error("Failed to save map objects: {Error}", result.ErrorMessage);
    }

    public void SetMapStateTtlOnLeave()
    {
        if (_mapStateRepo == null || CurrentMapStateId == null) return;

        long ttlUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 300;
        _mapStateRepo.SetTtl(CurrentMapStateId, ttlUtc);
    }

    private void ApplyDefaultEffects(WorldObject obj)
    {
        foreach (var defEffect in obj.Definition.Logic.DefaultEffects)
        {
            var effect = EffectRegistry.Get(defEffect.EffectId);
            if (effect == null) continue;
            obj.AddEffect(new ActiveEffect(effect, defEffect.Ttl));
        }
    }

    private void RestoreObjectEffects(WorldObject obj)
    {
        if (_mapStateRepo == null || string.IsNullOrEmpty(obj.InstanceId)) return;

        var loadResult = _mapStateRepo.LoadEffects(obj.InstanceId);
        if (!loadResult.Success) return;

        foreach (var model in loadResult.Value)
        {
            var effect = EffectRegistry.Get(model.EffectId);
            if (effect == null) continue;

            DateTime appliedAt = DateTime.TryParse(model.UpdatedAt, out var dt)
                ? dt : DateTime.UtcNow;

            var ae = ActiveEffect.FromPersisted(effect, model.Ttl, appliedAt);
            if (!ae.IsExpired)
                obj.AddEffect(ae);
        }
    }

    private void SaveObjectEffects(WorldObject obj)
    {
        if (_mapStateRepo == null || string.IsNullOrEmpty(obj.InstanceId)) return;

        var effectModels = obj.Effects
            .Where(ae => !ae.IsExpired)
            .Select(ae => new ObjectEffectModel
            {
                EffectId = ae.EffectId,
                Ttl = ae.TtlSeconds,
            }).ToList();

        _mapStateRepo.SaveEffects(obj.InstanceId, effectModels);
    }

    public void SaveSetting(string key, string value) => _settings?.Set(key, value);
    public string GetSetting(string key, string defaultValue = null) =>
        _settings?.Get(key, defaultValue) ?? defaultValue;

    public void ChangeLanguage(string language, string contentDir)
    {
        LocaleManager.Load(contentDir, language);
        SaveSetting("language", language);
    }

    public void DeleteAndReset()
    {
        var dbPath = _dbManager.DatabasePath;
        var currentLanguage = LocaleManager.CurrentLanguage;

        _dbManager.Backup();
        if (File.Exists(dbPath))
            File.Delete(dbPath);

        var freshDb = new DatabaseManager(GameConstants.GameTitle);
        var initResult = freshDb.Initialize();
        if (initResult.Success)
        {
            _dbManager = freshDb;
            _playerStateRepo = new PlayerStateRepository(freshDb);
            _mapStateRepo = new MapStateRepository(freshDb);
            _settings = new SettingRepository(freshDb);
            _playerUuid = Guid.NewGuid().ToString();
            _settings.Set("player_uuid", _playerUuid);
            _settings.Set("language", currentLanguage);
        }
        else
        {
            _dbManager = null;
            _playerStateRepo = null;
            _mapStateRepo = null;
            _settings = null;
        }

        CurrentMapStateId = null;
        HasSavedState = false;
    }
}
