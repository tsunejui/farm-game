using FarmGame.Combat;

namespace FarmGame.Entities.Objects;

/// <summary>
/// Represents a damage event targeting a specific object.
/// Enqueued into the object's per-instance DamageQueue by combat systems.
/// Processed during the object's update cycle, converting to TakeDamageEvent.
/// </summary>
public record DamageEvent(
    /// <summary>Source entity ID (attacker).</summary>
    string SourceId,
    /// <summary>Damage amount (before object-level effects).</summary>
    int Damage,
    /// <summary>Whether this was a critical hit.</summary>
    bool IsCritical,
    /// <summary>Attack classification (physical/magical/elemental).</summary>
    AttackInfo Attack);
