using AgenticPlatform.Core.Enums;

namespace AgenticPlatform.Core.DTOs.Arena;

public sealed class ArenaChallengeDto
{
    public Guid Id { get; set; }
    public Guid RealmId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string CreatedByDisplayName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TaskPrompt { get; set; } = string.Empty;
    public string Rules { get; set; } = string.Empty;
    public string ExpectedOutput { get; set; } = string.Empty;
    public string JudgeCriteria { get; set; } = string.Empty;
    public ArenaChallengeStatus Status { get; set; }
    public Guid? WinnerEntryId { get; set; }
    public string? JudgeSummary { get; set; }
    public string? ScorecardJson { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public IReadOnlyCollection<ArenaEntryDto> Entries { get; set; } = [];
}
