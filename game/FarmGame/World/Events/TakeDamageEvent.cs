// =============================================================================
// TakeDamageEvent.cs — Applies pre-calculated damage to an object
//
// Carries AttackInfo (category + element) for effect filtering.
// Damage = 0: shows "0" damage number but does not deduct HP.
// Damage > 0: distributes over DamageTickDurationMs via ObjectState.TakeDamage.
// =============================================================================

using FarmGame.Combat;
using FarmGame.Entities.Objects;

namespace FarmGame.World.Events;

public class TakeDamageEvent : IObjectEvent
{
    private readonly int _amount;
    private readonly bool _isCritical;
    private readonly AttackInfo _attackInfo;
    private bool _started;

    public bool IsComplete { get; private set; }

    public TakeDamageEvent(int amount, bool isCritical = false, AttackInfo attackInfo = null)
    {
        _amount = amount;
        _isCritical = isCritical;
        _attackInfo = attackInfo ?? AttackInfo.Physical;
    }

    public void Start(BaseObject obj, GameMap map)
    {
        _started = true;

        // Let effects filter damage based on attack type
        int finalAmount = obj.ApplyEffectsToDamage(_amount, _attackInfo);

        if (finalAmount <= 0)
        {
            obj.State.ShowDamageNumberOnly(0, _isCritical);
            IsComplete = true;
            return;
        }

        obj.State.TakeDamage(finalAmount, _isCritical);
    }

    public void Update(BaseObject obj, GameMap map, float deltaTime)
    {
        if (!_started) return;

        if (!obj.State.IsTakingDamage)
            IsComplete = true;
    }
}
