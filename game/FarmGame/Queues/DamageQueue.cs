using System.Collections.Concurrent;
using FarmGame.Entities.Objects;
using Serilog;

namespace FarmGame.Queues;

/// <summary>
/// Thread-safe per-object queue for incoming damage events.
/// Created automatically when an object is constructed with IsInteractable = true.
///
/// External systems (combat pipeline, effects, traps) enqueue DamageEvent here.
/// The owning object drains this queue during its update cycle via
/// BaseObject.ProcessDamageQueue(), which converts each DamageEvent
/// into a TakeDamageEvent on the object's sequential event queue.
/// </summary>
public class DamageQueue
{
    private readonly ConcurrentQueue<DamageEvent> _queue = new();
    private readonly BaseObject _owner;

    /// <summary>The object that owns this queue.</summary>
    public BaseObject Owner => _owner;

    /// <summary>Number of pending damage events.</summary>
    public int Count => _queue.Count;

    public DamageQueue(BaseObject owner)
    {
        _owner = owner;
    }

    /// <summary>
    /// Enqueue a damage event. Thread-safe: can be called from any thread.
    /// </summary>
    public void Enqueue(DamageEvent damageEvent)
    {
        _queue.Enqueue(damageEvent);
        Log.Debug("[DamageQueue] Enqueued damage {Damage} (crit={Crit}) on '{Owner}'",
            damageEvent.Damage, damageEvent.IsCritical, _owner.ItemId);
    }

    /// <summary>
    /// Try to dequeue a single damage event.
    /// </summary>
    public bool TryDequeue(out DamageEvent damageEvent) => _queue.TryDequeue(out damageEvent);

    /// <summary>
    /// Discard all pending damage events.
    /// </summary>
    public void Clear()
    {
        while (_queue.TryDequeue(out _)) { }
    }
}
