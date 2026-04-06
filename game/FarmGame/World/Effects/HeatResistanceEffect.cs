// =============================================================================
// HeatResistanceEffect.cs — Ignores fire element natural attack damage
//
// Applied to campfires and firewood so they don't damage each other.
// =============================================================================

using FarmGame.Combat;
using FarmGame.Entities.Objects;

namespace FarmGame.World.Effects;

public class HeatResistanceEffect : IEffect
{
    public string Id => "heat_resistance";
    public string DisplayName => "Heat Resistance";

    public int ModifyDamage(BaseObject obj, int damage, AttackInfo attackInfo)
    {
        if (attackInfo.Category == AttackCategory.Natural && attackInfo.Element == ElementType.Fire)
            return 0;
        return damage;
    }
}
