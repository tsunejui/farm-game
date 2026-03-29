// =============================================================================
// TakeDamageEvent.cs — Applies damage over time to an object
//
// Distributes the damage amount evenly over DamageTickDurationMs.
// Triggers the damage number display and flicker effect on the object's state.
// =============================================================================

using FarmGame.Core;

namespace FarmGame.World.Events;

public class TakeDamageEvent : IObjectEvent
{
    private readonly int _amount;
    private readonly bool _isCritical;
    private bool _started;

    public bool IsComplete { get; private set; }

    public TakeDamageEvent(int amount, bool isCritical = false)
    {
        _amount = amount;
        _isCritical = isCritical;
    }

    public void Start(WorldObject obj, GameMap map)
    {
        _started = true;
        obj.State.TakeDamage(_amount, _isCritical);
    }

    public void Update(WorldObject obj, GameMap map, float deltaTime)
    {
        if (!_started) return;

        // Complete when the damage tick window has finished
        if (!obj.State.IsTakingDamage)
            IsComplete = true;
    }
}
