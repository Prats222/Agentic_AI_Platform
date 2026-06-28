using AgenticPlatform.Core.Entities;
using AgenticPlatform.Core.Enums;

namespace AgenticPlatform.Core.Interfaces;

public interface IAgentRepository : IRepository<Agent>
{
    Task<Agent?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<Agent?> GetWithToolsAndWorkflowsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Agent>> GetByStatusAsync(AgentStatus status, CancellationToken cancellationToken = default);
}
