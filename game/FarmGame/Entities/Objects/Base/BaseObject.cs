using System;
using System.Collections.Generic;
using System.Linq;
using FarmGame.Data;
using FarmGame.Queues;
using FarmGame.World;
using FarmGame.World.Effects;
using FarmGame.World.Events;
using FarmGame.World.Interactions;
using Serilog;

namespace FarmGame.Entities.Objects;

/// <summary>
/// Abstract base class for all game world entities.
/// Encapsulates shared logic: position, state, effects, event queue,
/// interaction behavior, and optional per-object DamageQueue.
/// </summary>
public abstract class BaseObject : IEntity
{
    // ─── Identity ───────────────────────────────────────────

    public string Id { get; set; }
    public string ItemId { get; }
    public virtual string Name => Definition?.Metadata?.DisplayName ?? ItemId;
    public ItemDefinition Definition { get; }
    public ObjectCategory Category { get; }

    // ─── Position & Size ────────────────────────────────────

    public int TileX { get; set; }
    public int TileY { get; set; }
    public int EffectiveWidth { get; }
    public int EffectiveHeight { get; }

    // ─── State ──────────────────────────────────────────────

    public ObjectState State { get; protected set; }
    public bool IsAlive => State.IsAlive;
    public Dictionary<string, object> Properties { get; }

    // ─── Effects ────────────────────────────────────────────

    public List<ActiveEffect> Effects { get; } = new();
    private float _effectRefreshTimer;
    private const float EffectRefreshIntervalSeconds = 1f;

    // ─── Interaction ────────────────────────────────────────

    /// <summary>
    /// Whether this object supports interaction (damage, dialogue, etc.).
    /// Determined at construction from item definition.
    /// Interactable objects own a per-object DamageQueue.
    /// </summary>
    public bool IsInteractable { get; }

    public IInteractionBehavior InteractionBehavior { get; set; }
    private float _overlapTimer;
    public bool IsTriggered { get; private set; }

    public float OverlapProgress => InteractionBehavior != null
        ? Math.Clamp(_overlapTimer / InteractionBehavior.ChargeTime, 0f, 1f) : 0f;

    // ─── Per-Object Event Queue ─────────────────────────────

    private readonly Queue<IObjectEvent> _eventQueue = new();
    private IObjectEvent _currentEvent;

    // ─── Per-Object Damage Queue (only for interactable objects) ─

    /// <summary>
    /// Per-object damage queue. Only created when IsInteractable is true.
    /// External systems enqueue DamageEvent here; the object processes
    /// them during its update cycle.
    /// </summary>
    public DamageQueue DamageQueue { get; private set; }

    // ─── Constructor ────────────────────────────────────────

    protected BaseObject(string itemId, ItemDefinition definition, int tileX, int tileY,
        Dictionary<string, object> properties, bool? isInteractable = null)
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

        // Initialize runtime state from definition
        var faction = ObjectState.ParseFaction(definition.Logic.Faction);
        var behavior = ObjectState.ParseBehavior(definition.Logic.DefaultBehavior);
        int maxHp = definition.Logic.MaxHealth;
        int level = definition.Logic.Level;
        State = new ObjectState(maxHp > 0 ? maxHp : 1, faction, behavior, level);

        // Determine interactability: explicit override > definition flag
        IsInteractable = isInteractable ?? definition.Logic.IsInteractable;

        // Create per-object DamageQueue for interactable objects
        if (IsInteractable)
        {
            DamageQueue = new DamageQueue(this);
            Log.Debug("[BaseObject] Created DamageQueue for interactable object '{Id}' ({ItemId})",
                Id, ItemId);
        }

        // Set up interaction behavior from YAML config
        InitInteractionBehavior();
    }

    // ─── Interaction Behavior Setup ─────────────────────────

    private void InitInteractionBehavior()
    {
        var logic = Definition.Logic;
        if (string.IsNullOrEmpty(logic.InteractionBehavior) || logic.InteractionBehavior == "none")
            return;

        switch (logic.InteractionBehavior)
        {
            case "teleport":
                string targetMap = logic.Teleport?.TargetMap ?? "";
                int targetX = logic.Teleport?.TargetX ?? 0;
                int targetY = logic.Teleport?.TargetY ?? 0;

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
                InteractionBehavior = new DialogueBehavior(lines);
                break;
        }
    }

    // ─── Effects ────────────────────────────────────────────

    public void AddEffect(ActiveEffect effect) => Effects.Add(effect);

    public bool HasEffect(string effectId) =>
        Effects.Any(e => e.EffectId == effectId && !e.IsExpired);

    public int ApplyEffectsToDamage(int damage, Combat.AttackInfo attackInfo = null)
    {
        attackInfo ??= Combat.AttackInfo.Physical;
        foreach (var ae in Effects)
        {
            if (ae.IsExpired) continue;
            damage = ae.Effect.ModifyDamage(this, damage, attackInfo);
        }
        return damage;
    }

    public void UpdateEffects(float deltaTime)
    {
        if (!State.IsAlive)
        {
            if (Effects.Count > 0) Effects.Clear();
            return;
        }

        foreach (var ae in Effects)
            ae.Update(deltaTime);

        _effectRefreshTimer += deltaTime;
        if (_effectRefreshTimer >= EffectRefreshIntervalSeconds)
        {
            _effectRefreshTimer -= EffectRefreshIntervalSeconds;
            if (Effects.Count > 0)
                EnqueueEvent(new RefreshEffectsEvent());
        }
    }

    // ─── Object Event Queue ─────────────────────────────────

    public void EnqueueEvent(IObjectEvent evt) => _eventQueue.Enqueue(evt);

    public void UpdateEvents(GameMap map, float deltaTime)
    {
        if (_currentEvent == null)
        {
            if (_eventQueue.Count == 0) return;
            _currentEvent = _eventQueue.Dequeue();
            _currentEvent.Start(this, map);
        }

        _currentEvent.Update(this, map, deltaTime);

        if (_currentEvent.IsComplete)
            _currentEvent = null;
    }

    public bool HasPendingEvents => _currentEvent != null || _eventQueue.Count > 0;

    // ─── Damage Queue Processing ────────────────────────────

    /// <summary>
    /// Process pending damage events from the per-object DamageQueue.
    /// Converts each DamageEvent into a TakeDamageEvent on the object event queue.
    /// Call this during the object's update cycle.
    /// </summary>
    public void ProcessDamageQueue()
    {
        if (DamageQueue == null) return;

        while (DamageQueue.TryDequeue(out var dmgEvent))
        {
            EnqueueEvent(new TakeDamageEvent(
                dmgEvent.Damage,
                dmgEvent.IsCritical,
                dmgEvent.Attack));
        }
    }

    // ─── Overlap / Interaction ──────────────────────────────

    public InteractionRequest UpdateOverlap(BaseObject player, float deltaTime)
    {
        if (InteractionBehavior == null || IsTriggered) return null;

        bool overlaps = player.TileX < TileX + EffectiveWidth
            && player.TileX + player.EffectiveWidth > TileX
            && player.TileY < TileY + EffectiveHeight
            && player.TileY + player.EffectiveHeight > TileY;

        if (overlaps)
        {
            _overlapTimer += deltaTime;

            if (_overlapTimer >= InteractionBehavior.ChargeTime)
            {
                IsTriggered = true;
                // InteractionBehavior.Execute expects WorldObject; delegate to subclass
                var request = OnInteractionTriggered(player);
                _overlapTimer = 0f;
                return request;
            }
        }
        else
        {
            _overlapTimer = 0f;
        }

        return null;
    }

    /// <summary>
    /// Hook for subclasses to handle interaction trigger.
    /// Default returns null. Override to provide interaction logic.
    /// </summary>
    protected virtual InteractionRequest OnInteractionTriggered(BaseObject player) => null;

    public void ResetInteraction()
    {
        IsTriggered = false;
        _overlapTimer = 0f;
    }

    // ─── State Restore ──────────────────────────────────────

    public void RestoreState(int currentHp)
    {
        var faction = ObjectState.ParseFaction(Definition.Logic.Faction);
        var behavior = ObjectState.ParseBehavior(Definition.Logic.DefaultBehavior);
        int maxHp = Definition.Logic.MaxHealth;
        int level = Definition.Logic.Level;
        State = new ObjectState(maxHp > 0 ? maxHp : 1, currentHp, faction, behavior, level);
    }

    // ─── Category Parsing ───────────────────────────────────

    private static ObjectCategory ParseCategory(string category)
    {
        return category?.ToLowerInvariant() switch
        {
            "creature" => ObjectCategory.Creature,
            _ => ObjectCategory.Item,
        };
    }
}
