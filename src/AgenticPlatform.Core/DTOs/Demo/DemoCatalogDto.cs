namespace AgenticPlatform.Core.DTOs.Demo;

public sealed class DemoCatalogDto
{
    public IReadOnlyCollection<DemoToolDto> Tools { get; set; } = Array.Empty<DemoToolDto>();
    public IReadOnlyCollection<DemoAgentDto> Agents { get; set; } = Array.Empty<DemoAgentDto>();
    public IReadOnlyCollection<DemoWorkflowDto> Workflows { get; set; } = Array.Empty<DemoWorkflowDto>();
}
