// =============================================================================
// IndestructibleEffect.cs — Sets all incoming damage to 0
// =============================================================================

using FarmGame.Combat;

namespace FarmGame.World.Effects;

public class IndestructibleEffect : IEffect
{
    public string Id => "indestructible";
    public string DisplayName => "Indestructible";

    public int ModifyDamage(WorldObject obj, int damage, AttackInfo attackInfo) => 0;
}
