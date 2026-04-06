// =============================================================================
// TeleportBehavior.cs — Teleports the player to a target map + position
//
// Triggered when the player stands on the object for ChargeTime seconds.
// Returns an InteractionRequest with the target map/coordinates for
// PlayingScreen to handle the actual map transition.
// =============================================================================

using FarmGame.Core;
using FarmGame.Core.Managers;
using Serilog;

namespace FarmGame.World.Interactions;

public class TeleportBehavior : IInteractionBehavior
{
    public string TargetMap { get; }
    public int TargetX { get; }
    public int TargetY { get; }
    public float ChargeTime { get; }

    public TeleportBehavior(string targetMap, int targetX, int targetY, float chargeTime = 1f)
    {
        TargetMap = targetMap;
        TargetX = targetX;
        TargetY = targetY;
        ChargeTime = chargeTime;
    }

    public InteractionRequest Execute(WorldObject source, WorldObject player)
    {
        Log.Information("Teleport triggered: {Map} ({X},{Y})", TargetMap, TargetX, TargetY);
        MessageQueue.Enqueue(LocaleManager.Format("ui", "teleporting",
            LocaleManager.Get("maps", TargetMap, TargetMap)));

        return new InteractionRequest
        {
            TargetMap = TargetMap,
            TargetX = TargetX,
            TargetY = TargetY,
        };
    }
}
