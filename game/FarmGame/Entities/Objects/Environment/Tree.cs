using System.Collections.Generic;
using FarmGame.Data;

namespace FarmGame.Entities.Objects.Environment;

/// <summary>
/// Tree world object. Can be interactable (choppable for resources)
/// or purely decorative, determined by the item definition.
/// </summary>
public class Tree : BaseObject
{
    public Tree(string itemId, ItemDefinition definition, int tileX, int tileY,
        Dictionary<string, object> properties = null)
        : base(itemId, definition, tileX, tileY, properties)
    {
    }
}
