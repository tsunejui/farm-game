using System.Collections.Generic;
using FarmGame.Data;

namespace FarmGame.Entities.Objects.Creatures;

/// <summary>
/// Monster entity. Always interactable (can receive damage).
/// Controlled by CreatureBrain FSM (Idle/Neutral/Hostile states).
/// </summary>
public class Monster : BaseObject
{
    /// <summary>Attack damage dealt to player on contact.</summary>
    public int AttackDamage => Definition.Logic.AttackDamage;

    /// <summary>Attack range in tiles.</summary>
    public int AttackRange => Definition.Logic.AttackRange;

    /// <summary>Seconds between attacks.</summary>
    public float AttackCooldown => Definition.Logic.AttackCooldown;

    /// <summary>Tiles within which this monster detects the player.</summary>
    public int AggroRange => Definition.Logic.AggroRange;

    /// <summary>Movement speed (tiles per second).</summary>
    public float MoveSpeed => Definition.Logic.MoveSpeed;

    /// <summary>Speed multiplier when in hostile state.</summary>
    public float HostileSpeedMultiplier => Definition.Logic.HostileSpeedMultiplier;

    public Monster(string itemId, ItemDefinition definition, int tileX, int tileY,
        Dictionary<string, object> properties = null)
        : base(itemId, definition, tileX, tileY, properties, isInteractable: true)
    {
    }
}
