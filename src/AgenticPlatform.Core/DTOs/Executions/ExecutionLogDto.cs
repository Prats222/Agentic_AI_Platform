using AgenticPlatform.Core.Enums;

namespace AgenticPlatform.Core.DTOs.Executions;

public sealed class ExecutionLogDto
{
    public Guid Id { get; set; }
    public ExecutionLogLevel Level { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? DetailsJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
