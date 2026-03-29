// Step 4: Damage Variance
// Formula: Damage * (1 + random(-variance, +variance))
// e.g. variance = 0.05 → damage fluctuates 95%~105%

using System;
using FarmGame.Core;

namespace FarmGame.Combat.Steps;

public class DamageVarianceStep : IDamageStep
{
    private static readonly Random Rng = new();
    private readonly IDamageStep _inner;

    public DamageVarianceStep(IDamageStep inner) { _inner = inner; }

    public void Calculate(DamageContext context)
    {
        _inner.Calculate(context);

        float variance = GameConstants.DamageVariance;
        float roll = (float)(Rng.NextDouble() * 2.0 - 1.0); // -1.0 ~ +1.0
        context.Damage *= 1f + roll * variance;
    }
}
