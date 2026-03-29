// =============================================================================
// IInteractionBehavior.cs — Interface for object interaction behaviors
//
// Objects with an IInteractionBehavior listen for player overlap.
// When the player stands on the object for the required duration,
// Execute() is called. Each behavior type (teleport, dialogue, etc.)
// has its own implementation.
// =============================================================================

using System;

namespace FarmGame.World.Interactions;

public class InteractionRequest
{
    public string TargetMap { get; init; }
    public int TargetX { get; init; }
    public int TargetY { get; init; }
}

public interface IInteractionBehavior
{
    // Time in seconds the player must stand on the object to trigger
    float ChargeTime { get; }

    // Execute the interaction. Returns an InteractionRequest if the game
    // state needs to change (e.g. map transition), or null for local-only actions.
    InteractionRequest Execute(WorldObject source, WorldObject player);
}
