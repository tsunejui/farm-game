using System;
using System.Collections.Generic;
using System.Threading;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

using FarmGame.Queues;
using FarmGame.Entities.Objects;

namespace FarmGame.Core.Managers;

/// <summary>
/// Centralized queue manager holding typed sub-queues (Command and Event).
/// Thread-safe: Enqueue from any thread, ProcessAll on main thread.
///
/// Usage:
///   manager.Register("input", new EventQueue&lt;InputEvent&gt;());
///   manager.Enqueue(new InputEvent(...));          // from worker thread
///   manager.ProcessAll();                          // main thread
/// </summary>
public class QueueManager : IDisposable
{
    private IMediator _mediator;
    private IServiceProvider _serviceProvider;
    private readonly CancellationTokenSource _cts = new();

    // Queue registry: message Type → queue instance
    private readonly Dictionary<Type, object> _queues = new();
    // Queue registry by string ID → queue instance (for named lookup)
    private readonly Dictionary<string, object> _queuesById = new();

    // DI builder (handlers registered before Build)
    private readonly ServiceCollection _services = new();
    private bool _built;

    // General notification queue (for events without a dedicated queue)
    public EventQueue<INotification> General { get; } = new();

    public QueueManager()
    {
        _services.AddLogging();

        // Register default queues
        Register("input", new EventQueue<InputEvent>());

        General.SetId("general");
        General.Initialize();
        Log.Information("[QueueManager] Registered queue 'general' for type INotification");
    }

    // ─── Queue Registration ─────────────────────────────────

    /// <summary>
    /// Register a typed queue with a string ID.
    /// Sets the queue's ID, calls Initialize(), and indexes it by message type and ID.
    /// </summary>
    public void Register<T>(string id, BaseQueue<T> queue)
    {
        queue.SetId(id);
        queue.Initialize();
        _queues[typeof(T)] = queue;
        _queuesById[id] = queue;

        Log.Information("[QueueManager] Registered queue '{Id}' for type {Type}", id, typeof(T).Name);
    }

    /// <summary>
    /// Get a queue by its message type. Returns null if not found.
    /// </summary>
    public BaseQueue<T> Get<T>()
    {
        return _queues.TryGetValue(typeof(T), out var queue) ? (BaseQueue<T>)queue : null;
    }

    /// <summary>
    /// Get a queue by its string ID. Returns null if not found.
    /// </summary>
    public object GetById(string id)
    {
        return _queuesById.TryGetValue(id, out var queue) ? queue : null;
    }

    /// <summary>
    /// Remove a queue by its message type. Calls Dispose() on the queue.
    /// </summary>
    public bool Remove<T>()
    {
        if (!_queues.TryGetValue(typeof(T), out var queue))
            return false;

        var baseQueue = (BaseQueue<T>)queue;
        var id = baseQueue.Id;

        baseQueue.Dispose();
        _queues.Remove(typeof(T));
        if (id != null)
            _queuesById.Remove(id);

        Log.Debug("[QueueManager] Removed queue '{Id}' for type {Type}", id, typeof(T).Name);
        return true;
    }

    /// <summary>
    /// Remove a queue by its string ID. Calls Dispose() on the queue.
    /// </summary>
    public bool RemoveById(string id)
    {
        if (!_queuesById.TryGetValue(id, out var queue))
            return false;

        if (queue is IDisposable disposable)
            disposable.Dispose();

        _queuesById.Remove(id);

        // Also remove from type-based registry
        Type keyToRemove = null;
        foreach (var kvp in _queues)
        {
            if (ReferenceEquals(kvp.Value, queue))
            {
                keyToRemove = kvp.Key;
                break;
            }
        }
        if (keyToRemove != null)
            _queues.Remove(keyToRemove);

        Log.Debug("[QueueManager] Removed queue '{Id}'", id);
        return true;
    }

    /// <summary>
    /// Remove all registered queues. Calls Dispose() on each.
    /// </summary>
    public void RemoveAll()
    {
        foreach (var queue in _queuesById.Values)
        {
            if (queue is IDisposable disposable)
                disposable.Dispose();
        }

        _queues.Clear();
        _queuesById.Clear();
        _damageQueues.Clear();
        _actionQueues.Clear();

        Log.Debug("[QueueManager] Removed all queues");
    }

    // ─── Enqueue (thread-safe) ──────────────────────────────

    /// <summary>
    /// Enqueue a message to its typed queue. Falls back to General queue for
    /// INotification types without a dedicated queue.
    /// Thread-safe: can be called from any thread during parallel update.
    /// </summary>
    public void Enqueue<T>(T message)
    {
        if (_queues.TryGetValue(typeof(T), out var queue))
        {
            ((IGameQueue<T>)queue).Enqueue(message);
            return;
        }

        // Fallback: INotification → General queue
        if (message is INotification notification)
        {
            General.Enqueue(notification);
            return;
        }

        Log.Warning("[QueueManager] No queue registered for type {Type}", typeof(T).Name);
    }

    /// <summary>Convenience alias for backward compatibility.</summary>
    public void Publish(INotification notification)
    {
        // Try typed queue first
        var type = notification.GetType();
        if (_queues.TryGetValue(type, out var queue))
        {
            var enqueueMethod = queue.GetType().GetMethod("Enqueue");
            enqueueMethod?.Invoke(queue, new object[] { notification });
            return;
        }

        General.Enqueue(notification);
    }

    // ─── Process (main thread only) ─────────────────────────

    /// <summary>
    /// Drain all queues and dispatch via MediatR. Must be called on main thread.
    /// </summary>
    public void ProcessAll()
    {
        if (!_built) return;

        foreach (var queue in _queuesById.Values)
        {
            var processMethod = queue.GetType().GetMethod("Process");
            processMethod?.Invoke(queue, new object[] { _mediator });
        }

        General.Process(_mediator);
    }

    // ─── MediatR DI Setup ───────────────────────────────────

    /// <summary>
    /// Register a handler instance (controller) for MediatR resolution.
    /// Must be called before Build().
    /// </summary>
    public void RegisterHandler(object handler)
    {
        var handlerType = handler.GetType();
        foreach (var iface in handlerType.GetInterfaces())
        {
            if (iface.IsGenericType)
            {
                var genDef = iface.GetGenericTypeDefinition();
                if (genDef == typeof(INotificationHandler<>) ||
                    genDef == typeof(IRequestHandler<,>) ||
                    genDef == typeof(IRequestHandler<>))
                {
                    _services.AddSingleton(iface, handler);
                }
            }
        }
    }

    /// <summary>
    /// Build the DI container and create the MediatR instance.
    /// Call after all handlers are registered.
    /// </summary>
    public void Build()
    {
        _services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(IMediator).Assembly);
        });

        _serviceProvider = _services.BuildServiceProvider();
        _mediator = _serviceProvider.GetRequiredService<IMediator>();
        _built = true;

        Log.Information("[QueueManager] Built - {QueueCount} typed queues + General",
            _queuesById.Count);
    }

    // ─── Per-Object Queues ────────────────────────────────────

    // Object ID → DamageQueue
    private readonly Dictionary<string, DamageQueue> _damageQueues = new();
    // Object ID → ActionQueue
    private readonly Dictionary<string, ActionQueue> _actionQueues = new();

    /// <summary>
    /// Register per-object queues (DamageQueue + ActionQueue) for an object.
    /// Called when an interactable object is created.
    /// </summary>
    public void RegisterObjectQueues(string objectId, DamageQueue damageQueue, ActionQueue actionQueue)
    {
        if (damageQueue != null)
            _damageQueues[objectId] = damageQueue;
        if (actionQueue != null)
            _actionQueues[objectId] = actionQueue;

        Log.Information("[QueueManager] Registered object queues '{DamageId}', '{ActionId}'",
            damageQueue?.Id ?? "null", actionQueue?.Id ?? "null");
    }

    /// <summary>
    /// Remove per-object queues for an object.
    /// Called when an object is destroyed or removed from the world.
    /// </summary>
    public void UnregisterObjectQueues(string objectId)
    {
        bool removed = _damageQueues.Remove(objectId) | _actionQueues.Remove(objectId);
        if (removed)
            Log.Information("[QueueManager] Unregistered object queues for '{ObjectId}'", objectId);
    }

    /// <summary>Get the DamageQueue for a specific object. Returns null if not found.</summary>
    public DamageQueue GetDamageQueue(string objectId) =>
        _damageQueues.TryGetValue(objectId, out var q) ? q : null;

    /// <summary>Get the ActionQueue for a specific object. Returns null if not found.</summary>
    public ActionQueue GetActionQueue(string objectId) =>
        _actionQueues.TryGetValue(objectId, out var q) ? q : null;

    /// <summary>Number of registered per-object queue pairs.</summary>
    public int ObjectQueueCount => _damageQueues.Count;

    // ─── Diagnostics ────────────────────────────────────────

    /// <summary>Total items across all queues.</summary>
    public int TotalPending
    {
        get
        {
            var total = General.Count;
            foreach (var queue in _queuesById.Values)
            {
                var countProp = queue.GetType().GetProperty("Count");
                if (countProp != null)
                    total += (int)countProp.GetValue(queue);
            }
            return total;
        }
    }

    /// <summary>Number of registered queues (excluding General).</summary>
    public int RegisteredCount => _queuesById.Count;

    // ─── Graceful Shutdown ────────────────────────────────────

    /// <summary>
    /// Graceful shutdown: drain all queues in order, then dispose.
    /// Processes remaining events before closing so no data is lost.
    /// </summary>
    public void GracefulShutdown()
    {
        Log.Information("[QueueManager] Graceful shutdown starting...");

        // 1. Drain typed queues (MediatR-based)
        if (_built)
        {
            foreach (var kvp in _queuesById)
            {
                var drainMethod = kvp.Value.GetType().GetMethod("Drain");
                drainMethod?.Invoke(kvp.Value, new object[] { _mediator });
            }

            General.Drain(_mediator);
        }

        // 2. Drain per-object queues
        foreach (var kvp in _damageQueues)
            kvp.Value.Drain();
        foreach (var kvp in _actionQueues)
            kvp.Value.Drain();

        Log.Information("[QueueManager] Graceful shutdown complete — all queues drained");
    }

    public void Dispose()
    {
        GracefulShutdown();
        RemoveAll();
        General.Dispose();
        _cts.Cancel();
        _cts.Dispose();
    }
}
