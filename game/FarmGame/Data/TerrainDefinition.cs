using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace FarmGame.Data;

public class TerrainDefinition
{
    public TerrainMetadata Metadata { get; set; } = new();
    public TerrainVisuals Visuals { get; set; } = new();
    public Dictionary<string, object> Properties { get; set; } = new();
}

public class TerrainMetadata
{
    [YamlMember(Alias = "terrain_id")]
    public string TerrainId { get; set; } = "";

    [YamlMember(Alias = "display_name")]
    public string DisplayName { get; set; } = "";

    public string Category { get; set; } = "";
}

public class TerrainVisuals
{
    public string Color { get; set; } = "#FF00FF";

    // Decorations: small images randomly placed on terrain tiles
    public List<TerrainDecoration> Decorations { get; set; } = new();
}

public class TerrainDecoration
{
    [YamlMember(Alias = "image_path")]
    public string ImagePath { get; set; } = "";

    // "png" or "gif"
    [YamlMember(Alias = "file_type")]
    public string FileType { get; set; } = "png";

    // Percentage of tiles that get this decoration (0.0 ~ 1.0)
    public float Coverage { get; set; } = 0.05f;
}
