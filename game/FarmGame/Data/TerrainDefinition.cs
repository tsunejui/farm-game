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
}
