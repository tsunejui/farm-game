using System;
using System.Collections.Generic;
using System.Linq;
using FarmGame.Persistence.Entities;

namespace FarmGame.Persistence.Repositories;

public class MapStateRepository
{
    private readonly DatabaseManager _dbManager;

    public MapStateRepository(DatabaseManager dbManager)
    {
        _dbManager = dbManager;
    }

    public DatabaseResult<string> CreateMapState(string mapId, string stateJson = "{}")
    {
        try
        {
            using var db = _dbManager.Connect();
            var id = Guid.NewGuid().ToString();
            var now = DateTime.UtcNow.ToString("o");

            db.Insert(new MapStateRecord
            {
                Id = id,
                MapId = mapId,
                StateJson = stateJson,
                CreatedAt = now,
                UpdatedAt = now,
            });

            return DatabaseResult<string>.Ok(id);
        }
        catch (Exception ex)
        {
            return DatabaseResult<string>.Fail(DatabaseErrorKind.ConnectionFailed,
                $"Failed to create map state: {ex.Message}");
        }
    }

    public DatabaseResult<MapStateRecord> FindByMapId(string mapId)
    {
        try
        {
            using var db = _dbManager.Connect();
            var record = db.Table<MapStateRecord>()
                .Where(r => r.MapId == mapId)
                .OrderByDescending(r => r.UpdatedAt)
                .FirstOrDefault();

            if (record == null)
                return DatabaseResult<MapStateRecord>.Fail(DatabaseErrorKind.None,
                    $"No map state found for '{mapId}'");

            if (record.TtlUtc > 0)
            {
                long nowUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if (nowUtc > record.TtlUtc)
                {
                    db.Execute("DELETE FROM map_object WHERE map_state_id = ?", record.Id);
                    db.Execute("DELETE FROM map_state WHERE id = ?", record.Id);
                    return DatabaseResult<MapStateRecord>.Fail(DatabaseErrorKind.None,
                        $"Map state for '{mapId}' expired (TTL)");
                }
            }

            return DatabaseResult<MapStateRecord>.Ok(record);
        }
        catch (Exception ex)
        {
            return DatabaseResult<MapStateRecord>.Fail(DatabaseErrorKind.ConnectionFailed,
                $"Failed to find map state: {ex.Message}");
        }
    }

    public DatabaseResult SetTtl(string mapStateId, long ttlUtc)
    {
        try
        {
            using var db = _dbManager.Connect();
            db.Execute("UPDATE map_state SET TtlUtc = ? WHERE id = ?", ttlUtc, mapStateId);
            return DatabaseResult.Ok();
        }
        catch (Exception ex)
        {
            return DatabaseResult.Fail(DatabaseErrorKind.ConnectionFailed,
                $"Failed to set TTL: {ex.Message}");
        }
    }

    public DatabaseResult SaveObjects(string mapStateId, List<MapObjectRecord> objects)
    {
        try
        {
            using var db = _dbManager.Connect();
            var now = DateTime.UtcNow.ToString("o");

            db.Execute("DELETE FROM map_object WHERE map_state_id = ?", mapStateId);

            foreach (var obj in objects)
            {
                obj.MapStateId = mapStateId;
                if (string.IsNullOrEmpty(obj.Id))
                    obj.Id = Guid.NewGuid().ToString();
                if (string.IsNullOrEmpty(obj.CreatedAt))
                    obj.CreatedAt = now;
                obj.UpdatedAt = now;
                db.Insert(obj);
            }

            db.Execute("UPDATE map_state SET updated_at = ? WHERE id = ?", now, mapStateId);

            return DatabaseResult.Ok();
        }
        catch (Exception ex)
        {
            return DatabaseResult.Fail(DatabaseErrorKind.ConnectionFailed,
                $"Failed to save objects: {ex.Message}");
        }
    }

    public DatabaseResult<List<MapObjectRecord>> LoadObjects(string mapStateId)
    {
        try
        {
            using var db = _dbManager.Connect();
            var records = db.Table<MapObjectRecord>()
                .Where(r => r.MapStateId == mapStateId)
                .ToList();

            return DatabaseResult<List<MapObjectRecord>>.Ok(records);
        }
        catch (Exception ex)
        {
            return DatabaseResult<List<MapObjectRecord>>.Fail(DatabaseErrorKind.ConnectionFailed,
                $"Failed to load objects: {ex.Message}");
        }
    }

    public DatabaseResult SaveEffects(string objectId, List<ObjectEffectRecord> effects)
    {
        try
        {
            using var db = _dbManager.Connect();
            var now = DateTime.UtcNow.ToString("o");

            db.Execute("DELETE FROM object_effect WHERE object_id = ?", objectId);

            foreach (var eff in effects)
            {
                eff.ObjectId = objectId;
                if (string.IsNullOrEmpty(eff.Id))
                    eff.Id = Guid.NewGuid().ToString();
                if (string.IsNullOrEmpty(eff.CreatedAt))
                    eff.CreatedAt = now;
                eff.UpdatedAt = now;
                db.Insert(eff);
            }

            return DatabaseResult.Ok();
        }
        catch (Exception ex)
        {
            return DatabaseResult.Fail(DatabaseErrorKind.ConnectionFailed,
                $"Failed to save effects: {ex.Message}");
        }
    }

    public DatabaseResult<List<ObjectEffectRecord>> LoadEffects(string objectId)
    {
        try
        {
            using var db = _dbManager.Connect();
            var records = db.Table<ObjectEffectRecord>()
                .Where(r => r.ObjectId == objectId)
                .ToList();

            return DatabaseResult<List<ObjectEffectRecord>>.Ok(records);
        }
        catch (Exception ex)
        {
            return DatabaseResult<List<ObjectEffectRecord>>.Fail(DatabaseErrorKind.ConnectionFailed,
                $"Failed to load effects: {ex.Message}");
        }
    }
}
