// Step 3: Skill & Damage Multipliers
// Formula: Damage * SkillPower% * (1 + ElementalBonus + RacialBonus)

namespace FarmGame.Combat.Steps;

public class SkillMultiplierStep : IDamageStep
{
    private readonly IDamageStep _inner;

    public SkillMultiplierStep(IDamageStep inner) { _inner = inner; }

    public void Calculate(DamageContext context)
    {
        _inner.Calculate(context);

        context.Damage *= context.SkillPowerPercent
                        * (1f + context.ElementalBonus + context.RacialBonus);
    }
}
