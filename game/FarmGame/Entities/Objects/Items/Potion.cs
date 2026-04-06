using System.Collections.Generic;
using FarmGame.Data;

namespace FarmGame.Entities.Objects.Items;

/// <summary>
/// Consumable potion item in the world.
/// Interactable: player can pick up or use it.
/// </summary>
public class Potion : BaseObject
{
    /// <summary>Amount of HP restored on use.</summary>
    public int HealAmount { get; }

    public Potion(string itemId, ItemDefinition definition, int tileX, int tileY,
        Dictionary<string, object> properties = null)
        : base(itemId, definition, tileX, tileY, properties, isInteractable: true)
    {
        if (Properties.TryGetValue("heal_amount", out var ha))
            HealAmount = System.Convert.ToInt32(ha);
    }
}
