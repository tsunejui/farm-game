// Step 1: Base ATK Calculation
// Formula: (Strength + Dexterity + WeaponATK) * (1 + BuffPercent)

namespace FarmGame.Combat.Steps;

public class BaseAtkStep : IDamageStep
{
    private readonly IDamageStep _inner;

    public BaseAtkStep(IDamageStep inner = null) { _inner = inner; }

    public void Calculate(DamageContext context)
    {
        _inner?.Calculate(context);

        float rawAtk = context.Strength + context.Dexterity + context.WeaponAtk;
        context.Damage = rawAtk * (1f + context.BuffPercent);
    }
}
