using AgenticPlatform.Core.Entities;
using AgenticPlatform.Core.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgenticPlatform.Infrastructure.Data.Configurations;

public sealed class ExecutionConfiguration : IEntityTypeConfiguration<Execution>
{
    public void Configure(EntityTypeBuilder<Execution> builder)
    {
        builder.ToTable("Executions");

        builder.HasKey(execution => execution.Id);

        builder.Property(execution => execution.RealmId)
            .HasDefaultValue(ApplicationRealms.UserRealmId)
            .IsRequired();

        builder.HasOne(execution => execution.Realm)
            .WithMany(realm => realm.Executions)
            .HasForeignKey(execution => execution.RealmId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(execution => execution.TargetType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(execution => execution.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(execution => execution.InputJson)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(execution => execution.OutputJson)
            .HasColumnType("nvarchar(max)");

        builder.Property(execution => execution.ErrorMessage)
            .HasMaxLength(4000);

        builder.Property(execution => execution.Provider)
            .HasMaxLength(50);

        builder.Property(execution => execution.Model)
            .HasMaxLength(150);

        builder.Property(execution => execution.EstimatedCostUsd)
            .HasPrecision(18, 8);

        builder.HasOne(execution => execution.Agent)
            .WithMany(agent => agent.Executions)
            .HasForeignKey(execution => execution.AgentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(execution => execution.Workflow)
            .WithMany(workflow => workflow.Executions)
            .HasForeignKey(execution => execution.WorkflowId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(execution => execution.Logs)
            .WithOne(log => log.Execution)
            .HasForeignKey(log => log.ExecutionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(execution => execution.Status);
        builder.HasIndex(execution => execution.RealmId);
        builder.HasIndex(execution => execution.TargetType);
        builder.HasIndex(execution => execution.CreatedAt);
    }
}
