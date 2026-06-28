using AgenticPlatform.Core.Entities;
using AgenticPlatform.Core.Enums;

namespace AgenticPlatform.Core.Interfaces;

public interface IWorkflowRepository : IRepository<Workflow>
{
    Task<Workflow?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<Workflow?> GetWithStepsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Workflow>> GetByStatusAsync(WorkflowStatus status, CancellationToken cancellationToken = default);
}
