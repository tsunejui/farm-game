using MediatR;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using FarmGame.Combat;

namespace FarmGame.Core;

// ─── Input Events ───────────────────────────────────────────
/// <summary>Keyboard state snapshot published each frame.</summary>
public record InputEvent(KeyboardState Keyboard, GameTime GameTime) : INotification;

/// <summary>Toggle pause state (from Escape key).</summary>
public record TogglePauseEvent : INotification;

// ─── World Events ───────────────────────────────────────────
/// <summary>Damage was dealt to an entity.</summary>
public record DamageDealtEvent(
    string TargetId,
    int Damage,
    bool IsCritical,
    AttackInfo Attack,
    Vector2 WorldPosition) : INotification;

/// <summary>An entity was killed.</summary>
public record EntityKilledEvent(string EntityId, Vector2 WorldPosition) : INotification;

/// <summary>Player requests to teleport to another map.</summary>
public record TeleportRequestEvent(string TargetMap, int TargetX, int TargetY) : INotification;

/// <summary>Map has finished loading.</summary>
public record MapLoadedEvent(string MapId, string MapName) : INotification;

// ─── UI Events ──────────────────────────────────────────────
/// <summary>Toggle inventory open/close.</summary>
public record InventoryToggleEvent(bool IsOpen) : INotification;

// ─── System Events ──────────────────────────────────────────
/// <summary>Database connection was lost.</summary>
public record DatabaseDisconnectedEvent(string Reason) : INotification;

/// <summary>Database connection was restored.</summary>
public record DatabaseReconnectedEvent : INotification;
