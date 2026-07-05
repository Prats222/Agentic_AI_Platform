using AgenticPlatform.Core.Entities;
using AgenticPlatform.Core.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgenticPlatform.Infrastructure.Data.Configurations;

public sealed class WorkflowConfiguration : IEntityTypeConfiguration<Workflow>
{
    public void Configure(EntityTypeBuilder<Workflow> builder)
    {
        builder.ToTable("Workflows");

        builder.HasKey(workflow => workflow.Id);

        builder.Property(workflow => workflow.RealmId)
            .HasDefaultValue(ApplicationRealms.UserRealmId)
            .IsRequired();

        builder.HasOne(workflow => workflow.Realm)
            .WithMany(realm => realm.Workflows)
            .HasForeignKey(workflow => workflow.RealmId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(workflow => workflow.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(workflow => workflow.Description)
            .HasMaxLength(1000);

        builder.Property(workflow => workflow.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(workflow => new { workflow.RealmId, workflow.Name })
            .IsUnique();

        builder.HasMany(workflow => workflow.Steps)
            .WithOne(step => step.Workflow)
            .HasForeignKey(step => step.WorkflowId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
