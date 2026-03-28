using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace FarmGame.Data;

public class DataRegistry
{
    public Dictionary<string, TerrainDefinition> Terrains { get; } = new();
    public Dictionary<string, ItemDefinition> Items { get; } = new();
    public Dictionary<string, MapDefinition> Maps { get; } = new();

    public static DataRegistry LoadAll(string contentDir)
    {
        var registry = new DataRegistry();
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        // Load terrain definitions
        var terrainsDir = Path.Combine(contentDir, "Terrains");
        if (Directory.Exists(terrainsDir))
        {
            foreach (var file in Directory.GetFiles(terrainsDir, "*.yaml"))
            {
                var yaml = File.ReadAllText(file);
                var terrain = deserializer.Deserialize<TerrainDefinition>(yaml);
                registry.Terrains[terrain.Metadata.TerrainId] = terrain;
            }
        }

        // Load item definitions
        var itemsDir = Path.Combine(contentDir, "Items");
        if (Directory.Exists(itemsDir))
        {
            foreach (var file in Directory.GetFiles(itemsDir, "*.yaml"))
            {
                var yaml = File.ReadAllText(file);
                var item = deserializer.Deserialize<ItemDefinition>(yaml);
                registry.Items[item.Metadata.ItemId] = item;
            }
        }

        // Load map definitions
        var mapsDir = Path.Combine(contentDir, "Maps");
        if (Directory.Exists(mapsDir))
        {
            foreach (var file in Directory.GetFiles(mapsDir, "*.yaml"))
            {
                var yaml = File.ReadAllText(file);
                var map = deserializer.Deserialize<MapDefinition>(yaml);
                registry.Maps[map.Metadata.MapId] = map;
            }
        }

        Console.WriteLine($"DataRegistry: loaded {registry.Terrains.Count} terrains, {registry.Items.Count} items, {registry.Maps.Count} maps");
        return registry;
    }
}
