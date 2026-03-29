// Step 6: Final Mitigation
// Formula: Damage - Shield - FlatReduction
// Represents shields, blocks, or absolute damage reduction on the target.

using System;

namespace FarmGame.Combat.Steps;

public class FinalMitigationStep : IDamageStep
{
    private readonly IDamageStep _inner;

    public FinalMitigationStep(IDamageStep inner) { _inner = inner; }

    public void Calculate(DamageContext context)
    {
        _inner.Calculate(context);

        context.Damage -= context.TargetShield;
        context.Damage -= context.TargetFlatReduction;

        if (context.Damage < 0f) context.Damage = 0f;
    }
}
