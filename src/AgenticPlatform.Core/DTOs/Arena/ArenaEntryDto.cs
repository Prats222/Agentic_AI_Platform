namespace AgenticPlatform.Core.DTOs.Arena;

public sealed class ArenaEntryDto
{
    public Guid Id { get; set; }
    public Guid ChallengeId { get; set; }
    public Guid SubmittedByUserId { get; set; }
    public string SubmittedByDisplayName { get; set; } = string.Empty;
    public Guid AgentId { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public string? Output { get; set; }
    public double? Score { get; set; }
    public string? Feedback { get; set; }
    public double? DurationMs { get; set; }
    public string? Provider { get; set; }
    public string? Model { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
