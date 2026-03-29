// =============================================================================
// IObjectEvent.cs — Interface for queued object state update events
//
// Each event is processed one at a time from the object's event queue.
// An event runs across multiple frames until IsComplete returns true,
// then the next event in the queue begins.
// =============================================================================

namespace FarmGame.World.Events;

public interface IObjectEvent
{
    // True when this event has finished and should be dequeued
    bool IsComplete { get; }

    // Called once when the event starts processing
    void Start(WorldObject obj, GameMap map);

    // Called every frame while the event is active
    void Update(WorldObject obj, GameMap map, float deltaTime);
}
