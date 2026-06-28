using AgenticPlatform.Core.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AgenticPlatform.Infrastructure.HealthChecks;

public sealed class ExecutionEngineHealthCheck : IHealthCheck
{
    private readonly IExecutionQueue _executionQueue;

    public ExecutionEngineHealthCheck(IExecutionQueue executionQueue)
    {
        _executionQueue = executionQueue;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        return _executionQueue is null
            ? Task.FromResult(HealthCheckResult.Unhealthy("Execution queue is not available."))
            : Task.FromResult(HealthCheckResult.Healthy("Execution queue is available."));
    }
}
