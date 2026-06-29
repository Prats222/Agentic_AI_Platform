using AgenticPlatform.Core.Entities;

namespace AgenticPlatform.Core.Models.Tools;

public sealed class ToolExecutionRequest
{
    public Tool Tool { get; set; } = default!;
    public string InputJson { get; set; } = "{}";
}
