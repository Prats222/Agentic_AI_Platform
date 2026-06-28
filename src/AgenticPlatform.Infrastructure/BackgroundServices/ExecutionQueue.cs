using System.Threading.Channels;
using AgenticPlatform.Core.Interfaces;

namespace AgenticPlatform.Infrastructure.BackgroundServices;

public sealed class ExecutionQueue : IExecutionQueue
{
    private readonly Channel<Guid> _queue = Channel.CreateUnbounded<Guid>();

    public async ValueTask QueueAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        await _queue.Writer.WriteAsync(executionId, cancellationToken);
    }

    public async ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
}
