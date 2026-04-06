using System.Collections.Generic;
using FarmGame.Data;

namespace FarmGame.Entities.Objects.Creatures;

/// <summary>
/// Player entity in the object hierarchy.
/// Always interactable (owns a DamageQueue for receiving combat damage).
/// Combat stats are managed here; the Player coordinator delegates to this.
/// </summary>
public class PlayerObject : BaseObject
{
    // Combat stats
    public float Strength { get; set; }
    public float Dexterity { get; set; }
    public float WeaponAtk { get; set; }
    public float BuffPercent { get; set; }
    public float CritRate { get; set; }
    public float CritDamage { get; set; }

    public PlayerObject(string itemId, ItemDefinition definition, int tileX, int tileY,
        Dictionary<string, object> properties = null)
        : base(itemId, definition, tileX, tileY, properties, isInteractable: true)
    {
    }
}
