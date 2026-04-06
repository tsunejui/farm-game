using System;
using MediatR;

namespace FarmGame.Queues;

/// <summary>
/// Thread-safe queue backed by ConcurrentQueue.
/// Items are enqueued during Parallel Update and dispatched
/// via MediatR on the main thread during Process().
/// </summary>
/// <typeparam name="T">The message type (IRequest or INotification).</typeparam>
public interface IGameQueue<T> : IDisposable
{
    /// <summary>Unique identifier for this queue instance.</summary>
    string Id { get; }

    /// <summary>Thread-safe enqueue. Can be called from any thread.</summary>
    void Enqueue(T item);

    /// <summary>Try to dequeue a single item.</summary>
    bool TryDequeue(out T item);

    /// <summary>
    /// Drain the queue and dispatch each item via MediatR.
    /// Must be called on the main thread.
    /// </summary>
    void Process(IMediator mediator);

    /// <summary>Number of items currently queued.</summary>
    int Count { get; }
}
