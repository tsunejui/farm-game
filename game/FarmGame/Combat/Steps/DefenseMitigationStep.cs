// Step 2: Defense Mitigation
// Supports two models configured via GameConstants:
//   - "subtraction": Damage = BaseATK - TargetDEF
//   - "division":    Damage = BaseATK * (Constant / (Constant + TargetDEF))

using FarmGame.Core;

namespace FarmGame.Combat.Steps;

public class DefenseMitigationStep : IDamageStep
{
    private readonly IDamageStep _inner;

    public DefenseMitigationStep(IDamageStep inner) { _inner = inner; }

    public void Calculate(DamageContext context)
    {
        _inner.Calculate(context);

        string model = GameConstants.DefenseModel;
        float def = context.TargetDefense;

        if (model == "division")
        {
            float constant = GameConstants.DefenseConstant;
            context.Damage *= constant / (constant + def);
        }
        else // "subtraction" (default)
        {
            context.Damage -= def;
        }

        if (context.Damage < 0f) context.Damage = 0f;
    }
}
