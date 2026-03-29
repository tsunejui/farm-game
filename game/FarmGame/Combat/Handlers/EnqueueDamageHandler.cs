// =============================================================================
// Step 3: Enqueue the TakeDamageEvent into the target's event queue
//
// Final handler in the chain. Takes the computed damage and sends it
// as a TakeDamageEvent to the target object's state update queue.
// =============================================================================

using FarmGame.World.Events;

namespace FarmGame.Combat.Handlers;

public class EnqueueDamageHandler : IHitHandler
{
    public void Handle(HitContext context)
    {
        context.Target.EnqueueEvent(
            new TakeDamageEvent(context.Damage, context.IsCritical));
    }
}
