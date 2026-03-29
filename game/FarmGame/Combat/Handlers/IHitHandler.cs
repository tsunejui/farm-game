// =============================================================================
// IHitHandler.cs — Chain of Responsibility interface for hit processing
//
// Each handler processes the HitContext, then calls _next.Handle() to
// pass it down the chain. A handler may stop the chain by setting
// context.Cancelled = true and not calling _next.
// =============================================================================

namespace FarmGame.Combat.Handlers;

public interface IHitHandler
{
    void Handle(HitContext context);
}
