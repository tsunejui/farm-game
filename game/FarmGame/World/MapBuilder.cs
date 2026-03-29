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
        Func<string, Texture2D> loadTexture = null,
        GraphicsDevice graphicsDevice = null, string contentDir = null)
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

                        // Block movement on water terrain
                        if (terrainDef.Properties.ContainsKey("is_water"))
                            map.SetCollision(x, y, true);

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

        // Load terrain base textures (static and animated)
        if (loadTexture != null)
        {
            foreach (var (terrainId, terrainDef) in registry.Terrains)
            {
                string basePath = $"Images/terrain_bases/{terrainId}_base";

                // Try animated first (e.g. water)
                if (graphicsDevice != null && contentDir != null)
                {
                    try
                    {
                        var anim = AnimatedTexture.LoadFrames(graphicsDevice, contentDir, basePath, 500f);
                        if (anim.FrameCount > 0)
                        {
                            map.SetTerrainAnimBaseTexture(terrainId, anim);
                            continue;
                        }
                    }
                    catch { }
                }

                try
                {
                    var tex = loadTexture(basePath);
                    if (tex != null)
                        map.SetTerrainBaseTexture(terrainId, tex);
                }
                catch
                {
                    // No base texture for this terrain — color fill will be used
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

            var obj = new WorldObject(
                placement.Item, itemDef,
                placement.TileX, placement.TileY,
                placement.Properties);

            map.RegisterObject(obj);
            Log.Debug("Entity created: {ItemId} at ({X},{Y}), hp={Hp}, faction={Faction}, size={W}x{H}",
                obj.ItemId, obj.TileX, obj.TileY,
                obj.State.MaxHp, obj.State.Faction,
                obj.EffectiveWidth, obj.EffectiveHeight);

            // Apply collision grid
            if (itemDef.Physics.IsCollidable)
            {
                for (int x = placement.TileX; x < placement.TileX + obj.EffectiveWidth; x++)
                    for (int y = placement.TileY; y < placement.TileY + obj.EffectiveHeight; y++)
                        map.SetCollision(x, y, true);
            }

            // Load all state-based background textures (PNG or GIF frames)
            if (itemDef.Visuals.Background.Enabled && loadTexture != null)
            {
                var bg = itemDef.Visuals.Background;
                foreach (var (state, stateConfig) in bg.States)
                {
                    if (string.IsNullOrEmpty(stateConfig.ImagePath)) continue;

                    string texKey = state == "normal" ? "alive" : state;
                    string fileType = (stateConfig.FileType ?? "png").ToLowerInvariant();

                    try
                    {
                        if (fileType == "gif" && graphicsDevice != null && contentDir != null)
                        {
                            // Load animated frames: imagePath_frame0.png, _frame1.png, ...
                            var anim = AnimatedTexture.LoadFrames(
                                graphicsDevice, contentDir, stateConfig.ImagePath,
                                stateConfig.FrameDelay);
                            if (anim.FrameCount > 0)
                                map.SetAnimatedTexture(placement.Item, texKey, anim);
                        }
                        else
                        {
                            var texture = loadTexture(stateConfig.ImagePath);
                            if (texture != null)
                                map.SetBackgroundTexture(placement.Item, texKey, texture);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warning("Failed to load {State} background '{ImagePath}' for '{ItemId}': {Error}",
                            state, stateConfig.ImagePath, placement.Item, ex.Message);
                    }
                }
            }
        }

        // Load terrain decorations and assign randomly
        var coverages = new Dictionary<string, List<float>>();
        foreach (var (terrainId, terrainDef) in registry.Terrains)
        {
            if (terrainDef.Visuals.Decorations.Count == 0) continue;

            var rates = new List<float>();
            foreach (var deco in terrainDef.Visuals.Decorations)
            {
                if (string.IsNullOrEmpty(deco.ImagePath)) continue;
                string fileType = (deco.FileType ?? "png").ToLowerInvariant();

                try
                {
                    if (fileType == "gif" && graphicsDevice != null && contentDir != null)
                    {
                        var anim = AnimatedTexture.LoadFrames(graphicsDevice, contentDir, deco.ImagePath);
                        if (anim.FrameCount > 0)
                            map.AddTerrainAnimDecoration(terrainId, anim);
                    }
                    else if (loadTexture != null)
                    {
                        var tex = loadTexture(deco.ImagePath);
                        if (tex != null)
                            map.AddTerrainDecoration(terrainId, tex);
                    }
                    rates.Add(deco.Coverage);
                }
                catch (Exception ex)
                {
                    Log.Warning("Failed to load decoration '{Path}': {Error}", deco.ImagePath, ex.Message);
                    rates.Add(0f);
                }
            }

            if (rates.Count > 0)
                coverages[terrainId] = rates;
        }

        if (coverages.Count > 0)
            map.AssignDecorations(coverages);

        return map;
    }
}
