namespace AgenticPlatform.Core.DTOs.Arena;

public sealed class CreateArenaChallengeDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TaskPrompt { get; set; } = string.Empty;
    public string Rules { get; set; } = string.Empty;
    public string ExpectedOutput { get; set; } = string.Empty;
    public string JudgeCriteria { get; set; } = string.Empty;
}
