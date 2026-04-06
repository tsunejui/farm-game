using MediatR;

namespace FarmGame.Queues;

/// <summary>
/// Queue for IRequest&lt;T&gt; commands (modify state, sent to IRequestHandler).
/// Each item is dispatched via mediator.Send() sequentially.
/// </summary>
public class CommandQueue<TCommand, TResponse> : BaseQueue<TCommand>
    where TCommand : IRequest<TResponse>
{
    protected override void Dispatch(IMediator mediator, TCommand item)
    {
        mediator.Send(item).GetAwaiter().GetResult();
    }
}

/// <summary>
/// Convenience for commands with Unit (void) response.
/// </summary>
public class CommandQueue<TCommand> : CommandQueue<TCommand, Unit>
    where TCommand : IRequest<Unit>
{
}
