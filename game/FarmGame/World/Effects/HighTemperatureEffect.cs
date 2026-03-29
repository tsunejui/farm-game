// =============================================================================
// HighTemperatureEffect.cs — Deals 1-3 fire damage to adjacent objects
//
// On each tick (~1 second), objects within 1 tile of the owner
// have a 50% chance to take 1-3 fire damage (Natural/Fire attack).
// =============================================================================

using System;
using FarmGame.Combat;
using FarmGame.World.Events;

namespace FarmGame.World.Effects;

public class HighTemperatureEffect : IEffect
{
    private static readonly Random Rng = new();

    public string Id => "high_temperature";
    public string DisplayName => "High Temperature";

    public void OnTick(WorldObject owner, GameMap map)
    {
        int minX = owner.TileX - 1;
        int maxX = owner.TileX + owner.EffectiveWidth;
        int minY = owner.TileY - 1;
        int maxY = owner.TileY + owner.EffectiveHeight;

        foreach (var obj in map.Objects)
            TryDamage(obj, owner, minX, maxX, minY, maxY);

        if (map.PlayerProxy != null)
            TryDamage(map.PlayerProxy, owner, minX, maxX, minY, maxY);
    }

    private void TryDamage(WorldObject target, WorldObject owner,
        int minX, int maxX, int minY, int maxY)
    {
        if (target == owner) return;
        if (!target.State.IsAlive) return;

        bool overlaps = target.TileX <= maxX
            && target.TileX + target.EffectiveWidth - 1 >= minX
            && target.TileY <= maxY
            && target.TileY + target.EffectiveHeight - 1 >= minY;
        if (!overlaps) return;

        if (Rng.NextDouble() >= 0.5) return;

        int damage = Rng.Next(1, 4);
        target.EnqueueEvent(new TakeDamageEvent(damage, false, AttackInfo.NaturalFire));
    }
}
