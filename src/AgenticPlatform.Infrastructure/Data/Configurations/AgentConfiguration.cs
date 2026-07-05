using AgenticPlatform.Core.Entities;
using AgenticPlatform.Core.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgenticPlatform.Infrastructure.Data.Configurations;

public sealed class AgentConfiguration : IEntityTypeConfiguration<Agent>
{
    public void Configure(EntityTypeBuilder<Agent> builder)
    {
        builder.ToTable("Agents");

        builder.HasKey(agent => agent.Id);

        builder.Property(agent => agent.RealmId)
            .HasDefaultValue(ApplicationRealms.UserRealmId)
            .IsRequired();

        builder.HasOne(agent => agent.Realm)
            .WithMany(realm => realm.Agents)
            .HasForeignKey(agent => agent.RealmId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(agent => agent.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(agent => agent.Description)
            .HasMaxLength(1000);

        builder.Property(agent => agent.ProjectName)
            .HasMaxLength(150);

        builder.Property(agent => agent.Role)
            .HasMaxLength(150);

        builder.Property(agent => agent.Goal)
            .HasMaxLength(2000);

        builder.Property(agent => agent.ExpectedOutput)
            .HasMaxLength(2000);

        builder.Property(agent => agent.Tags)
            .HasMaxLength(500);

        builder.Property(agent => agent.ModelProvider)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(agent => agent.ModelName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(agent => agent.ModelConfigJson)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(agent => agent.InputSchemaJson)
            .HasColumnType("nvarchar(max)")
            .HasDefaultValue("{}")
            .IsRequired();

        builder.Property(agent => agent.UseGlobalAISettings)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(agent => agent.AIProvider)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(agent => agent.AIModel)
            .HasMaxLength(150);

        builder.Property(agent => agent.AISystemPrompt)
            .HasMaxLength(8000);

        builder.Property(agent => agent.AIApiKey)
            .HasMaxLength(4000);

        builder.Property(agent => agent.AIBaseUrl)
            .HasMaxLength(500);

        builder.Property(agent => agent.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(agent => new { agent.RealmId, agent.Name })
            .IsUnique();

        builder.HasMany(agent => agent.Tools)
            .WithMany(tool => tool.Agents)
            .UsingEntity<Dictionary<string, object>>(
                "AgentTools",
                right => right.HasOne<Tool>().WithMany().HasForeignKey("ToolId").OnDelete(DeleteBehavior.Cascade),
                left => left.HasOne<Agent>().WithMany().HasForeignKey("AgentId").OnDelete(DeleteBehavior.Cascade),
                join =>
                {
                    join.ToTable("AgentTools");
                    join.HasKey("AgentId", "ToolId");
                });

        builder.HasMany(agent => agent.Workflows)
            .WithMany(workflow => workflow.Agents)
            .UsingEntity<Dictionary<string, object>>(
                "AgentWorkflows",
                right => right.HasOne<Workflow>().WithMany().HasForeignKey("WorkflowId").OnDelete(DeleteBehavior.Cascade),
                left => left.HasOne<Agent>().WithMany().HasForeignKey("AgentId").OnDelete(DeleteBehavior.Cascade),
                join =>
                {
                    join.ToTable("AgentWorkflows");
                    join.HasKey("AgentId", "WorkflowId");
                });

        builder.HasMany(agent => agent.ContextDocuments)
            .WithMany(document => document.Agents)
            .UsingEntity<Dictionary<string, object>>(
                "AgentContextDocuments",
                right => right.HasOne<ContextDocument>().WithMany().HasForeignKey("ContextDocumentId").OnDelete(DeleteBehavior.Cascade),
                left => left.HasOne<Agent>().WithMany().HasForeignKey("AgentId").OnDelete(DeleteBehavior.Cascade),
                join =>
                {
                    join.ToTable("AgentContextDocuments");
                    join.HasKey("AgentId", "ContextDocumentId");
                });
    }
}
