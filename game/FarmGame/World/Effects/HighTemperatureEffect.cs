// =============================================================================
// HighTemperatureEffect.cs — Deals 1-3 damage to objects at distance 0
//
// On each tick (~1 second), objects standing on the same tiles as the
// owner have a 50% chance to take 1-3 fire damage.
// =============================================================================

using System;
using FarmGame.World.Events;

namespace FarmGame.World.Effects;

public class HighTemperatureEffect : IEffect
{
    private static readonly Random Rng = new();

    public string Id => "high_temperature";
    public string DisplayName => "High Temperature";

    public void OnTick(WorldObject owner, GameMap map)
    {
        for (int x = owner.TileX; x < owner.TileX + owner.EffectiveWidth; x++)
        {
            for (int y = owner.TileY; y < owner.TileY + owner.EffectiveHeight; y++)
            {
                // Check map objects
                foreach (var obj in map.Objects)
                    TryDamage(obj, owner, x, y);

                // Player is also an object — check player proxy
                if (map.PlayerProxy != null)
                    TryDamage(map.PlayerProxy, owner, x, y);
            }
        }
    }

    private void TryDamage(WorldObject target, WorldObject owner, int x, int y)
    {
        if (target == owner) return;
        if (!target.State.IsAlive) return;
        if (target.TileX != x || target.TileY != y) return;
        if (Rng.NextDouble() >= 0.5) return;

        int damage = Rng.Next(1, 4);
        target.EnqueueEvent(new TakeDamageEvent(damage));
    }
}
