using AgenticPlatform.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgenticPlatform.Infrastructure.Data.Configurations;

public sealed class AgentConfiguration : IEntityTypeConfiguration<Agent>
{
    public void Configure(EntityTypeBuilder<Agent> builder)
    {
        builder.ToTable("Agents");

        builder.HasKey(agent => agent.Id);

        builder.Property(agent => agent.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(agent => agent.Description)
            .HasMaxLength(1000);

        builder.Property(agent => agent.ModelProvider)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(agent => agent.ModelName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(agent => agent.ModelConfigJson)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(agent => agent.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(agent => agent.Name)
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
    }
}
