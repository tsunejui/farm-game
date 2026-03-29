// =============================================================================
// EffectRegistry.cs — Maps effect IDs to IEffect instances and YAML definitions
// =============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using Serilog;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using FarmGame.Data;

namespace FarmGame.World.Effects;

public static class EffectRegistry
{
    private static readonly Dictionary<string, IEffect> _effects = new();
    private static readonly Dictionary<string, EffectDefinition> _definitions = new();

    static EffectRegistry()
    {
        Register(new IndestructibleEffect());
        Register(new HighTemperatureEffect());
        Register(new RestEffect());
    }

    public static void Register(IEffect effect)
    {
        _effects[effect.Id] = effect;
    }

    public static IEffect Get(string id)
    {
        return _effects.GetValueOrDefault(id);
    }

    public static EffectDefinition GetDefinition(string id)
    {
        return _definitions.GetValueOrDefault(id);
    }

    public static bool Exists(string id) => _effects.ContainsKey(id);

    // Load YAML definitions + textures from Content/Effects/
    public static void LoadDefinitions(string contentDir, Func<string, Texture2D> loadTexture)
    {
        var effectsDir = Path.Combine(contentDir, "Effects");
        Log.Information("[EffectRegistry] Loading definitions from: {Dir}", effectsDir);
        if (!Directory.Exists(effectsDir))
        {
            Log.Warning("[EffectRegistry] Effects directory not found!");
            return;
        }

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        foreach (var file in Directory.GetFiles(effectsDir, "*.yaml"))
        {
            try
            {
                var yaml = File.ReadAllText(file);
                var def = deserializer.Deserialize<EffectDefinition>(yaml);
                if (string.IsNullOrEmpty(def.EffectId)) continue;

                // Load icon texture
                if (!string.IsNullOrEmpty(def.ImagePath) && loadTexture != null)
                {
                    try
                    {
                        def.Texture = loadTexture(def.ImagePath);
                        Log.Debug("Effect icon loaded: {Id} → {Path}", def.EffectId, def.ImagePath);
                    }
                    catch (Exception texEx)
                    {
                        Log.Warning("Failed to load effect icon '{Path}': {Error}",
                            def.ImagePath, texEx.Message);
                    }
                }

                _definitions[def.EffectId] = def;
                Log.Information("[EffectRegistry] Loaded: {Id}, texture={HasTex}, path={Path}",
                    def.EffectId, def.Texture != null, def.ImagePath);
            }
            catch (Exception ex)
            {
                Log.Warning("Failed to load effect YAML {File}: {Error}", file, ex.Message);
            }
        }
    }
}
