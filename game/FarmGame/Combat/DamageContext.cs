// =============================================================================
// DamageContext.cs — Data carrier for the damage calculation pipeline
//
// Holds attacker stats, target stats, and the running damage value that each
// decorator step reads and writes. Also carries output flags (e.g. IsCritical).
// =============================================================================

namespace FarmGame.Combat;

public class DamageContext
{
    // --- Attacker stats ---
    public float Strength { get; set; }
    public float Dexterity { get; set; }
    public float WeaponAtk { get; set; }
    public float BuffPercent { get; set; }        // e.g. 0.10 = +10%

    // --- Skill ---
    public float SkillPowerPercent { get; set; } = 1f;  // 1.0 = 100%
    public float ElementalBonus { get; set; }     // additive bonus (e.g. 0.15)
    public float RacialBonus { get; set; }        // additive bonus

    // --- Critical ---
    public float CritRate { get; set; }           // 0.0 ~ 1.0
    public float CritDamageMultiplier { get; set; } = 1.5f;

    // --- Target stats ---
    public float TargetDefense { get; set; }
    public float TargetShield { get; set; }
    public float TargetFlatReduction { get; set; }

    // --- Pipeline state ---
    public float Damage { get; set; }

    // --- Output flags ---
    public bool IsCritical { get; set; }
}
