// =============================================================================
// IndestructibleEffect.cs — Sets all incoming damage to 0
// =============================================================================

using FarmGame.Combat;
using FarmGame.Entities.Objects;

namespace FarmGame.World.Effects;

public class IndestructibleEffect : IEffect
{
    public string Id => "indestructible";
    public string DisplayName => "Indestructible";

    public int ModifyDamage(BaseObject obj, int damage, AttackInfo attackInfo) => 0;
}
