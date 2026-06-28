using System.Text;
using System.Text.Json;
using AgenticPlatform.Core.Entities;
using AgenticPlatform.Core.Enums;
using AgenticPlatform.Core.Interfaces;
using AgenticPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AgenticPlatform.Infrastructure.Services;

public sealed class ExecutionService : IExecutionService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ExecutionService> _logger;

    public ExecutionService(
        ApplicationDbContext dbContext,
        IHttpClientFactory httpClientFactory,
        ILogger<ExecutionService> logger)
    {
        _dbContext = dbContext;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task RunExecutionAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        var execution = await _dbContext.Executions
            .Include(item => item.Workflow)
                .ThenInclude(workflow => workflow!.Steps.OrderBy(step => step.Order))
                    .ThenInclude(step => step.Tool)
            .Include(item => item.Agent)
            .FirstOrDefaultAsync(item => item.Id == executionId, cancellationToken);

        if (execution is null)
        {
            _logger.LogWarning("Execution {ExecutionId} was not found.", executionId);
            return;
        }

        execution.Status = ExecutionStatus.Running;
        execution.StartedAt = DateTimeOffset.UtcNow;
        AddLog(execution.Id, ExecutionLogLevel.Information, "Execution started.");
        await _dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            if (execution.TargetType == ExecutionTargetType.Agent)
            {
                await RunAgentAsync(execution, cancellationToken);
            }
            else
            {
                await RunWorkflowAsync(execution, cancellationToken);
            }

            execution.Status = ExecutionStatus.Completed;
            execution.CompletedAt = DateTimeOffset.UtcNow;
            execution.OutputJson = JsonSerializer.Serialize(new
            {
                executionId = execution.Id,
                status = execution.Status.ToString(),
                completedAt = execution.CompletedAt
            });
            AddLog(execution.Id, ExecutionLogLevel.Information, "Execution completed.");
        }
        catch (Exception ex)
        {
            execution.Status = ExecutionStatus.Failed;
            execution.CompletedAt = DateTimeOffset.UtcNow;
            execution.ErrorMessage = ex.Message;
            AddLog(execution.Id, ExecutionLogLevel.Error, "Execution failed.", JsonSerializer.Serialize(new { ex.Message }));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task RunAgentAsync(Execution execution, CancellationToken cancellationToken)
    {
        if (execution.Agent is null)
        {
            throw new InvalidOperationException("Agent target was not found.");
        }

        AddLog(execution.Id, ExecutionLogLevel.Information, $"Simulated agent run for '{execution.Agent.Name}'.");
        await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
    }

    private async Task RunWorkflowAsync(Execution execution, CancellationToken cancellationToken)
    {
        if (execution.Workflow is null)
        {
            throw new InvalidOperationException("Workflow target was not found.");
        }

        AddLog(execution.Id, ExecutionLogLevel.Information, $"Workflow '{execution.Workflow.Name}' started.");

        foreach (var step in execution.Workflow.Steps.OrderBy(step => step.Order))
        {
            try
            {
                AddLog(execution.Id, ExecutionLogLevel.Information, $"Step {step.Order}: {step.Name} started.");

                if (step.StepType == WorkflowStepType.Tool)
                {
                    await RunToolStepAsync(step, execution.InputJson, cancellationToken);
                }
                else
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
                    AddLog(execution.Id, ExecutionLogLevel.Information, $"Simulated nested agent step {step.AgentId}.");
                }

                AddLog(execution.Id, ExecutionLogLevel.Information, $"Step {step.Order}: {step.Name} completed.");
            }
            catch (Exception ex) when (step.ContinueOnError)
            {
                AddLog(execution.Id, ExecutionLogLevel.Warning, $"Step {step.Order}: {step.Name} failed but workflow continued.", JsonSerializer.Serialize(new { ex.Message }));
            }
        }
    }

    private async Task RunToolStepAsync(WorkflowStep step, string inputJson, CancellationToken cancellationToken)
    {
        if (step.Tool is null)
        {
            throw new InvalidOperationException("Tool target was not found.");
        }

        if (Uri.TryCreate(step.Tool.EndpointUrl, UriKind.Absolute, out var endpoint)
            && endpoint.Host.Equals("example.com", StringComparison.OrdinalIgnoreCase))
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
            return;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, step.Tool.EndpointUrl)
        {
            Content = new StringContent(inputJson, Encoding.UTF8, "application/json")
        };

        var client = _httpClientFactory.CreateClient("tool-runner");
        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private void AddLog(Guid executionId, ExecutionLogLevel level, string message, string? detailsJson = null)
    {
        _dbContext.ExecutionLogs.Add(new ExecutionLog
        {
            ExecutionId = executionId,
            Level = level,
            Message = message,
            DetailsJson = detailsJson
        });
    }
}
