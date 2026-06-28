using AgenticPlatform.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgenticPlatform.Infrastructure.Data.Configurations;

public sealed class WorkflowConfiguration : IEntityTypeConfiguration<Workflow>
{
    public void Configure(EntityTypeBuilder<Workflow> builder)
    {
        builder.ToTable("Workflows");

        builder.HasKey(workflow => workflow.Id);

        builder.Property(workflow => workflow.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(workflow => workflow.Description)
            .HasMaxLength(1000);

        builder.Property(workflow => workflow.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(workflow => workflow.Name)
            .IsUnique();

        builder.HasMany(workflow => workflow.Steps)
            .WithOne(step => step.Workflow)
            .HasForeignKey(step => step.WorkflowId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
