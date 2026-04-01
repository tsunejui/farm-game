using System.Collections.Concurrent;
using MediatR;
using Serilog;

namespace FarmGame.Queues;

/// <summary>
/// Queue for IRequest&lt;T&gt; commands (modify state, sent to IRequestHandler).
/// Each item is dispatched via mediator.Send() sequentially.
/// </summary>
public class CommandQueue<TCommand, TResponse> : IGameQueue<TCommand>
    where TCommand : IRequest<TResponse>
{
    private readonly ConcurrentQueue<TCommand> _queue = new();

    public int Count => _queue.Count;

    public void Enqueue(TCommand item) => _queue.Enqueue(item);

    public void Process(IMediator mediator)
    {
        while (_queue.TryDequeue(out var cmd))
        {
            try
            {
                mediator.Send(cmd).GetAwaiter().GetResult();
            }
            catch (System.Exception ex)
            {
                Log.Warning("[CommandQueue<{Type}>] Error: {Error}",
                    typeof(TCommand).Name, ex.Message);
            }
        }
    }
}

/// <summary>
/// Convenience for commands with Unit (void) response.
/// </summary>
public class CommandQueue<TCommand> : CommandQueue<TCommand, Unit>
    where TCommand : IRequest<Unit>
{
}
