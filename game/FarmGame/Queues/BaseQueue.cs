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

    /// <summary>
    /// Set the queue identifier. Called once during registration.
    /// </summary>
    public void SetId(string id) => Id = id;

    /// <summary>
    /// Initialization hook. Called after the queue is registered.
    /// Override in subclasses for custom setup logic.
    /// </summary>
    public virtual void Initialize() { }

    /// <summary>Thread-safe enqueue. Can be called from any thread.</summary>
    public void Enqueue(T item) => _queue.Enqueue(item);

    /// <summary>
    /// Try to dequeue a single item from the queue.
    /// </summary>
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
                Log.Warning("[{QueueType}<{ItemType}>] Error: {Error}",
                    GetType().Name, typeof(T).Name, ex.Message);
            }
        }
    }

    /// <summary>
    /// Subclasses implement the actual MediatR dispatch strategy.
    /// </summary>
    protected abstract void Dispatch(IMediator mediator, T item);

    /// <summary>
    /// Cleanup hook called when the queue is removed.
    /// Override in subclasses for custom teardown logic.
    /// </summary>
    public virtual void Dispose() { }
}
