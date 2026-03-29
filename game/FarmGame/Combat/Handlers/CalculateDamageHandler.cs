// =============================================================================
// Step 1: Calculate raw damage number from attacker and target stats
//
// Runs the decorator-based DamagePipeline (7-step ATK/DEF/crit calculation)
// and writes the result into HitContext.Damage.
// =============================================================================

namespace FarmGame.Combat.Handlers;

public class CalculateDamageHandler : IHitHandler
{
    private readonly IHitHandler _next;

    public CalculateDamageHandler(IHitHandler next) { _next = next; }

    public void Handle(HitContext context)
    {
        var attacker = context.Attacker;
        var target = context.Target;

        var dmgCtx = new DamageContext
        {
            Strength = attacker.Strength,
            Dexterity = attacker.Dexterity,
            WeaponAtk = attacker.WeaponAtk,
            BuffPercent = attacker.BuffPercent,
            SkillPowerPercent = 1f,
            CritRate = attacker.CritRate,
            CritDamageMultiplier = attacker.CritDamage,
            TargetDefense = target.Definition.Logic.Defense,
        };

        context.Damage = DamagePipeline.CalculateDamage(dmgCtx);
        context.IsCritical = dmgCtx.IsCritical;

        _next?.Handle(context);
    }
}
