// =============================================================================
// RestEffect.cs — Heals the owner for 1% of max HP every 2 seconds
//
// Applied to creatures when they've been idle (no hostile creatures nearby)
// for 3+ seconds. Removed when entering hostile state.
// =============================================================================

using System;
using FarmGame.Entities.Objects;

namespace FarmGame.World.Effects;

public class RestEffect : IEffect
{
    public string Id => "rest";
    public string DisplayName => "Resting";

    public void OnTick(BaseObject owner, GameMap map)
    {
        if (!owner.State.IsAlive) return;
        if (owner.State.CurrentHp >= owner.State.MaxHp) return;

        // Heal 1% of max HP (minimum 1)
        int healAmount = Math.Max(1, owner.State.MaxHp / 100);
        owner.State.Heal(healAmount);
    }
}
