using System.Collections.Concurrent;
using FarmGame.Entities.Objects;
using Serilog;

namespace FarmGame.Queues;

/// <summary>
/// Thread-safe per-object queue for action events (move, attack, skill).
/// Created automatically when an object is constructed with IsInteractable = true.
///
/// AI systems or input handlers enqueue ActionEvent here.
/// The owning object drains this queue during its update cycle.
/// </summary>
public class ActionQueue
{
    private readonly ConcurrentQueue<ActionEvent> _queue = new();
    private readonly BaseObject _owner;

    public BaseObject Owner => _owner;
    public int Count => _queue.Count;

    public ActionQueue(BaseObject owner)
    {
        _owner = owner;
    }

    public void Enqueue(ActionEvent actionEvent)
    {
        _queue.Enqueue(actionEvent);
        Log.Debug("[ActionQueue] Enqueued '{Action}' to ({X},{Y}) on '{Owner}'",
            actionEvent.ActionType, actionEvent.TargetX, actionEvent.TargetY, _owner.ItemId);
    }

    public bool TryDequeue(out ActionEvent actionEvent) => _queue.TryDequeue(out actionEvent);

    public void Clear()
    {
        while (_queue.TryDequeue(out _)) { }
    }
}
