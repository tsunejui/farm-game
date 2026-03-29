// =============================================================================
// ObjectState.cs — Runtime mutable state for a world object
//
// Tracks HP, alive/dead status, faction, and the damage-over-time flash effect.
// Attached to each WorldObject at map load. Persisted to SQLite on save.
//
// Damage model:
//   When TakeDamage(amount) is called, the total damage is distributed evenly
//   over DamageTickDurationMs. Each millisecond deducts a fixed fraction.
//   During the tick window the object flashes. If hit again before the window
//   expires, remaining + new damage are combined and the timer restarts.
// =============================================================================

using System;

namespace FarmGame.World;

public enum Faction
{
    Neutral,   // Can be attacked
    Friendly,  // Cannot be attacked
    Enemy      // Can be attacked; may attack player in the future
}

public enum BehaviorState
{
    Idle,      // Standing still, no action
    Neutral,   // Default state, passive
    Hostile    // Aggressive, will chase and attack player
}

public class ObjectState
{
    public int MaxHp { get; }
    public int CurrentHp { get; private set; }
    public bool IsAlive => CurrentHp > 0;
    public Faction Faction { get; }
    public BehaviorState Behavior { get; set; } = BehaviorState.Neutral;
    public int Level { get; }

    // Damage-over-time tracking
    private float _pendingDamage;
    private float _damageTickRemainMs;
    private float _damagePerMs;
    private float _fractionalDamage;
    private float _flickerTimer;

    public bool IsTakingDamage => _damageTickRemainMs > 0f && IsAlive;
    public bool FlickerVisible { get; private set; }

    // Knockback bounce animation
    private float _bounceTimer;
    private const float BounceDurationMs = 200f;
    private const float BounceHeight = 6f;

    public bool IsBouncing => _bounceTimer > 0f;

    public float BounceOffsetY
    {
        get
        {
            if (_bounceTimer <= 0f) return 0f;
            float p = 1f - _bounceTimer / BounceDurationMs;
            return -BounceHeight * 4f * p * (1f - p);
        }
    }

    // Floating damage number display
    public int LastDamageAmount { get; private set; }
    public bool LastDamageWasCrit { get; private set; }
    private float _damageNumberTimer;
    private const float DamageNumberDurationMs = 800f;

    public bool ShowDamageNumber => _damageNumberTimer > 0f;
    public float DamageNumberProgress => 1f - _damageNumberTimer / DamageNumberDurationMs;

    public ObjectState(int maxHp, Faction faction, BehaviorState behavior = BehaviorState.Neutral, int level = 1)
    {
        MaxHp = maxHp;
        CurrentHp = maxHp;
        Faction = faction;
        Behavior = behavior;
        Level = level;
    }

    public ObjectState(int maxHp, int currentHp, Faction faction, BehaviorState behavior = BehaviorState.Neutral, int level = 1)
    {
        MaxHp = maxHp;
        CurrentHp = Math.Clamp(currentHp, 0, maxHp);
        Faction = faction;
        Behavior = behavior;
        Level = level;
    }

    // Sync HP from external source (used by Player proxy)
    public void SyncHp(int currentHp, int maxHp)
    {
        CurrentHp = currentHp;
    }

    public void TriggerBounce()
    {
        _bounceTimer = BounceDurationMs;
    }

    // Show damage number without actually deducting HP (for 0-damage hits)
    public void ShowDamageNumberOnly(int displayAmount, bool isCritical)
    {
        LastDamageAmount = displayAmount;
        LastDamageWasCrit = isCritical;
        _damageNumberTimer = DamageNumberDurationMs;
    }

    public void Heal(int amount)
    {
        if (!IsAlive || amount <= 0) return;
        CurrentHp = Math.Min(CurrentHp + amount, MaxHp);
    }

    public void TakeDamage(int amount, bool isCritical = false)
    {
        if (!IsAlive || amount <= 0) return;

        _pendingDamage += amount;
        int tickMs = Core.GameConstants.DamageTickDurationMs;
        _damageTickRemainMs = tickMs;
        _damagePerMs = _pendingDamage / tickMs;

        LastDamageAmount = amount;
        LastDamageWasCrit = isCritical;
        _damageNumberTimer = DamageNumberDurationMs;
    }

    public void Update(float deltaTimeSeconds)
    {
        if (_damageNumberTimer > 0f)
            _damageNumberTimer -= deltaTimeSeconds * 1000f;

        if (_bounceTimer > 0f)
            _bounceTimer -= deltaTimeSeconds * 1000f;

        if (_damageTickRemainMs <= 0f)
        {
            FlickerVisible = false;
            return;
        }

        float deltaMs = deltaTimeSeconds * 1000f;
        float deduct = _damagePerMs * deltaMs;
        if (deduct > _pendingDamage) deduct = _pendingDamage;

        _pendingDamage -= deduct;

        _fractionalDamage += deduct;
        int wholeDeduct = (int)_fractionalDamage;
        if (wholeDeduct > 0)
        {
            _fractionalDamage -= wholeDeduct;
            CurrentHp -= wholeDeduct;
            if (CurrentHp < 0) CurrentHp = 0;
        }

        _flickerTimer += deltaMs;
        const float flickerIntervalMs = 60f;
        if (_flickerTimer >= flickerIntervalMs)
        {
            FlickerVisible = !FlickerVisible;
            _flickerTimer -= flickerIntervalMs;
        }

        _damageTickRemainMs -= deltaMs;
        if (_damageTickRemainMs <= 0f)
        {
            float remaining = _pendingDamage + _fractionalDamage;
            if (remaining > 0f)
            {
                CurrentHp -= (int)Math.Ceiling(remaining);
                if (CurrentHp < 0) CurrentHp = 0;
                _pendingDamage = 0f;
                _fractionalDamage = 0f;
            }
            _damageTickRemainMs = 0f;
            FlickerVisible = false;
            _flickerTimer = 0f;
        }
    }

    public static Faction ParseFaction(string value)
    {
        return value?.ToLowerInvariant() switch
        {
            "friendly" => Faction.Friendly,
            "enemy" => Faction.Enemy,
            _ => Faction.Neutral,
        };
    }

    public static BehaviorState ParseBehavior(string value)
    {
        return value?.ToLowerInvariant() switch
        {
            "idle" => BehaviorState.Idle,
            "hostile" => BehaviorState.Hostile,
            _ => BehaviorState.Neutral,
        };
    }
}
