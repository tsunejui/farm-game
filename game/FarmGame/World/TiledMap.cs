using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FarmGame.World;

/// <summary>
/// Data classes for Tiled JSON map format (exported with --embed-tilesets).
/// </summary>
public class TiledMapData
{
    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("tilewidth")]
    public int TileWidth { get; set; }

    [JsonPropertyName("tileheight")]
    public int TileHeight { get; set; }

    [JsonPropertyName("tilesets")]
    public List<TiledTileset> Tilesets { get; set; } = new();

    [JsonPropertyName("layers")]
    public List<TiledLayer> Layers { get; set; } = new();
}

public class TiledTileset
{
    [JsonPropertyName("firstgid")]
    public int FirstGid { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("tilecount")]
    public int TileCount { get; set; }

    [JsonPropertyName("tiles")]
    public List<TiledTile> Tiles { get; set; } = new();
}

public class TiledTile
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("properties")]
    public List<TiledProperty> Properties { get; set; } = new();
}

public class TiledProperty
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "string";

    [JsonPropertyName("value")]
    public object Value { get; set; } = "";
}

public class TiledLayer
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("data")]
    public List<int> Data { get; set; } = new();

    [JsonPropertyName("objects")]
    public List<TiledObject> Objects { get; set; } = new();
}

public class TiledObject
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("x")]
    public float X { get; set; }

    [JsonPropertyName("y")]
    public float Y { get; set; }
}
