// =============================================================================
// HitContext.cs — Data carrier flowing through the hit handler chain
//
// Created by AttackAction when a hit connects. Carries attacker, target,
// and the running damage value through each handler in the chain.
// =============================================================================

using FarmGame.Entities;
using FarmGame.World;

namespace FarmGame.Combat;

public class HitContext
{
    // Source and target
    public Player Attacker { get; init; }
    public WorldObject Target { get; init; }

    // Running damage — each handler may read and modify
    public int Damage { get; set; }
    public bool IsCritical { get; set; }

    // Set to true by any handler to stop the chain
    public bool Cancelled { get; set; }
}
