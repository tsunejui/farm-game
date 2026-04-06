using FarmGame.Data;

namespace FarmGame.Entities.Config;

/// <summary>
/// Wraps a terrain definition (Terrains/*.yaml).
/// Keyed by terrain_id.
/// </summary>
public class TerrainConfig
{
    public string Id => Data.Metadata.TerrainId;
    public TerrainDefinition Data { get; set; }

    public TerrainConfig(TerrainDefinition data)
    {
        Data = data;
    }
}
