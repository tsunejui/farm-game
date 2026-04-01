using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace FarmGame.Architecture;

/// <summary>
/// Event bus using MediatR for pub/sub and System.Threading.Channels
/// for high-performance async event queuing.
/// Controllers publish events here; subscribers receive them.
/// </summary>
public class QueueManager : IDisposable
{
    private readonly IMediator _mediator;
    private readonly IServiceProvider _serviceProvider;
    private readonly Channel<INotification> _eventChannel;
    private readonly CancellationTokenSource _cts = new();

    // Pending events accumulated during parallel update, processed on main thread
    private readonly ConcurrentQueue<INotification> _pendingEvents = new();

    public QueueManager()
    {
        var services = new ServiceCollection();
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(QueueManager).Assembly);
        });
        _serviceProvider = services.BuildServiceProvider();
        _mediator = _serviceProvider.GetRequiredService<IMediator>();

        _eventChannel = Channel.CreateUnbounded<INotification>(
            new UnboundedChannelOptions { SingleReader = true });
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
    /// Publish an event immediately via MediatR (main thread only).
    /// </summary>
    public async Task PublishImmediate(INotification notification)
    {
        await _mediator.Publish(notification, _cts.Token);
    }

    /// <summary>
    /// Process all pending events on the main thread.
    /// Called after parallel update completes.
    /// </summary>
    public void ProcessPendingEvents()
    {
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

    /// <summary>Get a service from the DI container (for handler registration).</summary>
    public T GetService<T>() => _serviceProvider.GetService<T>();

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
