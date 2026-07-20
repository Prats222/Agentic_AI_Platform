using AgenticPlatform.Core.Constants;
using AgenticPlatform.Core.DTOs.Admin;
using AgenticPlatform.Core.Entities;
using AgenticPlatform.Core.Enums;
using AgenticPlatform.Core.Interfaces;
using AgenticPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgenticPlatform.Infrastructure.Services;

public sealed class ArtifactPublishingService : IArtifactPublishingService
{
    private readonly ApplicationDbContext _dbContext;

    public ArtifactPublishingService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ArtifactPublishResultDto?> PublishAsync(
        ArtifactType artifactType,
        Guid sourceArtifactId,
        Guid publishedByUserId,
        string publishedByDisplayName,
        CancellationToken cancellationToken = default)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            var session = new PublicationSession(publishedByUserId, publishedByDisplayName);

            ArtifactEntity? publishedEntity = null;
            string? publishedName = null;
            var wasCreated = false;
            switch (artifactType)
            {
                case ArtifactType.Agent:
                    var agentPublication = await PublishAgentAsync(sourceArtifactId, session, cancellationToken);
                    publishedEntity = agentPublication?.Entity;
                    publishedName = agentPublication?.Name;
                    wasCreated = agentPublication?.WasCreated ?? false;
                    break;
                case ArtifactType.Workflow:
                    var workflowPublication = await PublishWorkflowAsync(sourceArtifactId, session, cancellationToken);
                    publishedEntity = workflowPublication?.Entity;
                    publishedName = workflowPublication?.Name;
                    wasCreated = workflowPublication?.WasCreated ?? false;
                    break;
                case ArtifactType.Tool:
                    var toolPublication = await PublishToolAsync(sourceArtifactId, session, cancellationToken);
                    publishedEntity = toolPublication?.Entity;
                    publishedName = toolPublication?.Name;
                    wasCreated = toolPublication?.WasCreated ?? false;
                    break;
                case ArtifactType.ContextDocument:
                    var documentPublication = await PublishContextDocumentAsync(sourceArtifactId, session, cancellationToken);
                    publishedEntity = documentPublication?.Entity;
                    publishedName = documentPublication?.Name;
                    wasCreated = documentPublication?.WasCreated ?? false;
                    break;
            }

            if (publishedEntity is null || publishedName is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new ArtifactPublishResultDto
            {
                ArtifactType = artifactType,
                SourceArtifactId = sourceArtifactId,
                PublishedArtifactId = publishedEntity.Id,
                Name = publishedName,
                WasCreated = wasCreated,
                PublishedDependencyCount = Math.Max(0, session.PublishedArtifactCount - 1),
                PublishedAt = session.PublishedAt
            };
        });
    }

    private async Task<Publication<Agent>?> PublishAgentAsync(
        Guid sourceId,
        PublicationSession session,
        CancellationToken cancellationToken)
    {
        if (session.Agents.TryGetValue(sourceId, out var cached))
        {
            return new Publication<Agent>(cached, cached.Name, false);
        }

        var source = await _dbContext.Agents
            .Include(agent => agent.Tools)
            .Include(agent => agent.ContextDocuments)
            .SingleOrDefaultAsync(
                agent => agent.Id == sourceId && agent.RealmId == ApplicationRealms.AdminRealmId,
                cancellationToken);
        if (source is null)
        {
            return null;
        }

        var target = await _dbContext.Agents
            .Include(agent => agent.Tools)
            .Include(agent => agent.ContextDocuments)
            .SingleOrDefaultAsync(
                agent => agent.RealmId == ApplicationRealms.UserRealmId && agent.PublishedFromArtifactId == sourceId,
                cancellationToken);
        var wasCreated = target is null;
        target ??= new Agent { RealmId = ApplicationRealms.UserRealmId };
        session.Agents[sourceId] = target;
        if (wasCreated)
        {
            _dbContext.Agents.Add(target);
        }

        target.Name = await ResolveAgentNameAsync(source.Name, target.Id, session, cancellationToken);
        target.Description = source.Description;
        target.ProjectName = source.ProjectName;
        target.Role = source.Role;
        target.Goal = source.Goal;
        target.ExpectedOutput = source.ExpectedOutput;
        target.Tags = source.Tags;
        target.ModelProvider = source.ModelProvider;
        target.ModelName = source.ModelName;
        target.ModelConfigJson = source.ModelConfigJson;
        target.InputSchemaJson = source.InputSchemaJson;
        target.UseGlobalAISettings = source.UseGlobalAISettings;
        target.AIProvider = source.AIProvider;
        target.AIModel = source.AIModel;
        target.AITemperature = source.AITemperature;
        target.AIMaxTokens = source.AIMaxTokens;
        target.AITopP = source.AITopP;
        target.AISystemPrompt = source.AISystemPrompt;
        target.AIApiKey = source.AIApiKey;
        target.AIBaseUrl = source.AIBaseUrl;
        target.Status = source.Status;
        ApplyPublicationMetadata(target, sourceId, session, wasCreated);

        target.Tools.Clear();
        foreach (var sourceTool in source.Tools)
        {
            var publishedTool = await PublishToolAsync(sourceTool.Id, session, cancellationToken);
            if (publishedTool is not null)
            {
                target.Tools.Add(publishedTool.Entity);
            }
        }

        target.ContextDocuments.Clear();
        foreach (var sourceDocument in source.ContextDocuments)
        {
            var publishedDocument = await PublishContextDocumentAsync(sourceDocument.Id, session, cancellationToken);
            if (publishedDocument is not null)
            {
                target.ContextDocuments.Add(publishedDocument.Entity);
            }
        }

        return new Publication<Agent>(target, target.Name, wasCreated);
    }

    private async Task<Publication<Workflow>?> PublishWorkflowAsync(
        Guid sourceId,
        PublicationSession session,
        CancellationToken cancellationToken)
    {
        if (session.Workflows.TryGetValue(sourceId, out var cached))
        {
            return new Publication<Workflow>(cached, cached.Name, false);
        }

        var source = await _dbContext.Workflows
            .Include(workflow => workflow.Steps)
            .Include(workflow => workflow.Agents)
            .SingleOrDefaultAsync(
                workflow => workflow.Id == sourceId && workflow.RealmId == ApplicationRealms.AdminRealmId,
                cancellationToken);
        if (source is null)
        {
            return null;
        }

        var target = await _dbContext.Workflows
            .Include(workflow => workflow.Steps)
            .Include(workflow => workflow.Agents)
            .SingleOrDefaultAsync(
                workflow => workflow.RealmId == ApplicationRealms.UserRealmId && workflow.PublishedFromArtifactId == sourceId,
                cancellationToken);
        var wasCreated = target is null;
        target ??= new Workflow { RealmId = ApplicationRealms.UserRealmId };
        session.Workflows[sourceId] = target;
        if (wasCreated)
        {
            _dbContext.Workflows.Add(target);
        }

        target.Name = await ResolveWorkflowNameAsync(source.Name, target.Id, session, cancellationToken);
        target.Description = source.Description;
        target.Status = source.Status;
        ApplyPublicationMetadata(target, sourceId, session, wasCreated);

        target.Agents.Clear();
        foreach (var sourceAgent in source.Agents)
        {
            var publishedAgent = await PublishAgentAsync(sourceAgent.Id, session, cancellationToken);
            if (publishedAgent is not null)
            {
                target.Agents.Add(publishedAgent.Entity);
            }
        }

        _dbContext.WorkflowSteps.RemoveRange(target.Steps);
        target.Steps.Clear();
        foreach (var sourceStep in source.Steps.OrderBy(step => step.Order))
        {
            Guid? publishedAgentId = null;
            Guid? publishedToolId = null;

            if (sourceStep.AgentId.HasValue)
            {
                var publishedAgent = await PublishAgentAsync(sourceStep.AgentId.Value, session, cancellationToken);
                publishedAgentId = publishedAgent?.Entity.Id;
                if (publishedAgent is not null && !target.Agents.Contains(publishedAgent.Entity))
                {
                    target.Agents.Add(publishedAgent.Entity);
                }
            }

            if (sourceStep.ToolId.HasValue)
            {
                var publishedTool = await PublishToolAsync(sourceStep.ToolId.Value, session, cancellationToken);
                publishedToolId = publishedTool?.Entity.Id;
            }

            target.Steps.Add(new WorkflowStep
            {
                Name = sourceStep.Name,
                Description = sourceStep.Description,
                Order = sourceStep.Order,
                StepType = sourceStep.StepType,
                AgentId = publishedAgentId,
                ToolId = publishedToolId,
                InputMappingJson = sourceStep.InputMappingJson,
                ConfigurationJson = sourceStep.ConfigurationJson,
                ContinueOnError = sourceStep.ContinueOnError,
                CreatedByUserId = session.PublishedByUserId,
                CreatedByDisplayName = session.PublishedByDisplayName
            });
        }

        return new Publication<Workflow>(target, target.Name, wasCreated);
    }

    private async Task<Publication<Tool>?> PublishToolAsync(
        Guid sourceId,
        PublicationSession session,
        CancellationToken cancellationToken)
    {
        if (session.Tools.TryGetValue(sourceId, out var cached))
        {
            return new Publication<Tool>(cached, cached.Name, false);
        }

        var source = await _dbContext.Tools.SingleOrDefaultAsync(
            tool => tool.Id == sourceId && tool.RealmId == ApplicationRealms.AdminRealmId,
            cancellationToken);
        if (source is null)
        {
            return null;
        }

        var target = await _dbContext.Tools.SingleOrDefaultAsync(
            tool => tool.RealmId == ApplicationRealms.UserRealmId && tool.PublishedFromArtifactId == sourceId,
            cancellationToken);
        var wasCreated = target is null;
        target ??= new Tool { RealmId = ApplicationRealms.UserRealmId };
        session.Tools[sourceId] = target;
        if (wasCreated)
        {
            _dbContext.Tools.Add(target);
        }

        target.Name = await ResolveToolNameAsync(source.Name, target.Id, session, cancellationToken);
        target.Description = source.Description;
        target.Category = source.Category;
        target.InputSchemaJson = source.InputSchemaJson;
        target.EndpointUrl = source.EndpointUrl;
        target.SecretJson = source.SecretJson;
        target.IsEnabled = source.IsEnabled;
        ApplyPublicationMetadata(target, sourceId, session, wasCreated);

        return new Publication<Tool>(target, target.Name, wasCreated);
    }

    private async Task<Publication<ContextDocument>?> PublishContextDocumentAsync(
        Guid sourceId,
        PublicationSession session,
        CancellationToken cancellationToken)
    {
        if (session.ContextDocuments.TryGetValue(sourceId, out var cached))
        {
            return new Publication<ContextDocument>(cached, cached.Name, false);
        }

        var source = await _dbContext.ContextDocuments.SingleOrDefaultAsync(
            document => document.Id == sourceId && document.RealmId == ApplicationRealms.AdminRealmId,
            cancellationToken);
        if (source is null)
        {
            return null;
        }

        var target = await _dbContext.ContextDocuments.SingleOrDefaultAsync(
            document => document.RealmId == ApplicationRealms.UserRealmId && document.PublishedFromArtifactId == sourceId,
            cancellationToken);
        var wasCreated = target is null;
        target ??= new ContextDocument { RealmId = ApplicationRealms.UserRealmId };
        session.ContextDocuments[sourceId] = target;
        if (wasCreated)
        {
            _dbContext.ContextDocuments.Add(target);
        }

        target.Name = source.Name;
        target.FileName = source.FileName;
        target.ContentType = source.ContentType;
        target.FileExtension = source.FileExtension;
        target.SizeBytes = source.SizeBytes;
        target.ExtractedText = source.ExtractedText;
        target.StoragePath = source.StoragePath;
        ApplyPublicationMetadata(target, sourceId, session, wasCreated);

        return new Publication<ContextDocument>(target, target.Name, wasCreated);
    }

    private static void ApplyPublicationMetadata(
        ArtifactEntity target,
        Guid sourceId,
        PublicationSession session,
        bool wasCreated)
    {
        target.PublishedFromArtifactId = sourceId;
        target.PublishedAt = session.PublishedAt;
        target.PublishedByUserId = session.PublishedByUserId;
        target.PublishedByDisplayName = session.PublishedByDisplayName;
        target.UpdatedAt = session.PublishedAt;
        if (wasCreated)
        {
            target.CreatedByUserId = session.PublishedByUserId;
            target.CreatedByDisplayName = session.PublishedByDisplayName;
            target.CreatedAt = session.PublishedAt;
        }
    }

    private Task<string> ResolveAgentNameAsync(string name, Guid targetId, PublicationSession session, CancellationToken cancellationToken) =>
        ResolveNameAsync(_dbContext.Agents.Where(item => item.RealmId == ApplicationRealms.UserRealmId && item.Id != targetId).Select(item => item.Name), name, session.AgentNames, cancellationToken);

    private Task<string> ResolveWorkflowNameAsync(string name, Guid targetId, PublicationSession session, CancellationToken cancellationToken) =>
        ResolveNameAsync(_dbContext.Workflows.Where(item => item.RealmId == ApplicationRealms.UserRealmId && item.Id != targetId).Select(item => item.Name), name, session.WorkflowNames, cancellationToken);

    private Task<string> ResolveToolNameAsync(string name, Guid targetId, PublicationSession session, CancellationToken cancellationToken) =>
        ResolveNameAsync(_dbContext.Tools.Where(item => item.RealmId == ApplicationRealms.UserRealmId && item.Id != targetId).Select(item => item.Name), name, session.ToolNames, cancellationToken);

    private static async Task<string> ResolveNameAsync(
        IQueryable<string> existingNamesQuery,
        string requestedName,
        ISet<string> pendingNames,
        CancellationToken cancellationToken)
    {
        var existingNames = await existingNamesQuery
            .Where(name => name == requestedName || name.StartsWith(requestedName + " (Admin Verified"))
            .ToListAsync(cancellationToken);
        var unavailable = existingNames.Concat(pendingNames).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var candidate = requestedName;
        if (unavailable.Contains(candidate))
        {
            candidate = $"{requestedName} (Admin Verified)";
        }

        var suffix = 2;
        while (unavailable.Contains(candidate))
        {
            candidate = $"{requestedName} (Admin Verified {suffix++})";
        }

        pendingNames.Add(candidate);
        return candidate;
    }

    private sealed record Publication<T>(T Entity, string Name, bool WasCreated) where T : ArtifactEntity;

    private sealed class PublicationSession
    {
        public PublicationSession(Guid publishedByUserId, string publishedByDisplayName)
        {
            PublishedByUserId = publishedByUserId;
            PublishedByDisplayName = publishedByDisplayName;
        }

        public Guid PublishedByUserId { get; }
        public string PublishedByDisplayName { get; }
        public DateTimeOffset PublishedAt { get; } = DateTimeOffset.UtcNow;
        public Dictionary<Guid, Agent> Agents { get; } = [];
        public Dictionary<Guid, Workflow> Workflows { get; } = [];
        public Dictionary<Guid, Tool> Tools { get; } = [];
        public Dictionary<Guid, ContextDocument> ContextDocuments { get; } = [];
        public HashSet<string> AgentNames { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> WorkflowNames { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> ToolNames { get; } = new(StringComparer.OrdinalIgnoreCase);
        public int PublishedArtifactCount => Agents.Count + Workflows.Count + Tools.Count + ContextDocuments.Count;
    }
}
