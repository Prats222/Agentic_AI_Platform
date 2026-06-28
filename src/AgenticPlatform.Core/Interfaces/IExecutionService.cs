namespace AgenticPlatform.Core.Interfaces;

public interface IExecutionService
{
    Task RunExecutionAsync(Guid executionId, CancellationToken cancellationToken = default);
}
