using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace FarmGame.Data;

public class ItemDefinition
{
    public ItemMetadata Metadata { get; set; } = new();
    public ItemVisuals Visuals { get; set; } = new();
    public ItemPhysics Physics { get; set; } = new();
    public ItemLogic Logic { get; set; } = new();
}

public class ItemMetadata
{
    [YamlMember(Alias = "item_id")]
    public string ItemId { get; set; } = "";

    [YamlMember(Alias = "display_name")]
    public string DisplayName { get; set; } = "";

    public string Category { get; set; } = "";
}

public class ItemVisuals
{
    [YamlMember(Alias = "texture_path")]
    public string TexturePath { get; set; } = "";

    public string Color { get; set; } = "";

    [YamlMember(Alias = "origin_point")]
    public string OriginPoint { get; set; } = "top_left";

    public ItemBackground Background { get; set; } = new();
}

public class ItemBackground
{
    public bool Enabled { get; set; }

    // "stretch" = scale to fill occupy area
    // "tile"    = repeat pattern across occupy area
    // "center"  = draw once at center, no scaling
    [YamlMember(Alias = "display_mode")]
    public string DisplayMode { get; set; } = "stretch";

    // Optional offset in pixels from top-left of the entity area
    [YamlMember(Alias = "offset_x")]
    public int OffsetX { get; set; }

    [YamlMember(Alias = "offset_y")]
    public int OffsetY { get; set; }

    // State-specific images: "normal" (alive default), "dead", etc.
    // Each state maps to its own image_path.
    public Dictionary<string, ItemBackgroundState> States { get; set; } = new();

    // Helper: get the image path for the "normal" state (backward compat)
    [YamlIgnore]
    public string NormalImagePath =>
        States.TryGetValue("normal", out var s) ? s.ImagePath : "";
}

public class ItemBackgroundState
{
    [YamlMember(Alias = "image_path")]
    public string ImagePath { get; set; } = "";

    // Optional per-state display_mode override (null = inherit from parent)
    [YamlMember(Alias = "display_mode")]
    public string DisplayMode { get; set; }

    [YamlMember(Alias = "offset_x")]
    public int? OffsetX { get; set; }

    [YamlMember(Alias = "offset_y")]
    public int? OffsetY { get; set; }
}

public class ItemPhysics
{
    [YamlMember(Alias = "occupy_width")]
    public int OccupyWidth { get; set; } = 1;

    [YamlMember(Alias = "occupy_height")]
    public int OccupyHeight { get; set; } = 1;

    [YamlMember(Alias = "is_collidable")]
    public bool IsCollidable { get; set; }
}

public class ItemLogic
{
    [YamlMember(Alias = "action_handler")]
    public string ActionHandler { get; set; } = "none";

    [YamlMember(Alias = "max_health")]
    public int MaxHealth { get; set; }

    public float Defense { get; set; }

    // Faction: "neutral" (default), "friendly" (cannot be attacked), "enemy" (can be attacked)
    public string Faction { get; set; } = "neutral";

    public List<DropEntry> Drops { get; set; } = new();
    public Dictionary<string, object> Properties { get; set; } = new();
}

public class DropEntry
{
    public string Item { get; set; } = "";
    public int Amount { get; set; }
}
