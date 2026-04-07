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

    /// <summary>Unique queue ID. Set to {InstanceId}:action when registered.</summary>
    public string Id { get; private set; }

    public BaseObject Owner => _owner;
    public int Count => _queue.Count;

    public ActionQueue(BaseObject owner)
    {
        _owner = owner;
        Id = $"{owner.ItemId}:action"; // temporary, updated on RegisterQueues
    }

    /// <summary>Update ID to use the object's unique InstanceId.</summary>
    public void SetId(string objectId) => Id = $"{objectId}:action";

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

    /// <summary>
    /// Graceful shutdown: drain all remaining action events and log.
    /// </summary>
    public void Drain()
    {
        int count = _queue.Count;
        if (count > 0)
        {
            Log.Information("[{QueueId}] Draining {Count} pending action events...", Id, count);
            Clear();
            Log.Information("[{QueueId}] Drained", Id);
        }
    }
}
