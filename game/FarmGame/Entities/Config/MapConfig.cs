using FarmGame.Data;

namespace FarmGame.Entities.Config;

/// <summary>
/// Wraps a map definition (Maps/*.yaml).
/// Keyed by map_id.
/// </summary>
public class MapConfigEntry
{
    public string Id => Data.Metadata.MapId;
    public MapDefinition Data { get; set; }

    public MapConfigEntry(MapDefinition data)
    {
        Data = data;
    }
}
