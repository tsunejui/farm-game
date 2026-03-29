namespace FarmGame.Combat;

/// <summary>
/// Category of attack.
/// </summary>
public enum AttackCategory
{
    Physical,  // Melee/ranged physical strikes
    Magical,   // Spell-based damage
    Natural    // Environmental/elemental damage (fire, water, etc.)
}

/// <summary>
/// Elemental attribute for Natural attacks.
/// </summary>
public enum ElementType
{
    None,   // No element (physical/magical attacks)
    Gold,   // Metal element
    Wood,   // Plant element
    Water,  // Water element
    Fire,   // Fire element
    Earth   // Earth/ground element
}

/// <summary>
/// Describes the type and element of an attack.
/// </summary>
public record AttackInfo(
    AttackCategory Category = AttackCategory.Physical,
    ElementType Element = ElementType.None)
{
    public static readonly AttackInfo Physical = new(AttackCategory.Physical);
    public static readonly AttackInfo Magical = new(AttackCategory.Magical);
    public static AttackInfo NaturalFire => new(AttackCategory.Natural, ElementType.Fire);
    public static AttackInfo NaturalWater => new(AttackCategory.Natural, ElementType.Water);
    public static AttackInfo NaturalWood => new(AttackCategory.Natural, ElementType.Wood);
    public static AttackInfo NaturalGold => new(AttackCategory.Natural, ElementType.Gold);
    public static AttackInfo NaturalEarth => new(AttackCategory.Natural, ElementType.Earth);
}
