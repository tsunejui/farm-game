using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FarmGame.Core;
using FarmGame.Data;

namespace FarmGame.World;

public static class MapBuilder
{
    public static GameMap Build(MapDefinition mapDef, DataRegistry registry,
        Func<string, Texture2D> loadTexture = null)
    {
        var config = mapDef.Config;

        // Build terrain color dictionary from registry
        var terrainColors = new Dictionary<string, Color>();
        foreach (var (id, def) in registry.Terrains)
        {
            terrainColors[id] = ColorHelper.FromHex(def.Visuals.Color);
        }

        var map = new GameMap(mapDef.Metadata.MapId, config.Width, config.Height, terrainColors);

        // Fill default terrain
        for (int x = 0; x < config.Width; x++)
            for (int y = 0; y < config.Height; y++)
                map.SetTerrain(x, y, config.DefaultTerrain);

        // Apply default terrain properties from definition
        if (registry.Terrains.TryGetValue(config.DefaultTerrain, out var defaultTerrainDef))
        {
            if (defaultTerrainDef.Properties.Count > 0)
            {
                for (int x = 0; x < config.Width; x++)
                    for (int y = 0; y < config.Height; y++)
                        foreach (var (propName, propValue) in defaultTerrainDef.Properties)
                            map.SetTileProperty(x, y, propName, propValue);
            }
        }

        // Apply terrain placements
        foreach (var placement in mapDef.Terrains)
        {
            var terrainId = placement.Terrain;
            if (!registry.Terrains.ContainsKey(terrainId))
            {
                Console.WriteLine($"Warning: unknown terrain '{terrainId}' in map '{mapDef.Metadata.MapId}'");
                continue;
            }

            var terrainDef = registry.Terrains[terrainId];

            foreach (var r in placement.Regions)
            {
                for (int x = r.X; x < r.X + r.W; x++)
                    for (int y = r.Y; y < r.Y + r.H; y++)
                    {
                        map.SetTerrain(x, y, terrainId);

                        // Apply properties from terrain definition
                        foreach (var (propName, propValue) in terrainDef.Properties)
                            map.SetTileProperty(x, y, propName, propValue);

                        // Apply instance-specific property overrides
                        if (placement.Properties != null)
                            foreach (var (propName, propValue) in placement.Properties)
                                map.SetTileProperty(x, y, propName, propValue);
                    }
            }
        }

        // Place entities
        foreach (var placement in mapDef.Entities)
        {
            if (!registry.Items.TryGetValue(placement.Item, out var itemDef))
            {
                Console.WriteLine($"Warning: unknown item '{placement.Item}' in map '{mapDef.Metadata.MapId}'");
                continue;
            }

            var entity = new EntityInstance(
                placement.Item, itemDef,
                placement.TileX, placement.TileY,
                placement.Properties);

            map.RegisterEntity(entity);

            // Apply collision grid
            if (itemDef.Physics.IsCollidable)
            {
                for (int x = placement.TileX; x < placement.TileX + entity.EffectiveWidth; x++)
                    for (int y = placement.TileY; y < placement.TileY + entity.EffectiveHeight; y++)
                        map.SetCollision(x, y, true);
            }

            // Load background texture if enabled
            if (itemDef.Visuals.Background.Enabled && loadTexture != null)
            {
                var imagePath = itemDef.Visuals.Background.ImagePath;
                if (!string.IsNullOrEmpty(imagePath))
                {
                    try
                    {
                        var texture = loadTexture(imagePath);
                        if (texture != null)
                            map.SetBackgroundTexture(placement.Item, texture);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: failed to load background '{imagePath}' for '{placement.Item}': {ex.Message}");
                    }
                }
            }
        }

        return map;
    }
}
