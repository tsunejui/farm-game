// =============================================================================
// DamagePipeline.cs — Assembles the 7-step decorator chain
//
// Decorator wrapping order (outermost → innermost):
//   MinDamageStep → FinalMitigationStep → CriticalHitStep →
//   DamageVarianceStep → SkillMultiplierStep → DefenseMitigationStep →
//   BaseAtkStep (core, no inner)
//
// When Calculate() is called on the outermost step, execution flows inward
// to BaseAtkStep first, then each decorator applies its transformation
// on the way back out.
// =============================================================================

using FarmGame.Combat.Steps;

namespace FarmGame.Combat;

public static class DamagePipeline
{
    private static IDamageStep _instance;

    /// <summary>
    /// Returns the singleton pipeline. Built once, reused for every attack.
    /// </summary>
    public static IDamageStep Instance => _instance ??= Build();

    private static IDamageStep Build()
    {
        // Build inside-out: BaseAtkStep is the innermost (runs first)
        IDamageStep step = new BaseAtkStep();
        step = new DefenseMitigationStep(step);
        step = new SkillMultiplierStep(step);
        step = new DamageVarianceStep(step);
        step = new CriticalHitStep(step);
        step = new FinalMitigationStep(step);
        step = new MinDamageStep(step);
        return step;
    }

    /// <summary>
    /// Run the full pipeline and return the final integer damage.
    /// </summary>
    public static int CalculateDamage(DamageContext context)
    {
        context.Damage = 0f;
        context.IsCritical = false;
        Instance.Calculate(context);
        return (int)System.Math.Ceiling(context.Damage);
    }
}
