// =============================================================================
// ConfigManager.cs — Centralized configuration management
//
// Reads all YAML config files at initialization and stores them as typed
// config entities. Supports reload, update, and delete operations.
//
// Usage:
//   var config = new ConfigManager();
//   config.Initialize(configsDir);              // Load all configs
//   config.GetTerrain("grass");                 // Lookup by ID
//   config.Reload(configsDir);                  // Re-read all from disk
//   config.UpdateItem("sword", newDef);         // Update in memory
//   config.RemoveItem("sword");                 // Remove from registry
// =============================================================================

using System.Collections.Generic;
using System.IO;
using FarmGame.Data;
using FarmGame.Entities.Config;
using Serilog;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace FarmGame.Core;

public class ConfigManager
{
    private readonly IDeserializer _deserializer;

    // ─── Config Stores ──────────────────────────────────────

    public GameSettings GameSettings { get; private set; }
    public Dictionary<string, TerrainConfig> Terrains { get; } = new();
    public Dictionary<string, ItemConfig> Items { get; } = new();
    public Dictionary<string, MapConfigEntry> Maps { get; } = new();
    public Dictionary<string, EffectConfig> Effects { get; } = new();

    public ConfigManager()
    {
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();
    }

    // ─── Initialize ─────────────────────────────────────────

    /// <summary>
    /// Load all config files from the given Configs directory.
    /// Populates GameSettings, Terrains, Items, Maps, and Effects.
    /// </summary>
    public void Initialize(string configsDir)
    {
        LoadGameSettings(configsDir);
        LoadTerrains(configsDir);
        LoadItems(configsDir);
        LoadMaps(configsDir);
        LoadEffects(configsDir);

        Log.Information(
            "[ConfigManager] Initialized: {Terrains} terrains, {Items} items, {Maps} maps, {Effects} effects",
            Terrains.Count, Items.Count, Maps.Count, Effects.Count);
    }

    // ─── Reload ─────────────────────────────────────────────

    /// <summary>
    /// Clear all configs and re-read from disk.
    /// </summary>
    public void Reload(string configsDir)
    {
        Terrains.Clear();
        Items.Clear();
        Maps.Clear();
        Effects.Clear();
        GameSettings = null;

        Initialize(configsDir);
        Log.Information("[ConfigManager] Reloaded all configs");
    }

    // ─── GameSettings ───────────────────────────────────────

    private void LoadGameSettings(string configsDir)
    {
        var configPath = Path.Combine(configsDir, "system.yaml");
        if (!File.Exists(configPath)) return;

        var data = _deserializer.Deserialize<GameConfig>(File.ReadAllText(configPath));
        GameSettings = new GameSettings(data);
    }

    public void UpdateGameSettings(GameConfig data)
    {
        GameSettings = new GameSettings(data);
        Log.Debug("[ConfigManager] GameSettings updated");
    }

    // ─── Terrain ────────────────────────────────────────────

    public TerrainConfig GetTerrain(string id) =>
        Terrains.TryGetValue(id, out var cfg) ? cfg : null;

    public void UpdateTerrain(string id, TerrainDefinition data)
    {
        Terrains[id] = new TerrainConfig(data);
        Log.Debug("[ConfigManager] Terrain updated: {Id}", id);
    }

    public bool RemoveTerrain(string id)
    {
        var removed = Terrains.Remove(id);
        if (removed) Log.Debug("[ConfigManager] Terrain removed: {Id}", id);
        return removed;
    }

    private void LoadTerrains(string configsDir)
    {
        var dir = Path.Combine(configsDir, "terrains");
        if (!Directory.Exists(dir)) return;

        foreach (var file in Directory.GetFiles(dir, "*.yaml"))
        {
            var data = _deserializer.Deserialize<TerrainDefinition>(File.ReadAllText(file));
            Terrains[data.Metadata.TerrainId] = new TerrainConfig(data);
        }
    }

    // ─── Item ───────────────────────────────────────────────

    public ItemConfig GetItem(string id) =>
        Items.TryGetValue(id, out var cfg) ? cfg : null;

    public void UpdateItem(string id, ItemDefinition data)
    {
        Items[id] = new ItemConfig(data);
        Log.Debug("[ConfigManager] Item updated: {Id}", id);
    }

    public bool RemoveItem(string id)
    {
        var removed = Items.Remove(id);
        if (removed) Log.Debug("[ConfigManager] Item removed: {Id}", id);
        return removed;
    }

    private void LoadItems(string configsDir)
    {
        var dir = Path.Combine(configsDir, "objects");
        if (!Directory.Exists(dir)) return;

        // Recursively scan all subdirectories (creatures, items, environment)
        foreach (var file in Directory.GetFiles(dir, "*.yaml", SearchOption.AllDirectories))
        {
            var data = _deserializer.Deserialize<ItemDefinition>(File.ReadAllText(file));
            Items[data.Metadata.ItemId] = new ItemConfig(data);
        }
    }

    // ─── Map ────────────────────────────────────────────────

    public MapConfigEntry GetMap(string id) =>
        Maps.TryGetValue(id, out var cfg) ? cfg : null;

    public void UpdateMap(string id, MapDefinition data)
    {
        Maps[id] = new MapConfigEntry(data);
        Log.Debug("[ConfigManager] Map updated: {Id}", id);
    }

    public bool RemoveMap(string id)
    {
        var removed = Maps.Remove(id);
        if (removed) Log.Debug("[ConfigManager] Map removed: {Id}", id);
        return removed;
    }

    private void LoadMaps(string configsDir)
    {
        var dir = Path.Combine(configsDir, "maps");
        if (!Directory.Exists(dir)) return;

        foreach (var file in Directory.GetFiles(dir, "*.yaml"))
        {
            var data = _deserializer.Deserialize<MapDefinition>(File.ReadAllText(file));
            Maps[data.Metadata.MapId] = new MapConfigEntry(data);
        }
    }

    // ─── Effect ─────────────────────────────────────────────

    public EffectConfig GetEffect(string id) =>
        Effects.TryGetValue(id, out var cfg) ? cfg : null;

    public void UpdateEffect(string id, EffectDefinition data)
    {
        Effects[id] = new EffectConfig(data);
        Log.Debug("[ConfigManager] Effect updated: {Id}", id);
    }

    public bool RemoveEffect(string id)
    {
        var removed = Effects.Remove(id);
        if (removed) Log.Debug("[ConfigManager] Effect removed: {Id}", id);
        return removed;
    }

    private void LoadEffects(string configsDir)
    {
        var dir = Path.Combine(configsDir, "effects");
        if (!Directory.Exists(dir)) return;

        foreach (var file in Directory.GetFiles(dir, "*.yaml"))
        {
            var data = _deserializer.Deserialize<EffectDefinition>(File.ReadAllText(file));
            Effects[data.EffectId] = new EffectConfig(data);
        }
    }

    // ─── DataRegistry Compatibility ─────────────────────────

    /// <summary>
    /// Build a DataRegistry from current config state.
    /// Used by systems that still consume DataRegistry (MapBuilder, WorldController).
    /// </summary>
    public DataRegistry ToDataRegistry()
    {
        var registry = new DataRegistry();
        foreach (var kvp in Terrains)
            registry.Terrains[kvp.Key] = kvp.Value.Data;
        foreach (var kvp in Items)
            registry.Items[kvp.Key] = kvp.Value.Data;
        foreach (var kvp in Maps)
            registry.Maps[kvp.Key] = kvp.Value.Data;
        return registry;
    }
}
