using System.Collections.Generic;
using FarmGame.Data;

namespace FarmGame.World;

public class EntityInstance
{
    public string ItemId { get; }
    public ItemDefinition Definition { get; }
    public int TileX { get; }
    public int TileY { get; }
    public Dictionary<string, object> Properties { get; }

    public EntityInstance(string itemId, ItemDefinition definition, int tileX, int tileY,
        Dictionary<string, object> properties)
    {
        ItemId = itemId;
        Definition = definition;
        TileX = tileX;
        TileY = tileY;
        Properties = properties ?? new Dictionary<string, object>();
    }
}
