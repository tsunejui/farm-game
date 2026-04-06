using MediatR;

namespace FarmGame.Queues;

/// <summary>
/// Queue for INotification events (broadcast, sent to all INotificationHandler).
/// Each item is dispatched via mediator.Publish() sequentially.
/// </summary>
public class EventQueue<TEvent> : BaseQueue<TEvent>
    where TEvent : INotification
{
    protected override void Dispatch(IMediator mediator, TEvent item)
    {
        mediator.Publish(item).GetAwaiter().GetResult();
    }
}
