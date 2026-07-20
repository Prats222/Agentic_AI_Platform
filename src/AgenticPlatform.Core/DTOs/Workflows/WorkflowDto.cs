using AgenticPlatform.Core.DTOs.WorkflowSteps;
using AgenticPlatform.Core.Enums;

namespace AgenticPlatform.Core.DTOs.Workflows;

public sealed class WorkflowDto
{
    public Guid Id { get; set; }
    public Guid RealmId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public WorkflowStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public string? CreatedByDisplayName { get; set; }
    public Guid? PublishedFromArtifactId { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public Guid? PublishedByUserId { get; set; }
    public string? PublishedByDisplayName { get; set; }
    public IReadOnlyCollection<WorkflowStepDto> Steps { get; set; } = Array.Empty<WorkflowStepDto>();
}
