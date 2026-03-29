// Step 7: Final Output — Minimum Damage Guarantee
// If final damage < 1, force to 1 (guaranteed minimum).

using System;

namespace FarmGame.Combat.Steps;

public class MinDamageStep : IDamageStep
{
    private readonly IDamageStep _inner;

    public MinDamageStep(IDamageStep inner) { _inner = inner; }

    public void Calculate(DamageContext context)
    {
        _inner.Calculate(context);

        if (context.Damage < 1f)
            context.Damage = 1f;
    }
}
