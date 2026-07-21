using AgenticPlatform.Core.Enums;

namespace AgenticPlatform.Core.DTOs.Workflows;

public sealed class UpdateWorkflowDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public WorkflowStatus Status { get; set; }
    public ArtifactVisibility Visibility { get; set; } = ArtifactVisibility.Realm;
}
