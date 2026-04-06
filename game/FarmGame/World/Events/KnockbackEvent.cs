// =============================================================================
// KnockbackEvent.cs — Pushes an object away by a number of tiles
//
// Tries the full knockback distance first, falls back to shorter distances
// if blocked. Triggers bounce animation on alive objects.
// =============================================================================

using FarmGame.Entities.Objects;
using Serilog;

namespace FarmGame.World.Events;

public class KnockbackEvent : IObjectEvent
{
    private readonly int _dirX;
    private readonly int _dirY;
    private readonly int _distance;

    public bool IsComplete { get; private set; }

    /// <param name="dirX">Direction X component (-1, 0, or 1)</param>
    /// <param name="dirY">Direction Y component (-1, 0, or 1)</param>
    /// <param name="distance">Max tiles to push</param>
    public KnockbackEvent(int dirX, int dirY, int distance)
    {
        _dirX = dirX;
        _dirY = dirY;
        _distance = distance;
    }

    public void Start(BaseObject obj, GameMap map)
    {
        // MoveObject requires WorldObject; cast is safe since all map objects are WorldObject
        if (obj is WorldObject wo)
        {
            for (int dist = _distance; dist > 0; dist--)
            {
                int newX = wo.TileX + _dirX * dist;
                int newY = wo.TileY + _dirY * dist;
                if (map.MoveObject(wo, newX, newY))
                {
                    if (wo.State.IsAlive)
                        wo.State.TriggerBounce();
                    Log.Debug("Knockback: {ItemId} pushed {Dist} tiles to ({X},{Y})",
                        wo.ItemId, dist, newX, newY);
                    break;
                }
            }
        }

        IsComplete = true;
    }

    public void Update(BaseObject obj, GameMap map, float deltaTime) { }
}
