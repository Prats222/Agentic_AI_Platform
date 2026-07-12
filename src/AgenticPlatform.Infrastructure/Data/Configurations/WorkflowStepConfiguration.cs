using AgenticPlatform.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgenticPlatform.Infrastructure.Data.Configurations;

public sealed class WorkflowStepConfiguration : IEntityTypeConfiguration<WorkflowStep>
{
    public void Configure(EntityTypeBuilder<WorkflowStep> builder)
    {
        builder.ToTable("WorkflowSteps");

        builder.HasKey(step => step.Id);

        builder.Property(step => step.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(step => step.Description)
            .HasMaxLength(1000);

        builder.Property(step => step.StepType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(step => step.InputMappingJson)
            .IsRequired();

        builder.Property(step => step.ConfigurationJson)
            .IsRequired();

        builder.HasIndex(step => new { step.WorkflowId, step.Order })
            .IsUnique();

        builder.HasOne(step => step.Tool)
            .WithMany(tool => tool.WorkflowSteps)
            .HasForeignKey(step => step.ToolId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(step => step.Agent)
            .WithMany(agent => agent.WorkflowSteps)
            .HasForeignKey(step => step.AgentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
