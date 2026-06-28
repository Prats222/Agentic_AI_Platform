namespace AgenticPlatform.Core.Interfaces;

public interface IExecutionQueue
{
    ValueTask QueueAsync(Guid executionId, CancellationToken cancellationToken = default);
    ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken);
}
