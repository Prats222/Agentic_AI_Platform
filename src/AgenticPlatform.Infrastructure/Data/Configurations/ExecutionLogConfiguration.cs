using AgenticPlatform.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgenticPlatform.Infrastructure.Data.Configurations;

public sealed class ExecutionLogConfiguration : IEntityTypeConfiguration<ExecutionLog>
{
    public void Configure(EntityTypeBuilder<ExecutionLog> builder)
    {
        builder.ToTable("ExecutionLogs");

        builder.HasKey(log => log.Id);

        builder.Property(log => log.Level)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(log => log.Message)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(log => log.DetailsJson);

        builder.HasIndex(log => new { log.ExecutionId, log.CreatedAt });
    }
}
