using System.Collections.Generic;
using System.Linq;
using FarmGame.Models;
using FarmGame.Persistence.Entities;

namespace FarmGame.Mappings;

public static class ObjectEffectMapping
{
    public static ObjectEffectModel ToModel(this ObjectEffectRecord entity)
    {
        return new ObjectEffectModel
        {
            EffectId = entity.EffectId,
            Ttl = entity.Ttl,
            UpdatedAt = entity.UpdatedAt,
        };
    }

    public static List<ObjectEffectModel> ToModels(this List<ObjectEffectRecord> entities)
    {
        return entities.Select(e => e.ToModel()).ToList();
    }

    public static ObjectEffectRecord ToEntity(this ObjectEffectModel model)
    {
        return new ObjectEffectRecord
        {
            EffectId = model.EffectId,
            Ttl = model.Ttl,
        };
    }

    public static List<ObjectEffectRecord> ToEntities(this List<ObjectEffectModel> models)
    {
        return models.Select(m => m.ToEntity()).ToList();
    }
}
