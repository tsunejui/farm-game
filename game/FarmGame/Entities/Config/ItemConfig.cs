using FarmGame.Data;

namespace FarmGame.Entities.Config;

/// <summary>
/// Wraps an item definition (Items/*.yaml).
/// Keyed by item_id.
/// </summary>
public class ItemConfig
{
    public string Id => Data.Metadata.ItemId;
    public ItemDefinition Data { get; set; }

    public ItemConfig(ItemDefinition data)
    {
        Data = data;
    }
}
