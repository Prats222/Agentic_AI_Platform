using System.Text.Json;
using System.Text.RegularExpressions;
using AgenticPlatform.Core.Constants;
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
                        .ThenInclude(agent => agent!.ContextDocuments)
            .Include(item => item.Agent)
                .ThenInclude(agent => agent!.ContextDocuments)
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
        var agent = await _dbContext.Agents
            .AsNoTracking()
            .Include(item => item.ContextDocuments)
            .Include(item => item.Tools)
            .FirstOrDefaultAsync(item => item.Id == agentId, cancellationToken);

        if (agent is null)
        {
            throw new InvalidOperationException("Agent target was not found.");
        }

        var webContext = await TryBuildWebSearchContextAsync(agent, prompt, executionId, cancellationToken);
        var toolContexts = await ExecuteMatchingAttachedToolsAsync(agent, prompt, executionId, cancellationToken);
        var enrichedPrompt = BuildAgentRuntimePrompt(agent, prompt, webContext, toolContexts);
        var chatRequest = await _aiSettingsService.BuildChatRequestAsync(agentId, prompt, cancellationToken);
        if (chatRequest is null)
        {
            throw new InvalidOperationException("Agent target was not found.");
        }
        var messages = chatRequest.Messages
            .Select((message, index) => index == chatRequest.Messages.Count - 1
                ? new LLMChatMessage { Role = message.Role, Content = enrichedPrompt }
                : message)
            .ToList();
        chatRequest.Messages = messages;

        AddLog(executionId, ExecutionLogLevel.Information, $"Calling {chatRequest.Provider} model '{chatRequest.Model}'.");

        var provider = _llmProviderFactory.GetProvider(chatRequest.Provider);
        var totalInputTokens = 0;
        var totalOutputTokens = 0;
        string? finalContent = null;

        for (var round = 0; round < 4; round++)
        {
            totalInputTokens += EstimateTokens(string.Join("\n", messages.Select(message => message.Content)));
            var response = await provider.ChatAsync(chatRequest, cancellationToken);
            totalOutputTokens += EstimateTokens(response.Content);

            if (!TryParseToolCall(response.Content, agent.Tools, out var toolCall))
            {
                finalContent = response.Content;
                break;
            }

            if (round == 3)
            {
                throw new InvalidOperationException("The agent exceeded the maximum of three tool calls without producing a final answer.");
            }

            var tool = agent.Tools.FirstOrDefault(item => item.IsEnabled && ToolMatches(item, toolCall.ToolReference));
            if (tool is null)
            {
                throw new InvalidOperationException($"The model requested an unavailable tool: {toolCall.ToolReference}.");
            }

            AddLog(executionId, ExecutionLogLevel.Information, $"Agent requested attached tool '{tool.Name}'.", toolCall.ArgumentsJson);
            var toolResult = await _toolExecutionService.ExecuteAsync(tool.Id, toolCall.ArgumentsJson, cancellationToken);
            if (toolResult is null || !toolResult.Succeeded || string.IsNullOrWhiteSpace(toolResult.ResultJson))
            {
                var error = toolResult?.ErrorMessage ?? "The tool returned no result.";
                AddLog(executionId, ExecutionLogLevel.Error, $"Attached tool '{tool.Name}' failed.", JsonSerializer.Serialize(new { error }));
                throw new InvalidOperationException($"Attached tool '{tool.Name}' failed: {error}");
            }

            if (TryReadToolFailure(toolResult.ResultJson, out var toolError))
            {
                AddLog(executionId, ExecutionLogLevel.Error, $"Attached tool '{tool.Name}' returned an error.", toolResult.ResultJson);
                throw new InvalidOperationException($"Attached tool '{tool.Name}' failed: {toolError}");
            }

            AddLog(executionId, ExecutionLogLevel.Information, $"Attached tool '{tool.Name}' executed successfully.", toolResult.ResultJson);
            messages.Add(new LLMChatMessage { Role = "assistant", Content = response.Content });
            messages.Add(new LLMChatMessage
            {
                Role = "user",
                Content = $"""
                VERIFIED TOOL RESULT from {tool.Name}:
                {Truncate(toolResult.ResultJson, 16000)}

                The tool was executed by PratsPilot. Use this verified result to complete the original request. If another tool is essential, return only the tool-call JSON protocol. Otherwise return the final user-facing answer and do not repeat tool-call JSON.
                """
            });
            chatRequest.Messages = messages;
        }

        if (string.IsNullOrWhiteSpace(finalContent))
        {
            throw new InvalidOperationException("The agent did not produce a final answer after tool execution.");
        }

        var execution = await _dbContext.Executions.FirstOrDefaultAsync(item => item.Id == executionId, cancellationToken);
        if (execution is not null)
        {
            execution.Provider = chatRequest.Provider.ToString();
            execution.Model = chatRequest.Model;
            execution.EstimatedInputTokens = totalInputTokens;
            execution.EstimatedOutputTokens = totalOutputTokens;
        }

        AddLog(executionId, ExecutionLogLevel.Information, "LLM response received.", JsonSerializer.Serialize(new
        {
            chatRequest.Provider,
            chatRequest.Model,
            responseLength = finalContent.Length
        }));

        return new AgentRunResult(chatRequest.Provider, chatRequest.Model, finalContent);
    }

    private async Task<string?> TryBuildWebSearchContextAsync(Agent agent, string prompt, Guid executionId, CancellationToken cancellationToken)
    {
        var webSearchTool = agent.Tools.FirstOrDefault(tool =>
            tool.Category.Equals(BuiltInToolCategories.WebSearch, StringComparison.OrdinalIgnoreCase));

        if (webSearchTool is null || !ShouldUseWebSearch(prompt))
        {
            return null;
        }

        try
        {
            AddLog(executionId, ExecutionLogLevel.Information, $"Executing attached web search tool '{webSearchTool.Name}'.");
            var inputJson = JsonSerializer.Serialize(new { query = prompt });
            var result = await _toolExecutionService.ExecuteAsync(webSearchTool.Id, inputJson, cancellationToken);
            if (result is null || !result.Succeeded || string.IsNullOrWhiteSpace(result.ResultJson))
            {
                AddLog(executionId, ExecutionLogLevel.Warning, "Attached web search tool did not return a usable result.", JsonSerializer.Serialize(new { result?.ErrorMessage }));
                return null;
            }

            AddLog(executionId, ExecutionLogLevel.Information, "Web search context received.", result.ResultJson);
            return result.ResultJson;
        }
        catch (Exception ex)
        {
            AddLog(executionId, ExecutionLogLevel.Warning, "Attached web search tool failed.", JsonSerializer.Serialize(new { ex.Message }));
            return null;
        }
    }

    private static bool ShouldUseWebSearch(string prompt)
    {
        var text = prompt.ToLowerInvariant();
        return new[] { "today", "latest", "current", "weather", "score", "news", "live", "now", "yesterday", "this week", "search", "web" }
            .Any(text.Contains);
    }

    private async Task<IReadOnlyCollection<AttachedToolContext>> ExecuteMatchingAttachedToolsAsync(
        Agent agent,
        string prompt,
        Guid executionId,
        CancellationToken cancellationToken)
    {
        var matchingTools = agent.Tools
            .Where(tool => tool.IsEnabled
                && !tool.Category.Equals(BuiltInToolCategories.WebSearch, StringComparison.OrdinalIgnoreCase)
                && InputMatchesToolSchema(prompt, tool.InputSchemaJson))
            .ToArray();

        if (matchingTools.Length == 0)
        {
            return Array.Empty<AttachedToolContext>();
        }

        var contexts = new List<AttachedToolContext>(matchingTools.Length);
        foreach (var tool in matchingTools)
        {
            AddLog(executionId, ExecutionLogLevel.Information, $"Executing attached tool '{tool.Name}'.");
            var result = await _toolExecutionService.ExecuteAsync(tool.Id, prompt, cancellationToken);
            if (result is null || !result.Succeeded || string.IsNullOrWhiteSpace(result.ResultJson))
            {
                var error = result?.ErrorMessage ?? "The tool returned no result.";
                AddLog(executionId, ExecutionLogLevel.Error, $"Attached tool '{tool.Name}' failed.", JsonSerializer.Serialize(new { error }));
                throw new InvalidOperationException($"Attached tool '{tool.Name}' failed: {error}");
            }

            if (TryReadToolFailure(result.ResultJson, out var toolError))
            {
                AddLog(executionId, ExecutionLogLevel.Error, $"Attached tool '{tool.Name}' returned an error.", result.ResultJson);
                throw new InvalidOperationException($"Attached tool '{tool.Name}' failed: {toolError}");
            }

            AddLog(executionId, ExecutionLogLevel.Information, $"Attached tool '{tool.Name}' returned grounded context.", result.ResultJson);
            contexts.Add(new AttachedToolContext(tool.Name, result.ResultJson));
        }

        return contexts;
    }

    private static bool InputMatchesToolSchema(string inputJson, string schemaJson)
    {
        try
        {
            using var inputDocument = JsonDocument.Parse(inputJson);
            using var schemaDocument = JsonDocument.Parse(schemaJson);
            if (inputDocument.RootElement.ValueKind != JsonValueKind.Object
                || schemaDocument.RootElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            var input = inputDocument.RootElement;
            var schema = schemaDocument.RootElement;
            var requiredProperties = schema.TryGetProperty("required", out var required)
                && required.ValueKind == JsonValueKind.Array
                    ? required.EnumerateArray()
                        .Where(item => item.ValueKind == JsonValueKind.String)
                        .Select(item => item.GetString()!)
                        .ToArray()
                    : Array.Empty<string>();

            if (requiredProperties.Length > 0)
            {
                return requiredProperties.All(property => input.TryGetProperty(property, out _));
            }

            if (!schema.TryGetProperty("properties", out var properties)
                || properties.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            return properties.EnumerateObject().Any(property => input.TryGetProperty(property.Name, out _));
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryReadToolFailure(string resultJson, out string error)
    {
        try
        {
            using var document = JsonDocument.Parse(resultJson);
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty("success", out var success)
                && success.ValueKind == JsonValueKind.False)
            {
                error = root.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String
                    ? message.GetString() ?? "The tool reported an error."
                    : root.TryGetProperty("error", out var errorValue) && errorValue.ValueKind == JsonValueKind.String
                        ? errorValue.GetString() ?? "The tool reported an error."
                        : "The tool reported an error.";
                return true;
            }
        }
        catch (JsonException)
        {
            // A successful executor may return plain text, which is still valid grounding context.
        }

        error = string.Empty;
        return false;
    }

    private static string BuildAgentRuntimePrompt(
        Agent agent,
        string userPrompt,
        string? webSearchContext,
        IReadOnlyCollection<AttachedToolContext> toolContexts)
    {
        var sections = new List<string>
        {
            userPrompt
        };

        if (!string.IsNullOrWhiteSpace(webSearchContext))
        {
            sections.Add($"""
            Live web search context from an attached tool:
            {Truncate(webSearchContext, 8000)}

            Answer using this live web search context. Mention uncertainty if the snippets are not enough.
            """);
        }

        if (toolContexts.Count > 0)
        {
            sections.Add($"""
            Verified output from tools executed by the platform:
            {string.Join("\n\n", toolContexts.Select(context => $"### {context.ToolName}\n{Truncate(context.ResultJson, 12000)}"))}

            Base the answer only on the verified tool output above. Do not invent repository files, technologies, metadata, or results that are absent from it. If the data is insufficient, state exactly what is missing.
            """);
        }

        if (agent.ContextDocuments.Count > 0)
        {
            var contextBlocks = agent.ContextDocuments
                .Where(document => !string.IsNullOrWhiteSpace(document.ExtractedText))
                .Select(document => $"### {document.Name}\n{Truncate(document.ExtractedText, 6000)}")
                .ToArray();

            if (contextBlocks.Length > 0)
            {
                sections.Add($"""
                Attached knowledge base context:
                {string.Join("\n\n", contextBlocks)}

                Use the attached context when it is relevant. If the answer is present in the context, answer from it directly.
                """);
            }
        }

        if (agent.Tools.Count > 0)
        {
            const string toolCallProtocol = "{\"toolCall\":{\"toolId\":\"the tool ID above\",\"arguments\":{\"schema_field\":\"value\"}}}";
            sections.Add($"""
            Available tools attached to this agent:
            {string.Join("\n\n", agent.Tools.Where(tool => tool.IsEnabled).Select(tool => $"- Tool ID: {tool.Id}\n  Name: {tool.Name}\n  Category: {tool.Category}\n  Description: {tool.Description}\n  Input schema: {tool.InputSchemaJson}"))}

            To execute a tool, return ONLY valid JSON in this exact format, with no markdown or explanation:
            {toolCallProtocol}

            PratsPilot will execute it securely and send the verified result back to you. Never merely describe a tool call, print a Python-style function call, or claim a tool succeeded before receiving its verified result. If tool results are already provided above, use them directly.
            """);
        }

        return string.Join("\n\n", sections);
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength] + "\n...[truncated]";
    }

    private static bool TryParseToolCall(string content, IEnumerable<Tool> tools, out RequestedToolCall toolCall)
    {
        var candidate = content.Trim();
        if (candidate.StartsWith("```", StringComparison.Ordinal))
        {
            candidate = Regex.Replace(candidate, "^```(?:json)?\\s*|\\s*```$", string.Empty, RegexOptions.IgnoreCase).Trim();
        }

        var jsonStart = candidate.IndexOf('{');
        var jsonEnd = candidate.LastIndexOf('}');
        if (jsonStart >= 0 && jsonEnd > jsonStart)
        {
            candidate = candidate[jsonStart..(jsonEnd + 1)];
        }

        try
        {
            using var document = JsonDocument.Parse(candidate);
            var root = document.RootElement;
            var call = root.TryGetProperty("toolCall", out var nestedCall) ? nestedCall : root;
            var reference = call.TryGetProperty("toolId", out var toolId) && toolId.ValueKind == JsonValueKind.String
                ? toolId.GetString()
                : call.TryGetProperty("toolName", out var toolName) && toolName.ValueKind == JsonValueKind.String
                    ? toolName.GetString()
                    : call.TryGetProperty("tool", out var tool) && tool.ValueKind == JsonValueKind.String
                        ? tool.GetString()
                        : null;

            if (!string.IsNullOrWhiteSpace(reference)
                && call.TryGetProperty("arguments", out var arguments)
                && arguments.ValueKind == JsonValueKind.Object)
            {
                toolCall = new RequestedToolCall(reference, arguments.GetRawText());
                return true;
            }
        }
        catch (JsonException)
        {
            // Some providers emit Python-style function calls; handle that compatibility form below.
        }

        foreach (var tool in tools.Where(item => item.IsEnabled))
        {
            var match = Regex.Match(
                content,
                $@"(?is)\b{BuildFlexibleToolNamePattern(tool.Name)}\s*\((?<arguments>.*)\)\s*$");
            if (!match.Success)
            {
                continue;
            }

            var values = new Dictionary<string, object?>();
            foreach (Match argument in Regex.Matches(match.Groups["arguments"].Value, "(?is)(?<name>[A-Za-z_][A-Za-z0-9_]*)\\s*=\\s*(?<value>\"(?:\\\\.|[^\"])*\"|'(?:\\\\.|[^'])*'|true|false|null|-?[0-9]+(?:\\.[0-9]+)?)\\s*(?:,|$)"))
            {
                var raw = argument.Groups["value"].Value;
                values[argument.Groups["name"].Value] = ParseFunctionArgument(raw);
            }

            if (values.Count > 0)
            {
                toolCall = new RequestedToolCall(tool.Id.ToString(), JsonSerializer.Serialize(values));
                return true;
            }
        }

        toolCall = default!;
        return false;
    }

    private static object? ParseFunctionArgument(string raw)
    {
        if (raw.StartsWith('"'))
        {
            return JsonSerializer.Deserialize<string>(raw);
        }
        if (raw.StartsWith('\''))
        {
            return Regex.Unescape(raw[1..^1]);
        }
        if (bool.TryParse(raw, out var boolean))
        {
            return boolean;
        }
        if (raw.Equals("null", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }
        return double.TryParse(raw, System.Globalization.CultureInfo.InvariantCulture, out var number) ? number : raw;
    }

    private static string BuildFlexibleToolNamePattern(string name)
    {
        return string.Join("[\\s_-]*", Regex.Matches(name, "[A-Za-z0-9]+").Select(match => Regex.Escape(match.Value)));
    }

    private static string NormalizeToolName(string value)
    {
        return Regex.Replace(value, "[^A-Za-z0-9]", string.Empty);
    }

    private static bool ToolMatches(Tool tool, string reference)
    {
        return Guid.TryParse(reference, out var id)
            ? tool.Id == id
            : NormalizeToolName(tool.Name).Equals(NormalizeToolName(reference), StringComparison.OrdinalIgnoreCase);
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

    private sealed record AttachedToolContext(string ToolName, string ResultJson);

    private sealed record RequestedToolCall(string ToolReference, string ArgumentsJson);

    private sealed record WorkflowStepRuntimeOutput(
        Guid StepId,
        string StepName,
        int Order,
        WorkflowStepType StepType,
        string InputJson,
        string OutputJson,
        string? Error);
}
