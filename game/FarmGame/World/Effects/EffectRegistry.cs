// =============================================================================
// EffectRegistry.cs — Maps effect IDs to IEffect instances
//
// All known effects are registered here. Use Get(id) to look up an effect.
// =============================================================================

using System.Collections.Generic;

namespace FarmGame.World.Effects;

public static class EffectRegistry
{
    private static readonly Dictionary<string, IEffect> _effects = new();

    static EffectRegistry()
    {
        Register(new IndestructibleEffect());
    }

    public static void Register(IEffect effect)
    {
        _effects[effect.Id] = effect;
    }

    public static IEffect Get(string id)
    {
        return _effects.GetValueOrDefault(id);
    }

    public static bool Exists(string id) => _effects.ContainsKey(id);
}
