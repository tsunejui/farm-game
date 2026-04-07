using System.Collections.Concurrent;
using FarmGame.Entities.Objects;
using Serilog;

namespace FarmGame.Queues;

/// <summary>
/// Thread-safe per-object queue for incoming damage events.
/// Created automatically when an object is constructed with IsInteractable = true.
/// </summary>
public class DamageQueue
{
    private readonly ConcurrentQueue<DamageEvent> _queue = new();
    private readonly BaseObject _owner;

    /// <summary>Unique queue ID: {ItemId}:damage</summary>
    public string Id { get; }

    public BaseObject Owner => _owner;
    public int Count => _queue.Count;

    public DamageQueue(BaseObject owner)
    {
        _owner = owner;
        Id = $"{owner.ItemId}:damage";
    }

    public void Enqueue(DamageEvent damageEvent)
    {
        _queue.Enqueue(damageEvent);
        Log.Debug("[{QueueId}] Enqueued damage {Damage} (crit={Crit})",
            Id, damageEvent.Damage, damageEvent.IsCritical);
    }

    public bool TryDequeue(out DamageEvent damageEvent) => _queue.TryDequeue(out damageEvent);

    public void Clear()
    {
        while (_queue.TryDequeue(out _)) { }
    }
}
