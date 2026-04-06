using System.Collections.Generic;
using FarmGame.Data;

namespace FarmGame.Entities.Objects.Items;

/// <summary>
/// Weapon item in the world (e.g. sword on the ground, chest with weapon).
/// Interactable: player can pick up or interact with it.
/// </summary>
public class Weapon : BaseObject
{
    /// <summary>Bonus attack power this weapon provides when equipped.</summary>
    public float AttackPower { get; }

    public Weapon(string itemId, ItemDefinition definition, int tileX, int tileY,
        Dictionary<string, object> properties = null)
        : base(itemId, definition, tileX, tileY, properties, isInteractable: true)
    {
        if (Properties.TryGetValue("attack_power", out var ap))
            AttackPower = System.Convert.ToSingle(ap);
    }
}
