using System;
using System.Collections.Generic;
using FarmGame.Data;
using FarmGame.Entities.Objects;
using FarmGame.World.Interactions;

namespace FarmGame.World;

public enum ObjectCategory
{
    Item,       // Static world objects (rocks, trees, boxes, etc.)
    Creature    // Living entities (player, NPCs, enemies)
}

/// <summary>
/// Runtime entity in the game world. Extends BaseObject to inherit shared logic
/// (state, effects, event queue, damage queue) while preserving backward compatibility
/// with all existing systems (GameMap, MapBuilder, AI, Combat, etc.).
/// </summary>
public class WorldObject : BaseObject
{
    /// <summary>Unique instance ID for persistence (set when saving to / loading from DB).</summary>
    public string InstanceId
    {
        get => Id;
        set => Id = value;
    }

    public WorldObject(string itemId, ItemDefinition definition, int tileX, int tileY,
        Dictionary<string, object> properties)
        : base(itemId, definition, tileX, tileY, properties)
    {
    }

    // ─── Backward-compatible overlap that takes WorldObject ──

    public InteractionRequest UpdateOverlap(WorldObject player, float deltaTime)
    {
        return base.UpdateOverlap((BaseObject)player, deltaTime);
    }

    /// <summary>
    /// Handle interaction trigger. Delegates to InteractionBehavior.Execute().
    /// </summary>
    protected override InteractionRequest OnInteractionTriggered(BaseObject player)
    {
        if (InteractionBehavior == null) return null;

        // InteractionBehavior.Execute expects WorldObject
        if (player is WorldObject wo)
            return InteractionBehavior.Execute(this, wo);

        return null;
    }
}
