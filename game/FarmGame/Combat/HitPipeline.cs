// =============================================================================
// HitPipeline.cs — Assembles the Chain of Responsibility for hit processing
//
// Chain order:
//   1. CalculateDamageHandler — compute raw damage from attacker/target stats
//   2. ApplyEffectsHandler    — run through target's active effects
//   3. EnqueueDamageHandler   — send TakeDamageEvent to target's event queue
//
// Any handler can cancel the chain (e.g. effects negate all damage).
// =============================================================================

using FarmGame.Combat.Handlers;

namespace FarmGame.Combat;

public static class HitPipeline
{
    private static IHitHandler _chain;

    public static IHitHandler Chain => _chain ??= Build();

    private static IHitHandler Build()
    {
        // Build back-to-front: last handler has no next
        IHitHandler handler = new EnqueueDamageHandler();
        handler = new ApplyEffectsHandler(handler);
        handler = new CalculateDamageHandler(handler);
        return handler;
    }

    /// <summary>
    /// Execute the full hit chain. Returns the HitContext for inspection
    /// (e.g. to check IsCritical or Cancelled).
    /// </summary>
    public static HitContext Execute(Entities.Player attacker, World.WorldObject target)
    {
        var context = new HitContext
        {
            Attacker = attacker,
            Target = target,
        };

        Chain.Handle(context);
        return context;
    }
}
