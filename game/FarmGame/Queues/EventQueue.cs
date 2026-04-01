using System.Collections.Concurrent;
using MediatR;
using Serilog;

namespace FarmGame.Queues;

/// <summary>
/// Queue for INotification events (broadcast, sent to all INotificationHandler).
/// Each item is dispatched via mediator.Publish() sequentially.
/// </summary>
public class EventQueue<TEvent> : IGameQueue<TEvent>
    where TEvent : INotification
{
    private readonly ConcurrentQueue<TEvent> _queue = new();

    public int Count => _queue.Count;

    public void Enqueue(TEvent item) => _queue.Enqueue(item);

    public void Process(IMediator mediator)
    {
        while (_queue.TryDequeue(out var evt))
        {
            try
            {
                mediator.Publish(evt).GetAwaiter().GetResult();
            }
            catch (System.Exception ex)
            {
                Log.Warning("[EventQueue<{Type}>] Error: {Error}",
                    typeof(TEvent).Name, ex.Message);
            }
        }
    }
}
