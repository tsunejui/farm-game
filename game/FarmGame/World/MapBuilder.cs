using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Serilog;
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
                Log.Warning("Unknown terrain '{TerrainId}' in map '{MapId}'", terrainId, mapDef.Metadata.MapId);
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
                Log.Warning("Unknown item '{ItemId}' in map '{MapId}'", placement.Item, mapDef.Metadata.MapId);
                continue;
            }

            var entity = new EntityInstance(
                placement.Item, itemDef,
                placement.TileX, placement.TileY,
                placement.Properties);

            map.RegisterEntity(entity);
            Log.Debug("Entity created: {ItemId} at ({X},{Y}), hp={Hp}, faction={Faction}, size={W}x{H}",
                entity.ItemId, entity.TileX, entity.TileY,
                entity.State.MaxHp, entity.State.Faction,
                entity.EffectiveWidth, entity.EffectiveHeight);

            // Apply collision grid
            if (itemDef.Physics.IsCollidable)
            {
                for (int x = placement.TileX; x < placement.TileX + entity.EffectiveWidth; x++)
                    for (int y = placement.TileY; y < placement.TileY + entity.EffectiveHeight; y++)
                        map.SetCollision(x, y, true);
            }

            // Load all state-based background textures
            if (itemDef.Visuals.Background.Enabled && loadTexture != null)
            {
                var bg = itemDef.Visuals.Background;
                foreach (var (state, stateConfig) in bg.States)
                {
                    if (string.IsNullOrEmpty(stateConfig.ImagePath)) continue;

                    // Map "normal" state to internal "alive" key for renderer lookup
                    string texKey = state == "normal" ? "alive" : state;
                    try
                    {
                        var texture = loadTexture(stateConfig.ImagePath);
                        if (texture != null)
                            map.SetBackgroundTexture(placement.Item, texKey, texture);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning("Failed to load {State} background '{ImagePath}' for '{ItemId}': {Error}",
                            state, stateConfig.ImagePath, placement.Item, ex.Message);
                    }
                }
            }
        }

        return map;
    }
}
