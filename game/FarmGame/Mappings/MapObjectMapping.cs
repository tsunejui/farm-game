using System.Collections.Generic;
using System.Linq;
using FarmGame.Models;
using FarmGame.Persistence.Entities;

namespace FarmGame.Mappings;

public static class MapObjectMapping
{
    public static MapObjectModel ToModel(this MapObjectRecord entity)
    {
        return new MapObjectModel
        {
            Id = entity.Id,
            ItemId = entity.ItemId,
            Category = entity.Category,
            TileX = entity.TileX,
            TileY = entity.TileY,
            Hp = entity.Hp,
            StateJson = entity.StateJson,
        };
    }

    public static List<MapObjectModel> ToModels(this List<MapObjectRecord> entities)
    {
        return entities.Select(e => e.ToModel()).ToList();
    }

    public static MapObjectRecord ToEntity(this MapObjectModel model)
    {
        return new MapObjectRecord
        {
            Id = model.Id,
            ItemId = model.ItemId,
            Category = model.Category,
            TileX = model.TileX,
            TileY = model.TileY,
            Hp = model.Hp,
            StateJson = model.StateJson,
        };
    }

    public static List<MapObjectRecord> ToEntities(this List<MapObjectModel> models)
    {
        return models.Select(m => m.ToEntity()).ToList();
    }
}
