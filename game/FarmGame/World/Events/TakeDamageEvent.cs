// =============================================================================
// TakeDamageEvent.cs — Applies pre-calculated damage to an object
//
// Damage = 0: shows "0" damage number but does not deduct HP.
// Damage > 0: distributes over DamageTickDurationMs via ObjectState.TakeDamage.
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

        if (_amount <= 0)
        {
            // Show "0" damage number but don't deduct HP
            obj.State.ShowDamageNumberOnly(0, _isCritical);
            IsComplete = true;
            return;
        }

        obj.State.TakeDamage(_amount, _isCritical);
    }

    public void Update(WorldObject obj, GameMap map, float deltaTime)
    {
        if (!_started) return;

        if (!obj.State.IsTakingDamage)
            IsComplete = true;
    }
}
