using AgenticPlatform.Core.Enums;

namespace AgenticPlatform.Core.Entities;

public sealed class ArenaChallenge : BaseEntity
{
    public Guid RealmId { get; set; }
    public Realm? Realm { get; set; }

    public Guid CreatedByUserId { get; set; }
    public string CreatedByDisplayName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TaskPrompt { get; set; } = string.Empty;
    public string Rules { get; set; } = string.Empty;
    public string ExpectedOutput { get; set; } = string.Empty;
    public string JudgeCriteria { get; set; } = string.Empty;
    public ArenaChallengeStatus Status { get; set; } = ArenaChallengeStatus.Open;
    public Guid? WinnerEntryId { get; set; }
    public string? JudgeSummary { get; set; }
    public string? ScorecardJson { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    public ICollection<ArenaEntry> Entries { get; set; } = new List<ArenaEntry>();
}
