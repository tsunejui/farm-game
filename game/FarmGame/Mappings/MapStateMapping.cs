using FarmGame.Models;
using FarmGame.Persistence.Entities;

namespace FarmGame.Mappings;

public static class MapStateMapping
{
    public static MapStateModel ToModel(this MapStateRecord entity)
    {
        return new MapStateModel
        {
            Id = entity.Id,
            MapId = entity.MapId,
            StateJson = entity.StateJson,
            TtlUtc = entity.TtlUtc,
        };
    }
}
