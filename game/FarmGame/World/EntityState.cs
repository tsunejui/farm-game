// =============================================================================
// EntityState.cs — Runtime mutable state for a placed entity
//
// Tracks HP, alive/dead status, faction, and the damage-over-time flash effect.
// Attached to each EntityInstance at map load. Persisted to SQLite on save.
//
// Damage model:
//   When TakeDamage(amount) is called, the total damage is distributed evenly
//   over DamageTickDurationMs (default 500ms). Each millisecond deducts a fixed
//   fraction. During the tick window the entity flashes (dark gray overlay).
//   If hit again before the window expires, remaining + new damage are combined
//   and the timer restarts.
// =============================================================================

using System;

namespace FarmGame.World;

public enum Faction
{
    Neutral,   // Can be attacked
    Friendly,  // Cannot be attacked
    Enemy      // Can be attacked; may attack player in the future
}

public class EntityState
{
    public int MaxHp { get; }
    public int CurrentHp { get; private set; }
    public bool IsAlive => CurrentHp > 0;
    public Faction Faction { get; }

    // Damage-over-time tracking
    private float _pendingDamage;       // Total HP still to be deducted
    private float _damageTickRemainMs;  // Remaining flash/tick window
    private float _damagePerMs;         // HP to deduct per millisecond

    // True while the entity is in a damage tick window (used for flash rendering)
    public bool IsTakingDamage => _damageTickRemainMs > 0f && IsAlive;

    public EntityState(int maxHp, Faction faction)
    {
        MaxHp = maxHp;
        CurrentHp = maxHp;
        Faction = faction;
    }

    // Restore from persisted data
    public EntityState(int maxHp, int currentHp, Faction faction)
    {
        MaxHp = maxHp;
        CurrentHp = Math.Clamp(currentHp, 0, maxHp);
        Faction = faction;
    }

    /// <summary>
    /// Apply damage. The actual HP deduction is spread over DamageTickDurationMs.
    /// If already taking damage, remaining + new damage are combined and the timer restarts.
    /// </summary>
    public void TakeDamage(int amount)
    {
        if (!IsAlive || amount <= 0) return;

        // Combine any remaining pending damage with the new hit
        _pendingDamage += amount;
        int tickMs = Core.GameConstants.DamageTickDurationMs;
        _damageTickRemainMs = tickMs;
        _damagePerMs = _pendingDamage / tickMs;
    }

    /// <summary>
    /// Called every frame. Deducts HP incrementally and counts down the flash timer.
    /// </summary>
    public void Update(float deltaTimeSeconds)
    {
        if (_damageTickRemainMs <= 0f) return;

        float deltaMs = deltaTimeSeconds * 1000f;
        float deduct = _damagePerMs * deltaMs;

        // Clamp so we don't over-deduct
        if (deduct > _pendingDamage) deduct = _pendingDamage;

        _pendingDamage -= deduct;
        CurrentHp -= (int)Math.Ceiling(deduct);
        if (CurrentHp < 0) CurrentHp = 0;

        _damageTickRemainMs -= deltaMs;
        if (_damageTickRemainMs <= 0f)
        {
            // Flush any fractional remainder
            if (_pendingDamage > 0f)
            {
                CurrentHp -= (int)Math.Ceiling(_pendingDamage);
                if (CurrentHp < 0) CurrentHp = 0;
                _pendingDamage = 0f;
            }
            _damageTickRemainMs = 0f;
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
}
