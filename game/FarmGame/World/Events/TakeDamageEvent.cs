// =============================================================================
// TakeDamageEvent.cs — Applies pre-calculated damage to an object
//
// The damage amount has already been computed by the hit handler chain
// (CalculateDamage → ApplyEffects). This event simply distributes it
// over DamageTickDurationMs via ObjectState.TakeDamage.
// =============================================================================

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

        if (!obj.State.IsTakingDamage)
            IsComplete = true;
    }
}
