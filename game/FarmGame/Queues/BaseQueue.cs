using System;
using System.Collections.Concurrent;
using MediatR;
using Serilog;

namespace FarmGame.Queues;

/// <summary>
/// Thread-safe base queue backed by ConcurrentQueue.
/// Subclasses define how items are dispatched via MediatR (Send vs Publish).
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public abstract class BaseQueue<T> : IGameQueue<T>, IDisposable
{
    private readonly ConcurrentQueue<T> _queue = new();

    /// <summary>Unique identifier for this queue instance.</summary>
    public string Id { get; private set; }

    public int Count => _queue.Count;

    public void SetId(string id) => Id = id;

    public virtual void Initialize() { }

    public void Enqueue(T item) => _queue.Enqueue(item);

    public bool TryDequeue(out T item) => _queue.TryDequeue(out item);

    /// <summary>
    /// Drain the queue and dispatch each item via MediatR.
    /// Must be called on the main thread.
    /// </summary>
    public void Process(IMediator mediator)
    {
        while (_queue.TryDequeue(out var item))
        {
            try
            {
                Dispatch(mediator, item);
            }
            catch (Exception ex)
            {
                Log.Warning("[{QueueId}] Error dispatching {ItemType}: {Error}",
                    Id, typeof(T).Name, ex.Message);
            }
        }
    }

    protected abstract void Dispatch(IMediator mediator, T item);

    /// <summary>
    /// Graceful shutdown: drain all remaining items via MediatR, then log completion.
    /// </summary>
    public void Drain(IMediator mediator)
    {
        int count = _queue.Count;
        if (count > 0)
        {
            Log.Information("[{QueueId}] Draining {Count} pending items...", Id, count);
            Process(mediator);
            Log.Information("[{QueueId}] Drained", Id);
        }
        else
        {
            Log.Information("[{QueueId}] Empty, nothing to drain", Id);
        }
    }

    public virtual void Dispose() { }
}
