using AgenticPlatform.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgenticPlatform.Infrastructure.Data.Configurations;

public sealed class HumanApprovalRequestConfiguration : IEntityTypeConfiguration<HumanApprovalRequest>
{
    public void Configure(EntityTypeBuilder<HumanApprovalRequest> builder)
    {
        builder.ToTable("HumanApprovalRequests");

        builder.HasKey(request => request.Id);

        builder.Property(request => request.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(request => request.Instructions)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(request => request.PayloadJson)
            .IsRequired();

        builder.Property(request => request.ReviewerComment)
            .HasMaxLength(2000);

        builder.HasOne(request => request.Execution)
            .WithMany(execution => execution.HumanApprovalRequests)
            .HasForeignKey(request => request.ExecutionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(request => request.WorkflowStep)
            .WithMany()
            .HasForeignKey(request => request.WorkflowStepId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
