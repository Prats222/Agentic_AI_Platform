using AgenticPlatform.Core.Enums;

namespace AgenticPlatform.Core.DTOs.Workflows;

public sealed class CreateWorkflowDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public WorkflowStatus Status { get; set; } = WorkflowStatus.Draft;
    public ArtifactVisibility Visibility { get; set; } = ArtifactVisibility.Realm;
}
