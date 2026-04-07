using System.Collections.Concurrent;
using FarmGame.Entities.Objects;
using Serilog;

namespace FarmGame.Queues;

/// <summary>
/// Thread-safe per-object queue for action events (move, attack, skill).
/// Created automatically when an object is constructed with IsInteractable = true.
/// </summary>
public class ActionQueue
{
    private readonly ConcurrentQueue<ActionEvent> _queue = new();
    private readonly BaseObject _owner;

    /// <summary>Unique queue ID: {ItemId}:action</summary>
    public string Id { get; }

    public BaseObject Owner => _owner;
    public int Count => _queue.Count;

    public ActionQueue(BaseObject owner)
    {
        _owner = owner;
        Id = $"{owner.ItemId}:action";
    }

    public void Enqueue(ActionEvent actionEvent)
    {
        _queue.Enqueue(actionEvent);
        Log.Debug("[{QueueId}] Enqueued '{Action}' to ({X},{Y})",
            Id, actionEvent.ActionType, actionEvent.TargetX, actionEvent.TargetY);
    }

    public bool TryDequeue(out ActionEvent actionEvent) => _queue.TryDequeue(out actionEvent);

    public void Clear()
    {
        while (_queue.TryDequeue(out _)) { }
    }
}
