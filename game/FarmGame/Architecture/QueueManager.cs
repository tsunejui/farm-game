using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace FarmGame.Architecture;

/// <summary>
/// Event bus using MediatR for pub/sub and System.Threading.Channels
/// for high-performance async event queuing.
///
/// Controllers are registered as singleton instances so MediatR resolves
/// the existing objects rather than trying to construct new ones.
/// Call Build() after all controllers are registered.
/// </summary>
public class QueueManager : IDisposable
{
    private IMediator _mediator;
    private IServiceProvider _serviceProvider;
    private readonly Channel<INotification> _eventChannel;
    private readonly CancellationTokenSource _cts = new();
    private readonly ConcurrentQueue<INotification> _pendingEvents = new();

    // Collect handler instances before building
    private readonly ServiceCollection _services = new();
    private bool _built;

    public QueueManager()
    {
        _services.AddLogging();
        _eventChannel = Channel.CreateUnbounded<INotification>(
            new UnboundedChannelOptions { SingleReader = true });
    }

    /// <summary>
    /// Register a controller instance as a MediatR notification handler.
    /// Must be called before Build().
    /// </summary>
    public void RegisterHandler(object handler)
    {
        var handlerType = handler.GetType();

        // Register each INotificationHandler<T> interface the handler implements
        foreach (var iface in handlerType.GetInterfaces())
        {
            if (iface.IsGenericType &&
                iface.GetGenericTypeDefinition() == typeof(INotificationHandler<>))
            {
                _services.AddSingleton(iface, handler);
            }
        }
    }

    /// <summary>
    /// Build the DI container and create the MediatR instance.
    /// Call after all controllers are registered via RegisterHandler().
    /// </summary>
    public void Build()
    {
        _services.AddMediatR(cfg =>
        {
            // Don't auto-register from assembly — we registered handlers manually
            cfg.RegisterServicesFromAssembly(typeof(QueueManager).Assembly);
        });

        _serviceProvider = _services.BuildServiceProvider();
        _mediator = _serviceProvider.GetRequiredService<IMediator>();
        _built = true;

        Log.Information("[QueueManager] Built with MediatR");
    }

    /// <summary>
    /// Enqueue an event for processing on the main thread.
    /// Thread-safe: can be called from any thread during parallel update.
    /// </summary>
    public void Publish(INotification notification)
    {
        _pendingEvents.Enqueue(notification);
    }

    /// <summary>
    /// Process all pending events on the main thread.
    /// Called after parallel update completes.
    /// </summary>
    public void ProcessPendingEvents()
    {
        if (!_built) return;

        while (_pendingEvents.TryDequeue(out var notification))
        {
            try
            {
                _mediator.Publish(notification, _cts.Token).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Log.Warning("Event processing error: {Error}", ex.Message);
            }
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
