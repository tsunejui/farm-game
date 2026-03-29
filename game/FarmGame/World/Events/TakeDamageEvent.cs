// =============================================================================
// TakeDamageEvent.cs — Applies damage over time to an object
//
// Runs the damage through the object's active effects first (e.g.
// IndestructibleEffect sets damage to 0). Then distributes the
// remaining damage over DamageTickDurationMs via ObjectState.TakeDamage.
// =============================================================================

namespace FarmGame.World.Events;

public class TakeDamageEvent : IObjectEvent
{
    private readonly int _originalAmount;
    private readonly bool _isCritical;
    private bool _started;

    public bool IsComplete { get; private set; }

    public TakeDamageEvent(int amount, bool isCritical = false)
    {
        _originalAmount = amount;
        _isCritical = isCritical;
    }

    public void Start(WorldObject obj, GameMap map)
    {
        _started = true;

        // Run damage through active effects (e.g. Indestructible → 0)
        int finalDamage = obj.ApplyEffectsToDamage(_originalAmount);

        if (finalDamage <= 0)
        {
            // Effects negated all damage — skip entirely, no damage number
            IsComplete = true;
            return;
        }

        obj.State.TakeDamage(finalDamage, _isCritical);
    }

    public void Update(WorldObject obj, GameMap map, float deltaTime)
    {
        if (!_started) return;

        // Complete when the damage tick window has finished
        if (!obj.State.IsTakingDamage)
            IsComplete = true;
    }
}
