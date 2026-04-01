using MediatR;
using Microsoft.Xna.Framework;
using FarmGame.Combat;

namespace FarmGame.Core;

// ─── Command: Damage ────────────────────────────────────────
/// <summary>Apply damage to a target entity.</summary>
public record DamageCommand(
    string TargetId,
    int TileX,
    int TileY,
    int Damage,
    bool IsCritical,
    AttackInfo Attack) : IRequest<Unit>;

// ─── Command: Actor Action ──────────────────────────────────
/// <summary>Queue an action for an actor (move, attack, use skill).</summary>
public record ActorActionCommand(
    string ActorId,
    string ActionType,  // "move", "attack", "skill"
    int TargetX,
    int TargetY) : IRequest<Unit>;

// ─── Command: Spawn Entity ──────────────────────────────────
/// <summary>Spawn a new entity into the world.</summary>
public record SpawnEntityCommand(
    string ItemId,
    int TileX,
    int TileY,
    string Faction = "neutral") : IRequest<Unit>;
