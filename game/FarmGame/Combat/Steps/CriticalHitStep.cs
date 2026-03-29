// Step 5: Critical Hit
// If random(0~1) < CritRate → Damage *= CritDamageMultiplier
// Sets context.IsCritical flag for UI feedback.

using System;

namespace FarmGame.Combat.Steps;

public class CriticalHitStep : IDamageStep
{
    private static readonly Random Rng = new();
    private readonly IDamageStep _inner;

    public CriticalHitStep(IDamageStep inner) { _inner = inner; }

    public void Calculate(DamageContext context)
    {
        _inner.Calculate(context);

        if (Rng.NextDouble() < context.CritRate)
        {
            context.Damage *= context.CritDamageMultiplier;
            context.IsCritical = true;
        }
    }
}
