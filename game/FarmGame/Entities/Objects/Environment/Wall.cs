using System.Collections.Generic;
using FarmGame.Data;

namespace FarmGame.Entities.Objects.Environment;

/// <summary>
/// Wall / barrier world object. Typically not interactable.
/// Acts as a collidable obstacle blocking movement.
/// </summary>
public class Wall : BaseObject
{
    public Wall(string itemId, ItemDefinition definition, int tileX, int tileY,
        Dictionary<string, object> properties = null)
        : base(itemId, definition, tileX, tileY, properties, isInteractable: false)
    {
    }
}
