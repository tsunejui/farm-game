using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Xna.Framework;

namespace FarmGame.World;

public static class TiledMapLoader
{
    public static (TileMap map, Point playerStart) Load(string jsonPath)
    {
        var json = File.ReadAllText(jsonPath);
        var data = JsonSerializer.Deserialize<TiledMapData>(json)
            ?? throw new InvalidOperationException($"Failed to deserialize {jsonPath}");

        var tileset = data.Tilesets.First();

        // Build tile ID → metadata lookup (tile ID is local, add firstgid for global)
        var tileMeta = new Dictionary<int, TileMeta>();
        foreach (var tile in tileset.Tiles)
        {
            var props = tile.Properties.ToDictionary(p => p.Name, p => p.Value?.ToString() ?? "");
            var gid = tile.Id + tileset.FirstGid;

            tileMeta[gid] = new TileMeta
            {
                Layer = props.GetValueOrDefault("layer", ""),
                Type = props.GetValueOrDefault("type", ""),
                Color = ParseColor(props.GetValueOrDefault("color", "")),
                CustomProperties = tile.Properties
                    .Where(p => p.Name != "type" && p.Name != "layer" && p.Name != "color")
                    .ToDictionary(p => p.Name, p => ParsePropertyValue(p)),
            };
        }

        // Build color dictionaries from tileset metadata
        var terrainColors = new Dictionary<TerrainType, Color>();
        var objectColors = new Dictionary<ObjectType, Color>();
        foreach (var (_, meta) in tileMeta)
        {
            if (meta.Layer == "terrain" && Enum.TryParse<TerrainType>(meta.Type, true, out var tt))
                terrainColors.TryAdd(tt, meta.Color);
            else if (meta.Layer == "object" && Enum.TryParse<ObjectType>(meta.Type, true, out var ot))
                objectColors.TryAdd(ot, meta.Color);
        }

        var map = new TileMap(data.Width, data.Height, terrainColors, objectColors);

        // Process tile layers
        foreach (var layer in data.Layers.Where(l => l.Type == "tilelayer"))
        {
            for (int i = 0; i < layer.Data.Count; i++)
            {
                int gid = layer.Data[i];
                if (gid == 0) continue;

                int x = i % layer.Width;
                int y = i / layer.Width;

                if (!tileMeta.TryGetValue(gid, out var meta)) continue;

                if (meta.Layer == "terrain" && Enum.TryParse<TerrainType>(meta.Type, true, out var terrainType))
                {
                    map.SetTerrain(x, y, terrainType);
                }
                else if (meta.Layer == "object" && Enum.TryParse<ObjectType>(meta.Type, true, out var objectType))
                {
                    map.SetObject(x, y, objectType);
                }

                // Apply custom properties to tile
                foreach (var (propName, propValue) in meta.CustomProperties)
                {
                    map.SetTileProperty(x, y, propName, propValue);
                }
            }
        }

        // Extract player start from object group
        var playerStart = Point.Zero;
        var spawnsLayer = data.Layers.FirstOrDefault(l => l.Type == "objectgroup" && l.Name == "spawns");
        if (spawnsLayer != null)
        {
            var spawnObj = spawnsLayer.Objects.FirstOrDefault(o => o.Name == "player_start");
            if (spawnObj != null)
            {
                playerStart = new Point(
                    (int)(spawnObj.X / data.TileWidth),
                    (int)(spawnObj.Y / data.TileHeight));
            }
        }

        return (map, playerStart);
    }

    private static Color ParseColor(string colorStr)
    {
        if (string.IsNullOrEmpty(colorStr)) return Color.Magenta;
        var parts = colorStr.Split(',');
        if (parts.Length != 3) return Color.Magenta;
        return new Color(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]));
    }

    private static object ParsePropertyValue(TiledProperty prop)
    {
        return prop.Type switch
        {
            "bool" => prop.Value is JsonElement je ? je.GetBoolean() : bool.Parse(prop.Value?.ToString() ?? "false"),
            "int" => prop.Value is JsonElement ji ? ji.GetInt32() : int.Parse(prop.Value?.ToString() ?? "0"),
            "float" => prop.Value is JsonElement jf ? jf.GetSingle() : float.Parse(prop.Value?.ToString() ?? "0"),
            _ => prop.Value?.ToString() ?? "",
        };
    }

    private class TileMeta
    {
        public string Layer { get; set; } = "";
        public string Type { get; set; } = "";
        public Color Color { get; set; }
        public Dictionary<string, object> CustomProperties { get; set; } = new();
    }
}
