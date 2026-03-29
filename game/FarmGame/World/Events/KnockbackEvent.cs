// =============================================================================
// KnockbackEvent.cs — Pushes an object away by a number of tiles
//
// Tries the full knockback distance first, falls back to shorter distances
// if blocked. Triggers bounce animation on alive objects.
// =============================================================================

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

    public void Start(WorldObject obj, GameMap map)
    {
        // Try full distance, reduce if blocked
        for (int dist = _distance; dist > 0; dist--)
        {
            int newX = obj.TileX + _dirX * dist;
            int newY = obj.TileY + _dirY * dist;
            if (map.MoveObject(obj, newX, newY))
            {
                if (obj.State.IsAlive)
                    obj.State.TriggerBounce();
                Log.Debug("Knockback: {ItemId} pushed {Dist} tiles to ({X},{Y})",
                    obj.ItemId, dist, newX, newY);
                break;
            }
        }

        IsComplete = true;
    }

    public void Update(WorldObject obj, GameMap map, float deltaTime) { }
}
