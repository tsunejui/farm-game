using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace FarmGame.Data;

public class MapDefinition
{
    public MapMetadata Metadata { get; set; } = new();
    public MapConfig Config { get; set; } = new();
    public List<TerrainPlacement> Terrains { get; set; } = new();
    public List<EntityPlacement> Entities { get; set; } = new();
}

public class MapMetadata
{
    [YamlMember(Alias = "map_id")]
    public string MapId { get; set; } = "";

    [YamlMember(Alias = "display_name")]
    public string DisplayName { get; set; } = "";
}

public class MapConfig
{
    public int Width { get; set; }
    public int Height { get; set; }

    [YamlMember(Alias = "tile_size")]
    public int TileSize { get; set; } = 32;

    [YamlMember(Alias = "default_terrain")]
    public string DefaultTerrain { get; set; } = "grass";

    [YamlMember(Alias = "player_start")]
    public List<int> PlayerStart { get; set; } = new();
}

public class TerrainPlacement
{
    public string Terrain { get; set; } = "";
    public Dictionary<string, object> Properties { get; set; } = new();
    public List<RegionRect> Regions { get; set; } = new();
}

public class RegionRect
{
    public int X { get; set; }
    public int Y { get; set; }
    public int W { get; set; }
    public int H { get; set; }
}

public class EntityPlacement
{
    public string Item { get; set; } = "";

    [YamlMember(Alias = "tile_x")]
    public int TileX { get; set; }

    [YamlMember(Alias = "tile_y")]
    public int TileY { get; set; }

    public Dictionary<string, object> Properties { get; set; } = new();
}
