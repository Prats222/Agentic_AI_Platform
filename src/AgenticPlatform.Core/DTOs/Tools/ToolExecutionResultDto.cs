namespace AgenticPlatform.Core.DTOs.Tools;

public sealed class ToolExecutionResultDto
{
    public Guid ToolId { get; set; }
    public string ToolName { get; set; } = string.Empty;
    public string ExecutorName { get; set; } = string.Empty;
    public bool Succeeded { get; set; }
    public string ResultJson { get; set; } = "{}";
    public string? ErrorMessage { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset CompletedAt { get; set; }
    public double DurationMs { get; set; }
}
