using System;
using System.Collections.Generic;
using System.Linq;
using FarmGame.Data;
using FarmGame.World.Effects;
using FarmGame.World.Events;
using FarmGame.World.Interactions;

namespace FarmGame.World;

public enum ObjectCategory
{
    Item,       // Static world objects (rocks, trees, boxes, etc.)
    Creature    // Living entities (player, NPCs, enemies)
}

public class WorldObject
{
    public string ItemId { get; }
    public ItemDefinition Definition { get; }
    public int TileX { get; set; }
    public int TileY { get; set; }
    public Dictionary<string, object> Properties { get; }
    public ObjectCategory Category { get; }

    // Pre-computed effective occupy dimensions (considers fill_width/fill_height overrides)
    public int EffectiveWidth { get; }
    public int EffectiveHeight { get; }

    // Runtime mutable state (HP, alive/dead, damage flash)
    public ObjectState State { get; private set; }

    // Unique instance ID for persistence (set when saving to / loading from DB)
    public string InstanceId { get; set; }

    // Active effects (stackable buffs/debuffs with TTL tracking)
    public List<ActiveEffect> Effects { get; } = new();

    // Internal timer: every 1 second, enqueue a RefreshEffectsEvent
    private float _effectRefreshTimer;
    private const float EffectRefreshIntervalSeconds = 1f;

    // Interaction behavior (teleport, dialogue, etc.) — null if not interactable
    public IInteractionBehavior InteractionBehavior { get; set; }

    // Overlap detection for interaction trigger
    private float _overlapTimer;
    public bool IsTriggered { get; private set; }
    public float OverlapProgress => InteractionBehavior != null
        ? Math.Clamp(_overlapTimer / InteractionBehavior.ChargeTime, 0f, 1f) : 0f;

    // Event queue — events are processed one at a time per frame
    private readonly Queue<IObjectEvent> _eventQueue = new();
    private IObjectEvent _currentEvent;

    public WorldObject(string itemId, ItemDefinition definition, int tileX, int tileY,
        Dictionary<string, object> properties)
    {
        ItemId = itemId;
        Definition = definition;
        TileX = tileX;
        TileY = tileY;
        Properties = properties ?? new Dictionary<string, object>();
        Category = ParseCategory(definition.Metadata.Category);

        // Compute effective size once at construction
        EffectiveWidth = definition.Physics.OccupyWidth;
        EffectiveHeight = definition.Physics.OccupyHeight;
        if (Properties.TryGetValue("fill_width", out var fw))
            EffectiveWidth = Convert.ToInt32(fw);
        if (Properties.TryGetValue("fill_height", out var fh))
            EffectiveHeight = Convert.ToInt32(fh);

        // Initialize runtime combat state from definition
        var faction = ObjectState.ParseFaction(definition.Logic.Faction);
        int maxHp = definition.Logic.MaxHealth;
        State = new ObjectState(maxHp > 0 ? maxHp : 1, faction);

        // Set up interaction behavior from YAML config
        InitInteractionBehavior();
    }

    private void InitInteractionBehavior()
    {
        var logic = Definition.Logic;
        if (string.IsNullOrEmpty(logic.InteractionBehavior) || logic.InteractionBehavior == "none")
            return;

        switch (logic.InteractionBehavior)
        {
            case "teleport":
                // Teleport config: from item definition or instance properties
                string targetMap = logic.Teleport?.TargetMap ?? "";
                int targetX = logic.Teleport?.TargetX ?? 0;
                int targetY = logic.Teleport?.TargetY ?? 0;

                // Instance properties override item defaults
                if (Properties.TryGetValue("target_map", out var tm))
                    targetMap = tm.ToString();
                if (Properties.TryGetValue("target_x", out var tx))
                    targetX = Convert.ToInt32(tx);
                if (Properties.TryGetValue("target_y", out var ty))
                    targetY = Convert.ToInt32(ty);

                if (!string.IsNullOrEmpty(targetMap))
                    InteractionBehavior = new TeleportBehavior(targetMap, targetX, targetY, logic.ChargeTime);
                break;

            case "dialogue":
                // Read dialogue lines from instance properties
                var lines = new List<string>();
                if (Properties.TryGetValue("dialogue_lines", out var dlObj))
                {
                    if (dlObj is List<object> dlList)
                        foreach (var item in dlList)
                            lines.Add(item.ToString());
                    else if (dlObj is System.Collections.IEnumerable enumerable)
                        foreach (var item in enumerable)
                            lines.Add(item.ToString());
                }
                Serilog.Log.Debug("DialogueBehavior created for {Id}: {Count} lines, propType={Type}",
                    ItemId, lines.Count, dlObj?.GetType().Name ?? "null");
                InteractionBehavior = new DialogueBehavior(lines);
                break;
        }
    }

    // Add an effect to this object
    public void AddEffect(ActiveEffect effect)
    {
        Effects.Add(effect);
    }

    // Check if this object has a specific effect
    public bool HasEffect(string effectId)
    {
        return Effects.Any(e => e.EffectId == effectId && !e.IsExpired);
    }

    // Run damage through all active effects (returns modified damage)
    public int ApplyEffectsToDamage(int damage)
    {
        foreach (var ae in Effects)
        {
            if (ae.IsExpired) continue;
            damage = ae.Effect.ModifyDamage(this, damage);
        }
        return damage;
    }

    // Tick effect TTLs and periodically enqueue a RefreshEffectsEvent
    public void UpdateEffects(float deltaTime)
    {
        // Remove all effects on death
        if (!State.IsAlive)
        {
            if (Effects.Count > 0) Effects.Clear();
            return;
        }

        // Tick all effect TTLs
        foreach (var ae in Effects)
            ae.Update(deltaTime);

        // Every 1 second, enqueue a RefreshEffectsEvent to process expirations via the queue
        _effectRefreshTimer += deltaTime;
        if (_effectRefreshTimer >= EffectRefreshIntervalSeconds)
        {
            _effectRefreshTimer -= EffectRefreshIntervalSeconds;
            if (Effects.Count > 0)
                EnqueueEvent(new RefreshEffectsEvent());
        }
    }

    // Add an event to the queue (processed sequentially, one per frame tick)
    public void EnqueueEvent(IObjectEvent evt)
    {
        _eventQueue.Enqueue(evt);
    }

    // Process the current event; advance to next when complete
    public void UpdateEvents(GameMap map, float deltaTime)
    {
        // If no current event, dequeue the next one
        if (_currentEvent == null)
        {
            if (_eventQueue.Count == 0) return;
            _currentEvent = _eventQueue.Dequeue();
            _currentEvent.Start(this, map);
        }

        // Tick the current event
        _currentEvent.Update(this, map, deltaTime);

        // If complete, clear it so the next event can start next frame
        if (_currentEvent.IsComplete)
            _currentEvent = null;
    }

    public bool HasPendingEvents => _currentEvent != null || _eventQueue.Count > 0;

    /// <summary>
    /// Check if the player overlaps this object and manage the interaction timer.
    /// Returns an InteractionRequest when the charge completes, or null.
    /// </summary>
    public InteractionRequest UpdateOverlap(WorldObject player, float deltaTime)
    {
        if (InteractionBehavior == null || IsTriggered) return null;

        // Check AABB overlap: player tile vs object area
        bool overlaps = player.TileX < TileX + EffectiveWidth
            && player.TileX + player.EffectiveWidth > TileX
            && player.TileY < TileY + EffectiveHeight
            && player.TileY + player.EffectiveHeight > TileY;

        if (overlaps)
        {
            // Accumulate overlap time
            _overlapTimer += deltaTime;

            // Trigger when fully charged
            if (_overlapTimer >= InteractionBehavior.ChargeTime)
            {
                IsTriggered = true;
                var request = InteractionBehavior.Execute(this, player);
                _overlapTimer = 0f;
                return request;
            }
        }
        else
        {
            // Player left — reset timer
            _overlapTimer = 0f;
        }

        return null;
    }

    // Reset triggered state (call after map transition completes)
    public void ResetInteraction()
    {
        IsTriggered = false;
        _overlapTimer = 0f;
    }

    // Restore state from persisted data (used when loading map state from DB)
    public void RestoreState(int currentHp)
    {
        var faction = ObjectState.ParseFaction(Definition.Logic.Faction);
        int maxHp = Definition.Logic.MaxHealth;
        State = new ObjectState(maxHp > 0 ? maxHp : 1, currentHp, faction);
    }

    private static ObjectCategory ParseCategory(string category)
    {
        return category?.ToLowerInvariant() switch
        {
            "creature" => ObjectCategory.Creature,
            _ => ObjectCategory.Item,
        };
    }
}
