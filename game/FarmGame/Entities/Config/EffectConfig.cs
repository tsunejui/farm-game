using FarmGame.Data;

namespace FarmGame.Entities.Config;

/// <summary>
/// Wraps an effect definition (Effects/*.yaml).
/// Keyed by effect_id.
/// </summary>
public class EffectConfig
{
    public string Id => Data.EffectId;
    public EffectDefinition Data { get; set; }

    public EffectConfig(EffectDefinition data)
    {
        Data = data;
    }
}
