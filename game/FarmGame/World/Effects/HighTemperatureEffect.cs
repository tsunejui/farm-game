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
        // Check all tiles occupied by the owner
        for (int x = owner.TileX; x < owner.TileX + owner.EffectiveWidth; x++)
        {
            for (int y = owner.TileY; y < owner.TileY + owner.EffectiveHeight; y++)
            {
                // Check if any other object is at distance 0 (same tile)
                // For now, check all map objects (including player proxy)
                foreach (var obj in map.Objects)
                {
                    if (obj == owner) continue;
                    if (!obj.State.IsAlive) continue;
                    if (obj.TileX != x || obj.TileY != y) continue;

                    // 50% chance
                    if (Rng.NextDouble() >= 0.5) continue;

                    int damage = Rng.Next(1, 4); // 1-3
                    obj.EnqueueEvent(new TakeDamageEvent(damage));
                }
            }
        }
    }
}
