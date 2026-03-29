// =============================================================================
// MapStateRepository.cs — Persist and restore per-map object states
//
// Functions:
//   - CreateMapState(mapId)              : Create a new map_state record, returns its UUID.
//   - FindByMapId(mapId)                 : Find the most recent map_state for a map ID.
//   - SaveObjects(mapStateId, objects)   : Bulk upsert object records for a map.
//   - LoadObjects(mapStateId)            : Load all object records for a map.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using FarmGame.Persistence.Models;

namespace FarmGame.Persistence.Repositories;

public class MapStateRepository
{
    private readonly DatabaseBootstrapper _db;

    public MapStateRepository(DatabaseBootstrapper db)
    {
        _db = db;
    }

    public DatabaseResult<string> CreateMapState(string mapId, string stateJson = "{}")
    {
        try
        {
            using var db = _db.CreateConnection();
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
            using var db = _db.CreateConnection();
            var record = db.Table<MapStateRecord>()
                .Where(r => r.MapId == mapId)
                .OrderByDescending(r => r.UpdatedAt)
                .FirstOrDefault();

            if (record == null)
                return DatabaseResult<MapStateRecord>.Fail(DatabaseErrorKind.None,
                    $"No map state found for '{mapId}'");

            return DatabaseResult<MapStateRecord>.Ok(record);
        }
        catch (Exception ex)
        {
            return DatabaseResult<MapStateRecord>.Fail(DatabaseErrorKind.ConnectionFailed,
                $"Failed to find map state: {ex.Message}");
        }
    }

    public DatabaseResult SaveObjects(string mapStateId, List<MapObjectRecord> objects)
    {
        try
        {
            using var db = _db.CreateConnection();
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
            using var db = _db.CreateConnection();
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
}
