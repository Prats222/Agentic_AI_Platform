namespace AgenticPlatform.Core.DTOs.AI;

public sealed class LLMModelDto
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Provider { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int? ContextLength { get; set; }

    public decimal PromptPrice { get; set; }

    public decimal CompletionPrice { get; set; }

    public bool IsFree { get; set; }
}
