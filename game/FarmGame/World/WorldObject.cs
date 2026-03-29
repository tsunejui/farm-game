using System;
using System.Collections.Generic;
using System.Linq;
using FarmGame.Data;
using FarmGame.World.Effects;
using FarmGame.World.Events;

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
