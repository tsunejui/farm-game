// =============================================================================
// MapStateRepository.cs — Persist and restore per-map entity states
//
// Functions:
//   - CreateMapState(mapId)             : Create a new map_state record, returns its UUID.
//   - LoadMapState(mapStateId)          : Load a map_state record by UUID.
//   - SaveEntities(mapStateId, entities): Bulk upsert entity records for a map.
//   - LoadEntities(mapStateId)          : Load all entity records for a map.
//   - FindMapStateByMapId(mapId)        : Find the most recent map_state for a map ID.
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

    public DatabaseResult SaveEntities(string mapStateId, List<MapEntityRecord> entities)
    {
        try
        {
            using var db = _db.CreateConnection();
            var now = DateTime.UtcNow.ToString("o");

            // Delete existing entities for this map state
            db.Execute("DELETE FROM map_entity WHERE map_state_id = ?", mapStateId);

            // Insert all current entities
            foreach (var entity in entities)
            {
                entity.MapStateId = mapStateId;
                if (string.IsNullOrEmpty(entity.Id))
                    entity.Id = Guid.NewGuid().ToString();
                if (string.IsNullOrEmpty(entity.CreatedAt))
                    entity.CreatedAt = now;
                entity.UpdatedAt = now;
                db.Insert(entity);
            }

            // Update map_state timestamp
            db.Execute("UPDATE map_state SET updated_at = ? WHERE id = ?", now, mapStateId);

            return DatabaseResult.Ok();
        }
        catch (Exception ex)
        {
            return DatabaseResult.Fail(DatabaseErrorKind.ConnectionFailed,
                $"Failed to save entities: {ex.Message}");
        }
    }

    public DatabaseResult<List<MapEntityRecord>> LoadEntities(string mapStateId)
    {
        try
        {
            using var db = _db.CreateConnection();
            var records = db.Table<MapEntityRecord>()
                .Where(r => r.MapStateId == mapStateId)
                .ToList();

            return DatabaseResult<List<MapEntityRecord>>.Ok(records);
        }
        catch (Exception ex)
        {
            return DatabaseResult<List<MapEntityRecord>>.Fail(DatabaseErrorKind.ConnectionFailed,
                $"Failed to load entities: {ex.Message}");
        }
    }
}
