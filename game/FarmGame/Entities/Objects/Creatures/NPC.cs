using System.Collections.Generic;
using FarmGame.Data;

namespace FarmGame.Entities.Objects.Creatures;

/// <summary>
/// Non-player character. Interactable by default (dialogue, quests).
/// May have AI movement via CreatureBrain, but is typically friendly.
/// </summary>
public class NPC : BaseObject
{
    /// <summary>Movement speed (tiles per second). 0 = stationary.</summary>
    public float MoveSpeed => Definition.Logic.MoveSpeed;

    public NPC(string itemId, ItemDefinition definition, int tileX, int tileY,
        Dictionary<string, object> properties = null)
        : base(itemId, definition, tileX, tileY, properties, isInteractable: true)
    {
    }
}
