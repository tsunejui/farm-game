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
    private float _fractionalDamage;    // Accumulates sub-integer damage between frames
    private float _flickerTimer;        // Accumulator for rapid flash toggle

    // True while the entity is in a damage tick window
    public bool IsTakingDamage => _damageTickRemainMs > 0f && IsAlive;

    // Rapid on/off toggle for visual flicker during damage (toggles every ~60ms)
    public bool FlickerVisible { get; private set; }

    // Floating damage number display
    public int LastDamageAmount { get; private set; }
    public bool LastDamageWasCrit { get; private set; }
    private float _damageNumberTimer;
    private const float DamageNumberDurationMs = 800f;

    // True while the floating damage number should be visible
    public bool ShowDamageNumber => _damageNumberTimer > 0f;

    // 0.0 (just appeared) → 1.0 (about to disappear), used for float-up animation
    public float DamageNumberProgress => 1f - _damageNumberTimer / DamageNumberDurationMs;

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
    public void TakeDamage(int amount, bool isCritical = false)
    {
        if (!IsAlive || amount <= 0) return;

        // Combine any remaining pending damage with the new hit
        _pendingDamage += amount;
        int tickMs = Core.GameConstants.DamageTickDurationMs;
        _damageTickRemainMs = tickMs;
        _damagePerMs = _pendingDamage / tickMs;

        // Trigger floating damage number
        LastDamageAmount = amount;
        LastDamageWasCrit = isCritical;
        _damageNumberTimer = DamageNumberDurationMs;
    }

    /// <summary>
    /// Called every frame. Deducts HP incrementally and counts down the flash timer.
    /// </summary>
    public void Update(float deltaTimeSeconds)
    {
        // Tick floating damage number timer (independent of damage ticks)
        if (_damageNumberTimer > 0f)
            _damageNumberTimer -= deltaTimeSeconds * 1000f;

        if (_damageTickRemainMs <= 0f)
        {
            FlickerVisible = false;
            return;
        }

        float deltaMs = deltaTimeSeconds * 1000f;
        float deduct = _damagePerMs * deltaMs;

        // Clamp so we don't over-deduct
        if (deduct > _pendingDamage) deduct = _pendingDamage;

        _pendingDamage -= deduct;

        // Accumulate fractional damage; only deduct whole integers from HP
        _fractionalDamage += deduct;
        int wholeDeduct = (int)_fractionalDamage;
        if (wholeDeduct > 0)
        {
            _fractionalDamage -= wholeDeduct;
            CurrentHp -= wholeDeduct;
            if (CurrentHp < 0) CurrentHp = 0;
        }

        // Rapid flicker: toggle visibility every ~60ms for a fast flash effect
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
            // Flush any fractional remainder
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
}
