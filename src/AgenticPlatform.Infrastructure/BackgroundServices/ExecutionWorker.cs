using AgenticPlatform.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AgenticPlatform.Infrastructure.BackgroundServices;

public sealed class ExecutionWorker : BackgroundService
{
    private readonly IExecutionQueue _executionQueue;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<ExecutionWorker> _logger;

    public ExecutionWorker(
        IExecutionQueue executionQueue,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<ExecutionWorker> logger)
    {
        _executionQueue = executionQueue;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var executionId = await _executionQueue.DequeueAsync(stoppingToken);

            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var executionService = scope.ServiceProvider.GetRequiredService<IExecutionService>();
                await executionService.RunExecutionAsync(executionId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error while processing execution {ExecutionId}.", executionId);
            }
        }
    }
}
