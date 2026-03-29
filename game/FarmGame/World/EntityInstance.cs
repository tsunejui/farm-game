using System;
using System.Collections.Generic;
using FarmGame.Data;

namespace FarmGame.World;

public class EntityInstance
{
    public string ItemId { get; }
    public ItemDefinition Definition { get; }
    public int TileX { get; set; }
    public int TileY { get; set; }
    public Dictionary<string, object> Properties { get; }

    // Pre-computed effective occupy dimensions (considers fill_width/fill_height overrides)
    public int EffectiveWidth { get; }
    public int EffectiveHeight { get; }

    // Runtime mutable state (HP, alive/dead, damage flash)
    public EntityState State { get; private set; }

    // Unique instance ID for persistence (set when saving to / loading from DB)
    public string InstanceId { get; set; }

    public EntityInstance(string itemId, ItemDefinition definition, int tileX, int tileY,
        Dictionary<string, object> properties)
    {
        ItemId = itemId;
        Definition = definition;
        TileX = tileX;
        TileY = tileY;
        Properties = properties ?? new Dictionary<string, object>();

        // Compute effective size once at construction
        EffectiveWidth = definition.Physics.OccupyWidth;
        EffectiveHeight = definition.Physics.OccupyHeight;
        if (Properties.TryGetValue("fill_width", out var fw))
            EffectiveWidth = Convert.ToInt32(fw);
        if (Properties.TryGetValue("fill_height", out var fh))
            EffectiveHeight = Convert.ToInt32(fh);

        // Initialize runtime combat state from definition
        var faction = EntityState.ParseFaction(definition.Logic.Faction);
        int maxHp = definition.Logic.MaxHealth;
        State = new EntityState(maxHp > 0 ? maxHp : 1, faction);
    }

    // Restore state from persisted data (used when loading map state from DB)
    public void RestoreState(int currentHp)
    {
        var faction = EntityState.ParseFaction(Definition.Logic.Faction);
        int maxHp = Definition.Logic.MaxHealth;
        State = new EntityState(maxHp > 0 ? maxHp : 1, currentHp, faction);
    }
}
