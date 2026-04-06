// =============================================================================
// BounceEvent.cs — Triggers a bounce animation on the object
//
// Completes when the bounce animation finishes (tracked by ObjectState).
// =============================================================================

using FarmGame.Entities.Objects;

namespace FarmGame.World.Events;

public class BounceEvent : IObjectEvent
{
    private bool _started;

    public bool IsComplete { get; private set; }

    public void Start(BaseObject obj, GameMap map)
    {
        _started = true;
        obj.State.TriggerBounce();
    }

    public void Update(BaseObject obj, GameMap map, float deltaTime)
    {
        if (!_started) return;
        if (!obj.State.IsBouncing)
            IsComplete = true;
    }
}
