using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace FarmGame.World;

public class MapData
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int Width { get; set; }
    public int Height { get; set; }

    [YamlMember(Alias = "player_start")]
    public List<int> PlayerStart { get; set; } = new();

    [YamlMember(Alias = "terrain_colors")]
    public Dictionary<string, List<int>> TerrainColors { get; set; } = new();

    [YamlMember(Alias = "object_colors")]
    public Dictionary<string, List<int>> ObjectColors { get; set; } = new();

    [YamlMember(Alias = "default_terrain")]
    public string DefaultTerrain { get; set; } = "grass";

    public List<TerrainEntry> Terrain { get; set; } = new();
    public List<ObjectEntry> Objects { get; set; } = new();
}

public class TerrainEntry
{
    public string Type { get; set; } = "";
    public List<RegionRect> Regions { get; set; } = new();
}

public class ObjectEntry
{
    public string Type { get; set; } = "";
    public List<RegionRect> Regions { get; set; } = new();
}

public class RegionRect
{
    public int X { get; set; }
    public int Y { get; set; }
    public int W { get; set; }
    public int H { get; set; }
}

public static class MapLoader
{
    public static (TileMap map, Point playerStart) Load(string yamlPath)
    {
        var yaml = File.ReadAllText(yamlPath);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        var data = deserializer.Deserialize<MapData>(yaml);

        var terrainColors = new Dictionary<TerrainType, Color>();
        foreach (var (key, rgb) in data.TerrainColors)
        {
            if (Enum.TryParse<TerrainType>(key, ignoreCase: true, out var terrainType))
                terrainColors[terrainType] = new Color(rgb[0], rgb[1], rgb[2]);
        }

        var objectColors = new Dictionary<ObjectType, Color>();
        foreach (var (key, rgb) in data.ObjectColors)
        {
            if (Enum.TryParse<ObjectType>(key, ignoreCase: true, out var objectType))
                objectColors[objectType] = new Color(rgb[0], rgb[1], rgb[2]);
        }

        Enum.TryParse<TerrainType>(data.DefaultTerrain, ignoreCase: true, out var defaultTerrain);

        var map = new TileMap(data.Width, data.Height, terrainColors, objectColors);

        // Fill default terrain
        for (int x = 0; x < data.Width; x++)
            for (int y = 0; y < data.Height; y++)
                map.SetTerrain(x, y, defaultTerrain);

        // Apply terrain regions
        foreach (var entry in data.Terrain)
        {
            if (!Enum.TryParse<TerrainType>(entry.Type, ignoreCase: true, out var type))
                continue;

            foreach (var r in entry.Regions)
            {
                for (int x = r.X; x < r.X + r.W; x++)
                    for (int y = r.Y; y < r.Y + r.H; y++)
                        map.SetTerrain(x, y, type);
            }
        }

        // Apply object placements
        foreach (var entry in data.Objects)
        {
            if (!Enum.TryParse<ObjectType>(entry.Type, ignoreCase: true, out var type))
                continue;

            foreach (var r in entry.Regions)
            {
                for (int x = r.X; x < r.X + r.W; x++)
                    for (int y = r.Y; y < r.Y + r.H; y++)
                        map.SetObject(x, y, type);
            }
        }

        var playerStart = new Point(data.PlayerStart[0], data.PlayerStart[1]);
        return (map, playerStart);
    }
}
