// =============================================================================
// Step 2: Apply target object's active effects to the damage number
//
// Iterates through the target's Effects array in order. Each effect
// may modify (reduce, amplify, or zero-out) the damage.
// Damage = 0 still proceeds (shows "0" damage number).
// Damage < 0 (miss) cancels the chain entirely.
// =============================================================================

namespace FarmGame.Combat.Handlers;

public class ApplyEffectsHandler : IHitHandler
{
    private readonly IHitHandler _next;

    public ApplyEffectsHandler(IHitHandler next) { _next = next; }

    public void Handle(HitContext context)
    {
        context.Damage = context.Target.ApplyEffectsToDamage(context.Damage);

        // Only cancel on miss (-1); damage = 0 still shows effect
        if (context.Damage < 0)
        {
            context.Cancelled = true;
            return;
        }

        _next?.Handle(context);
    }
}
