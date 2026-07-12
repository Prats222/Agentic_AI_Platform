using AgenticPlatform.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgenticPlatform.Infrastructure.Data.Configurations;

public sealed class ArenaEntryConfiguration : IEntityTypeConfiguration<ArenaEntry>
{
    public void Configure(EntityTypeBuilder<ArenaEntry> builder)
    {
        builder.ToTable("ArenaEntries");
        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.SubmittedByDisplayName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(entry => entry.AgentName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(entry => entry.Output);

        builder.Property(entry => entry.Feedback)
            .HasMaxLength(4000);

        builder.Property(entry => entry.Provider)
            .HasMaxLength(50);

        builder.Property(entry => entry.Model)
            .HasMaxLength(150);

        builder.HasOne(entry => entry.Agent)
            .WithMany()
            .HasForeignKey(entry => entry.AgentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(entry => new { entry.ChallengeId, entry.AgentId })
            .IsUnique();
    }
}
