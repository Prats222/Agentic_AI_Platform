namespace AgenticPlatform.Core.DTOs.AI;

public sealed class TestLLMProviderDto
{
    public Guid? AgentId { get; set; }
    public string Prompt { get; set; } = "Say hello from the configured model.";
}
