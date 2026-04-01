using System;
using System.Collections.Generic;
using System.Threading;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using FarmGame.Queues.Commands;
using FarmGame.Queues.Events;

namespace FarmGame.Queues;

/// <summary>
/// Centralized queue manager holding typed sub-queues (Command and Event).
/// Thread-safe: Enqueue from any thread, ProcessAll on main thread.
///
/// Usage:
///   queue.Enqueue(new DamageCommand(...));      // from worker thread
///   queue.Enqueue(new VFXRequestEvent(...));     // from worker thread
///   queue.ProcessAll();                          // main thread, after parallel update
/// </summary>
public class QueueManager : IDisposable
{
    private IMediator _mediator;
    private IServiceProvider _serviceProvider;
    private readonly CancellationTokenSource _cts = new();

    // Typed queue container: Type → IGameQueue instance
    private readonly Dictionary<Type, object> _queues = new();

    // DI builder (handlers registered before Build)
    private readonly ServiceCollection _services = new();
    private bool _built;

    // ─── Concrete Queues ────────────────────────────────────

    // Commands (IRequest → IRequestHandler, modify state)
    public CommandQueue<DamageCommand> Damage { get; } = new();
    public CommandQueue<ActorActionCommand> ActorAction { get; } = new();
    public CommandQueue<SpawnEntityCommand> Spawn { get; } = new();

    // Events (INotification → INotificationHandler, broadcast)
    public EventQueue<InputEvent> Input { get; } = new();
    public EventQueue<VFXRequestEvent> VFX { get; } = new();

    // General notification queue (for events without a dedicated queue)
    public EventQueue<INotification> General { get; } = new();

    public QueueManager()
    {
        _services.AddLogging();

        // Register typed queues in the container
        _queues[typeof(DamageCommand)] = Damage;
        _queues[typeof(ActorActionCommand)] = ActorAction;
        _queues[typeof(SpawnEntityCommand)] = Spawn;
        _queues[typeof(InputEvent)] = Input;
        _queues[typeof(VFXRequestEvent)] = VFX;
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
            // Use reflection to call Enqueue on the correct generic type
            var enqueueMethod = queue.GetType().GetMethod("Enqueue");
            enqueueMethod?.Invoke(queue, new object[] { notification });
            return;
        }

        General.Enqueue(notification);
    }

    // ─── Process (main thread only) ─────────────────────────

    /// <summary>
    /// Drain all queues and dispatch via MediatR. Must be called on main thread.
    /// Order: Commands first (state mutations), then Events (notifications).
    /// </summary>
    public void ProcessAll()
    {
        if (!_built) return;

        // 1. Commands (state-changing, sequential)
        Damage.Process(_mediator);
        ActorAction.Process(_mediator);
        Spawn.Process(_mediator);

        // 2. Events (broadcast notifications)
        Input.Process(_mediator);
        VFX.Process(_mediator);
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

        Log.Information("[QueueManager] Built — {QueueCount} typed queues + General",
            _queues.Count);
    }

    // ─── Diagnostics ────────────────────────────────────────

    /// <summary>Total items across all queues.</summary>
    public int TotalPending =>
        Damage.Count + ActorAction.Count + Spawn.Count +
        Input.Count + VFX.Count + General.Count;

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
