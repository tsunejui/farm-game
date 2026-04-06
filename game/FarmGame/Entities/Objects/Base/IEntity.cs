using System.Collections.Generic;
using FarmGame.World;
using FarmGame.World.Effects;

namespace FarmGame.Entities.Objects;

/// <summary>
/// Common contract for all game entities (creatures, items, environment).
/// Every object in the world implements this interface.
/// </summary>
public interface IEntity
{
    /// <summary>Unique instance ID (for persistence and lookup).</summary>
    string Id { get; }

    /// <summary>Display name shown in HUD / inspector.</summary>
    string Name { get; }

    /// <summary>Grid X position.</summary>
    int TileX { get; set; }

    /// <summary>Grid Y position.</summary>
    int TileY { get; set; }

    /// <summary>Width in tiles.</summary>
    int EffectiveWidth { get; }

    /// <summary>Height in tiles.</summary>
    int EffectiveHeight { get; }

    /// <summary>Whether this object is still alive / active.</summary>
    bool IsAlive { get; }

    /// <summary>
    /// Whether this object supports interaction (damage, dialogue, etc.).
    /// Interactable objects own a per-object event queue.
    /// </summary>
    bool IsInteractable { get; }

    /// <summary>Runtime mutable state (HP, faction, animations).</summary>
    ObjectState State { get; }

    /// <summary>Active effects (buffs / debuffs).</summary>
    List<ActiveEffect> Effects { get; }
}
