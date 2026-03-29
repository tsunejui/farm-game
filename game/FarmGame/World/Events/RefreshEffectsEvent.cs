// =============================================================================
// RefreshEffectsEvent.cs — Periodic self-check of active effects
//
// Iterates the object's Effects array. For each non-expired effect,
// confirms it is still active (future effects may have on-tick logic).
// Expired effects are removed. Completes immediately.
// =============================================================================

using Serilog;

namespace FarmGame.World.Events;

public class RefreshEffectsEvent : IObjectEvent
{
    public bool IsComplete { get; private set; }

    public void Start(WorldObject obj, GameMap map)
    {
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

        if (removed > 0)
            Log.Debug("RefreshEffects: {ItemId} — {Removed} expired, {Remaining} active",
                obj.ItemId, removed, obj.Effects.Count);

        IsComplete = true;
    }

    public void Update(WorldObject obj, GameMap map, float deltaTime) { }
}
