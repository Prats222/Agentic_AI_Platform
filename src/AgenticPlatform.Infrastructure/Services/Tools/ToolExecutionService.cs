using System.Text.Json;
using AgenticPlatform.Core.Interfaces;
using AgenticPlatform.Core.Models.Tools;
using AgenticPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgenticPlatform.Infrastructure.Services.Tools;

public sealed class ToolExecutionService : IToolExecutionService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IEnumerable<IToolExecutor> _toolExecutors;

    public ToolExecutionService(ApplicationDbContext dbContext, IEnumerable<IToolExecutor> toolExecutors)
    {
        _dbContext = dbContext;
        _toolExecutors = toolExecutors;
    }

    public async Task<ToolExecutionResult?> ExecuteAsync(Guid toolId, string inputJson, CancellationToken cancellationToken = default)
    {
        var tool = await _dbContext.Tools
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == toolId, cancellationToken);

        if (tool is null)
        {
            return null;
        }

        if (!tool.IsEnabled)
        {
            throw new InvalidOperationException("Tool is disabled.");
        }

        var executor = _toolExecutors.FirstOrDefault(item => item.CanExecute(tool));
        if (executor is null)
        {
            throw new NotSupportedException($"No executor is registered for tool category '{tool.Category}'.");
        }

        return await executor.ExecuteAsync(new ToolExecutionRequest
        {
            Tool = tool,
            InputJson = MergeSecrets(inputJson, tool.SecretJson)
        }, cancellationToken);
    }

    private static string MergeSecrets(string inputJson, string? secretJson)
    {
        if (string.IsNullOrWhiteSpace(secretJson) || secretJson == "{}")
        {
            return inputJson;
        }

        using var inputDocument = JsonDocument.Parse(string.IsNullOrWhiteSpace(inputJson) ? "{}" : inputJson);
        using var secretDocument = JsonDocument.Parse(secretJson);
        if (inputDocument.RootElement.ValueKind != JsonValueKind.Object || secretDocument.RootElement.ValueKind != JsonValueKind.Object)
        {
            return inputJson;
        }

        var merged = new Dictionary<string, object?>();
        foreach (var property in inputDocument.RootElement.EnumerateObject())
        {
            merged[property.Name] = JsonSerializer.Deserialize<object?>(property.Value.GetRawText());
        }

        merged["secrets"] = JsonSerializer.Deserialize<object?>(secretDocument.RootElement.GetRawText());
        return JsonSerializer.Serialize(merged);
    }
}
