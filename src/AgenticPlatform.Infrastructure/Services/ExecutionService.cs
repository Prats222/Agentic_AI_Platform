using System.Text.Json;
using AgenticPlatform.Core.Entities;
using AgenticPlatform.Core.Enums;
using AgenticPlatform.Core.Interfaces;
using AgenticPlatform.Core.Models.LLM;
using AgenticPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AgenticPlatform.Infrastructure.Services;

public sealed class ExecutionService : IExecutionService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IAISettingsService _aiSettingsService;
    private readonly ILLMProviderFactory _llmProviderFactory;
    private readonly IToolExecutionService _toolExecutionService;
    private readonly ILogger<ExecutionService> _logger;

    public ExecutionService(
        ApplicationDbContext dbContext,
        IAISettingsService aiSettingsService,
        ILLMProviderFactory llmProviderFactory,
        IToolExecutionService toolExecutionService,
        ILogger<ExecutionService> logger)
    {
        _dbContext = dbContext;
        _aiSettingsService = aiSettingsService;
        _llmProviderFactory = llmProviderFactory;
        _toolExecutionService = toolExecutionService;
        _logger = logger;
    }

    public async Task RunExecutionAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        var execution = await _dbContext.Executions
            .Include(item => item.Workflow)
                .ThenInclude(workflow => workflow!.Steps.OrderBy(step => step.Order))
                    .ThenInclude(step => step.Tool)
            .Include(item => item.Workflow)
                .ThenInclude(workflow => workflow!.Steps.OrderBy(step => step.Order))
                    .ThenInclude(step => step.Agent)
            .Include(item => item.Agent)
            .FirstOrDefaultAsync(item => item.Id == executionId, cancellationToken);

        if (execution is null)
        {
            _logger.LogWarning("Execution {ExecutionId} was not found.", executionId);
            return;
        }

        execution.Status = ExecutionStatus.Running;
        execution.StartedAt = DateTimeOffset.UtcNow;
        var executionStartedAt = DateTimeOffset.UtcNow;
        AddLog(execution.Id, ExecutionLogLevel.Information, "Execution started.");
        await _dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            string outputJson;

            if (execution.TargetType == ExecutionTargetType.Agent)
            {
                outputJson = await RunAgentAsync(execution, cancellationToken);
            }
            else
            {
                outputJson = await RunWorkflowAsync(execution, cancellationToken);
            }

            execution.OutputJson = outputJson;
            execution.DurationMs = (DateTimeOffset.UtcNow - executionStartedAt).TotalMilliseconds;
            if (execution.Status == ExecutionStatus.Running)
            {
                execution.Status = ExecutionStatus.Completed;
                execution.CompletedAt = DateTimeOffset.UtcNow;
                AddLog(execution.Id, ExecutionLogLevel.Information, "Execution completed.");
            }
        }
        catch (Exception ex)
        {
            execution.Status = ExecutionStatus.Failed;
            execution.CompletedAt = DateTimeOffset.UtcNow;
            execution.DurationMs = (DateTimeOffset.UtcNow - executionStartedAt).TotalMilliseconds;
            execution.ErrorMessage = ex.Message;
            AddLog(execution.Id, ExecutionLogLevel.Error, "Execution failed.", JsonSerializer.Serialize(new { ex.Message }));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> RunAgentAsync(Execution execution, CancellationToken cancellationToken)
    {
        if (execution.Agent is null)
        {
            throw new InvalidOperationException("Agent target was not found.");
        }

        var prompt = BuildPromptFromJson(execution.InputJson);
        var response = await RunAgentByIdAsync(execution.Agent.Id, prompt, execution.Id, cancellationToken);

        return JsonSerializer.Serialize(new
        {
            executionId = execution.Id,
            targetType = execution.TargetType.ToString(),
            agentId = execution.Agent.Id,
            agentName = execution.Agent.Name,
            input = TryDeserialize(execution.InputJson),
            output = response.Content,
            provider = response.Provider.ToString(),
            model = response.Model,
            completedAt = DateTimeOffset.UtcNow
        });
    }

    private async Task<string> RunWorkflowAsync(Execution execution, CancellationToken cancellationToken)
    {
        if (execution.Workflow is null)
        {
            throw new InvalidOperationException("Workflow target was not found.");
        }

        AddLog(execution.Id, ExecutionLogLevel.Information, $"Workflow '{execution.Workflow.Name}' started.");
        var originalInputJson = execution.InputJson;
        var currentInputJson = execution.InputJson;
        var stepOutputs = new List<WorkflowStepRuntimeOutput>();

        foreach (var step in execution.Workflow.Steps.OrderBy(step => step.Order))
        {
            try
            {
                AddLog(execution.Id, ExecutionLogLevel.Information, $"Step {step.Order}: {step.Name} started.");
                var stepInputJson = BuildStepInputJson(step, originalInputJson, currentInputJson, stepOutputs);
                string stepOutputJson;

                if (step.StepType == WorkflowStepType.HumanApproval)
                {
                    stepOutputJson = await RunHumanApprovalStepAsync(execution, step, stepInputJson, cancellationToken);
                }
                else if (step.StepType == WorkflowStepType.Tool)
                {
                    stepOutputJson = await RunToolStepAsync(execution.Id, step, stepInputJson, cancellationToken);
                }
                else
                {
                    stepOutputJson = await RunAgentStepAsync(execution.Id, step, stepInputJson, cancellationToken);
                }

                currentInputJson = stepOutputJson;
                stepOutputs.Add(new WorkflowStepRuntimeOutput(
                    step.Id,
                    step.Name,
                    step.Order,
                    step.StepType,
                    stepInputJson,
                    stepOutputJson,
                    null));
                AddLog(execution.Id, ExecutionLogLevel.Information, $"Step {step.Order}: {step.Name} completed.");

                if (execution.Status == ExecutionStatus.WaitingForApproval)
                {
                    break;
                }
            }
            catch (Exception ex) when (step.ContinueOnError)
            {
                var errorJson = JsonSerializer.Serialize(new
                {
                    error = ex.Message
                });
                currentInputJson = errorJson;
                stepOutputs.Add(new WorkflowStepRuntimeOutput(
                    step.Id,
                    step.Name,
                    step.Order,
                    step.StepType,
                    "{}",
                    errorJson,
                    ex.Message));
                AddLog(execution.Id, ExecutionLogLevel.Warning, $"Step {step.Order}: {step.Name} failed but workflow continued.", JsonSerializer.Serialize(new { ex.Message }));
            }
        }

        var finalOutputJson = stepOutputs.LastOrDefault(stepOutput => stepOutput.StepType != WorkflowStepType.HumanApproval && stepOutput.Error is null)?.OutputJson
            ?? currentInputJson;

        return JsonSerializer.Serialize(new
        {
            executionId = execution.Id,
            targetType = execution.TargetType.ToString(),
            workflowId = execution.Workflow.Id,
            workflowName = execution.Workflow.Name,
            input = TryDeserialize(execution.InputJson),
            finalOutput = TryDeserialize(finalOutputJson),
            runtimeState = TryDeserialize(currentInputJson),
            steps = stepOutputs.Select(stepOutput => new
            {
                stepId = stepOutput.StepId,
                stepName = stepOutput.StepName,
                order = stepOutput.Order,
                stepType = stepOutput.StepType.ToString(),
                input = TryDeserialize(stepOutput.InputJson),
                output = TryDeserialize(stepOutput.OutputJson),
                error = stepOutput.Error
            }),
            completedAt = DateTimeOffset.UtcNow
        });
    }

    private static string BuildStepInputJson(
        WorkflowStep step,
        string originalInputJson,
        string previousOutputJson,
        IReadOnlyCollection<WorkflowStepRuntimeOutput> stepOutputs)
    {
        using var document = JsonDocument.Parse(step.InputMappingJson);
        var mapping = document.RootElement;

        if (mapping.ValueKind != JsonValueKind.Object || !mapping.EnumerateObject().Any())
        {
            return previousOutputJson;
        }

        var selectedJson = ResolveMappingSource(mapping, originalInputJson, previousOutputJson, stepOutputs);

        if (TryGetStringProperty(mapping, "template", out var template))
        {
            var rendered = RenderTemplate(template, originalInputJson, previousOutputJson, stepOutputs);
            var wrapAs = TryGetStringProperty(mapping, "wrapAs", out var wrapAsValue)
                ? wrapAsValue
                : "prompt";

            return JsonSerializer.Serialize(new Dictionary<string, string>
            {
                [wrapAs] = rendered
            });
        }

        if (TryGetStringProperty(mapping, "wrapAs", out var selectedWrapAs))
        {
            return JsonSerializer.Serialize(new Dictionary<string, object?>
            {
                [selectedWrapAs] = TryDeserialize(selectedJson)
            });
        }

        return selectedJson;
    }

    private static string ResolveMappingSource(
        JsonElement mapping,
        string originalInputJson,
        string previousOutputJson,
        IReadOnlyCollection<WorkflowStepRuntimeOutput> stepOutputs)
    {
        var source = TryGetStringProperty(mapping, "source", out var sourceValue)
            ? sourceValue
            : "previous";

        if (source.Equals("original", StringComparison.OrdinalIgnoreCase))
        {
            return originalInputJson;
        }

        if (source.Equals("step", StringComparison.OrdinalIgnoreCase))
        {
            var stepOutput = ResolveStepOutput(mapping, stepOutputs);
            return stepOutput?.OutputJson ?? previousOutputJson;
        }

        return previousOutputJson;
    }

    private static WorkflowStepRuntimeOutput? ResolveStepOutput(
        JsonElement mapping,
        IReadOnlyCollection<WorkflowStepRuntimeOutput> stepOutputs)
    {
        if (mapping.TryGetProperty("stepOrder", out var stepOrderElement)
            && stepOrderElement.ValueKind == JsonValueKind.Number
            && stepOrderElement.TryGetInt32(out var stepOrder))
        {
            return stepOutputs.LastOrDefault(step => step.Order == stepOrder);
        }

        if (TryGetStringProperty(mapping, "stepName", out var stepName))
        {
            return stepOutputs.LastOrDefault(step => step.StepName.Equals(stepName, StringComparison.OrdinalIgnoreCase));
        }

        return stepOutputs.LastOrDefault();
    }

    private static string RenderTemplate(
        string template,
        string originalInputJson,
        string previousOutputJson,
        IReadOnlyCollection<WorkflowStepRuntimeOutput> stepOutputs)
    {
        var rendered = template
            .Replace("{{original}}", JsonToText(originalInputJson), StringComparison.OrdinalIgnoreCase)
            .Replace("{{previous}}", JsonToText(previousOutputJson), StringComparison.OrdinalIgnoreCase);

        rendered = ReplaceKnownJsonFields(rendered, "original", originalInputJson);
        rendered = ReplaceKnownJsonFields(rendered, "previous", previousOutputJson);

        foreach (var stepOutput in stepOutputs)
        {
            var key = $"step{stepOutput.Order}";
            rendered = rendered.Replace($"{{{{{key}}}}}", JsonToText(stepOutput.OutputJson), StringComparison.OrdinalIgnoreCase);
            rendered = ReplaceKnownJsonFields(rendered, key, stepOutput.OutputJson);
        }

        return rendered;
    }

    private static string ReplaceKnownJsonFields(string value, string prefix, string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            return value;
        }

        foreach (var property in root.EnumerateObject())
        {
            value = value.Replace(
                $"{{{{{prefix}.{property.Name}}}}}",
                JsonElementToText(property.Value),
                StringComparison.OrdinalIgnoreCase);
        }

        return value;
    }

    private static string JsonToText(string json)
    {
        using var document = JsonDocument.Parse(json);
        return JsonElementToText(document.RootElement);
    }

    private static string JsonElementToText(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => string.Empty,
            _ => element.GetRawText()
        };
    }

    private async Task<string> RunToolStepAsync(Guid executionId, WorkflowStep step, string inputJson, CancellationToken cancellationToken)
    {
        if (step.ToolId is null)
        {
            throw new InvalidOperationException("Tool target was not found.");
        }

        var result = await _toolExecutionService.ExecuteAsync(step.ToolId.Value, inputJson, cancellationToken);
        if (result is null)
        {
            throw new InvalidOperationException("Tool target was not found.");
        }

        AddLog(
            executionId,
            result.Succeeded ? ExecutionLogLevel.Information : ExecutionLogLevel.Error,
            $"Tool '{result.ToolName}' executed by {result.ExecutorName}.",
            JsonSerializer.Serialize(new
            {
                result.Succeeded,
                result.DurationMs,
                result.ErrorMessage
            }));

        if (!result.Succeeded)
        {
            throw new InvalidOperationException(result.ErrorMessage ?? "Tool execution failed.");
        }

        return result.ResultJson;
    }

    private async Task<string> RunAgentStepAsync(Guid executionId, WorkflowStep step, string inputJson, CancellationToken cancellationToken)
    {
        if (step.AgentId is null)
        {
            throw new InvalidOperationException("Agent target was not found.");
        }

        var prompt = BuildPromptFromJson(inputJson);
        var response = await RunAgentByIdAsync(step.AgentId.Value, prompt, executionId, cancellationToken);

        return JsonSerializer.Serialize(new
        {
            agentId = step.AgentId.Value,
            output = response.Content,
            provider = response.Provider.ToString(),
            model = response.Model
        });
    }

    private async Task<string> RunHumanApprovalStepAsync(
        Execution execution,
        WorkflowStep step,
        string inputJson,
        CancellationToken cancellationToken)
    {
        var existingApproval = await _dbContext.HumanApprovalRequests
            .FirstOrDefaultAsync(item => item.ExecutionId == execution.Id && item.WorkflowStepId == step.Id, cancellationToken);

        if (existingApproval?.IsApproved == true)
        {
            AddLog(execution.Id, ExecutionLogLevel.Information, $"Human approval already approved at step {step.Order}: {step.Name}.");
            return inputJson;
        }

        if (existingApproval?.IsRejected == true)
        {
            throw new InvalidOperationException($"Human approval was rejected at step {step.Order}: {step.Name}.");
        }

        var instructions = "Review the previous step output before continuing this workflow.";
        try
        {
            using var configuration = JsonDocument.Parse(step.ConfigurationJson);
            if (TryGetStringProperty(configuration.RootElement, "instructions", out var configuredInstructions))
            {
                instructions = configuredInstructions;
            }
        }
        catch (JsonException)
        {
            // Keep default instructions when step configuration is malformed.
        }

        if (existingApproval is null)
        {
            await _dbContext.HumanApprovalRequests.AddAsync(new HumanApprovalRequest
            {
                ExecutionId = execution.Id,
                WorkflowStepId = step.Id,
                Title = step.Name,
                Instructions = instructions,
                PayloadJson = inputJson
            }, cancellationToken);
        }

        execution.Status = ExecutionStatus.WaitingForApproval;
        execution.CompletedAt = DateTimeOffset.UtcNow;
        AddLog(execution.Id, ExecutionLogLevel.Warning, $"Human approval required at step {step.Order}: {step.Name}.");

        return JsonSerializer.Serialize(new
        {
            waitingForApproval = true,
            stepId = step.Id,
            stepName = step.Name,
            instructions,
            payload = TryDeserialize(inputJson)
        });
    }

    private async Task<AgentRunResult> RunAgentByIdAsync(Guid agentId, string prompt, Guid executionId, CancellationToken cancellationToken)
    {
        var chatRequest = await _aiSettingsService.BuildChatRequestAsync(agentId, prompt, cancellationToken);
        if (chatRequest is null)
        {
            throw new InvalidOperationException("Agent target was not found.");
        }

        AddLog(executionId, ExecutionLogLevel.Information, $"Calling {chatRequest.Provider} model '{chatRequest.Model}'.");

        var provider = _llmProviderFactory.GetProvider(chatRequest.Provider);
        var response = await provider.ChatAsync(chatRequest, cancellationToken);
        var execution = await _dbContext.Executions.FirstOrDefaultAsync(item => item.Id == executionId, cancellationToken);
        if (execution is not null)
        {
            execution.Provider = chatRequest.Provider.ToString();
            execution.Model = chatRequest.Model;
            execution.EstimatedInputTokens = EstimateTokens(prompt);
            execution.EstimatedOutputTokens = EstimateTokens(response.Content);
        }

        AddLog(executionId, ExecutionLogLevel.Information, "LLM response received.", JsonSerializer.Serialize(new
        {
            chatRequest.Provider,
            chatRequest.Model,
            responseLength = response.Content.Length
        }));

        return new AgentRunResult(chatRequest.Provider, chatRequest.Model, response.Content);
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

    private static string BuildPromptFromJson(string inputJson)
    {
        using var document = JsonDocument.Parse(inputJson);
        var root = document.RootElement;

        if (root.ValueKind == JsonValueKind.Object)
        {
            if (TryGetStringProperty(root, "prompt", out var prompt))
            {
                return prompt;
            }

            if (TryGetStringProperty(root, "question", out var question))
            {
                return question;
            }

            if (TryGetStringProperty(root, "input", out var input))
            {
                return input;
            }

            if (TryGetStringProperty(root, "output", out var output))
            {
                return output;
            }
        }

        return root.GetRawText();
    }

    private static bool TryGetStringProperty(JsonElement root, string propertyName, out string value)
    {
        if (root.TryGetProperty(propertyName, out var element)
            && element.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(element.GetString()))
        {
            value = element.GetString()!;
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static object? TryDeserialize(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<object>(json);
        }
        catch (JsonException)
        {
            return json;
        }
    }

    private static int EstimateTokens(string value)
    {
        return Math.Max(1, (int)Math.Ceiling(value.Length / 4.0));
    }

    private sealed record AgentRunResult(AIProvider Provider, string Model, string Content);

    private sealed record WorkflowStepRuntimeOutput(
        Guid StepId,
        string StepName,
        int Order,
        WorkflowStepType StepType,
        string InputJson,
        string OutputJson,
        string? Error);
}
