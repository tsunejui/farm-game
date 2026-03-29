// =============================================================================
// IDamageStep.cs — Decorator interface for damage calculation pipeline
//
// Each step wraps an inner step. Calculate() calls the inner step first,
// then applies its own transformation to context.Damage.
// =============================================================================

namespace FarmGame.Combat;

public interface IDamageStep
{
    void Calculate(DamageContext context);
}
