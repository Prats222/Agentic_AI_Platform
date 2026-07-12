using AgenticPlatform.Core.Constants;
using AgenticPlatform.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgenticPlatform.Infrastructure.Data.Configurations;

public sealed class ArenaChallengeConfiguration : IEntityTypeConfiguration<ArenaChallenge>
{
    public void Configure(EntityTypeBuilder<ArenaChallenge> builder)
    {
        builder.ToTable("ArenaChallenges");
        builder.HasKey(challenge => challenge.Id);

        builder.Property(challenge => challenge.RealmId)
            .HasDefaultValue(ApplicationRealms.UserRealmId)
            .IsRequired();

        builder.Property(challenge => challenge.CreatedByDisplayName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(challenge => challenge.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(challenge => challenge.Description)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(challenge => challenge.TaskPrompt)
            .IsRequired();

        builder.Property(challenge => challenge.Rules)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(challenge => challenge.ExpectedOutput)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(challenge => challenge.JudgeCriteria)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(challenge => challenge.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(challenge => challenge.ScorecardJson);

        builder.Property(challenge => challenge.JudgeSummary)
            .HasMaxLength(4000);

        builder.HasOne(challenge => challenge.Realm)
            .WithMany(realm => realm.ArenaChallenges)
            .HasForeignKey(challenge => challenge.RealmId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(challenge => challenge.Entries)
            .WithOne(entry => entry.Challenge)
            .HasForeignKey(entry => entry.ChallengeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(challenge => challenge.RealmId);
        builder.HasIndex(challenge => challenge.Status);
        builder.HasIndex(challenge => challenge.CreatedAt);
    }
}
