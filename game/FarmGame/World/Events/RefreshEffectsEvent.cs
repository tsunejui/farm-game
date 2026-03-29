// =============================================================================
// RefreshEffectsEvent.cs — Periodic effect tick and cleanup
//
// 1. Calls OnTick() on each active effect (aura/DoT logic)
// 2. Removes expired effects
// =============================================================================

using Serilog;

namespace FarmGame.World.Events;

public class RefreshEffectsEvent : IObjectEvent
{
    public bool IsComplete { get; private set; }

    public void Start(WorldObject obj, GameMap map)
    {
        // Tick active effects (aura damage, buffs, etc.)
        foreach (var ae in obj.Effects)
        {
            if (!ae.IsExpired)
                ae.Effect.OnTick(obj, map);
        }

        // Remove expired effects
        int removed = 0;
        for (int i = obj.Effects.Count - 1; i >= 0; i--)
        {
            if (obj.Effects[i].IsExpired)
            {
                Log.Debug("Effect expired: {EffectId} on {ItemId}",
                    obj.Effects[i].EffectId, obj.ItemId);
                obj.Effects.RemoveAt(i);
                removed++;
            }
        }

        IsComplete = true;
    }

    public void Update(WorldObject obj, GameMap map, float deltaTime) { }
}
