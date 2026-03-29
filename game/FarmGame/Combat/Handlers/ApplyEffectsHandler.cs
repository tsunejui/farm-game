// =============================================================================
// Step 2: Apply target object's active effects to the damage number
//
// Iterates through the target's Effects array in order. Each effect
// may modify (reduce, amplify, or zero-out) the damage. If damage
// drops to 0, the chain is cancelled — no damage event is enqueued.
// =============================================================================

namespace FarmGame.Combat.Handlers;

public class ApplyEffectsHandler : IHitHandler
{
    private readonly IHitHandler _next;

    public ApplyEffectsHandler(IHitHandler next) { _next = next; }

    public void Handle(HitContext context)
    {
        context.Damage = context.Target.ApplyEffectsToDamage(context.Damage);

        if (context.Damage <= 0)
        {
            context.Cancelled = true;
            return;
        }

        _next?.Handle(context);
    }
}
