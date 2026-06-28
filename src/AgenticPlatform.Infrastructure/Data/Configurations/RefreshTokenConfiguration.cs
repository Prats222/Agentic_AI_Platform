using AgenticPlatform.Core.Entities;
using AgenticPlatform.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgenticPlatform.Infrastructure.Data.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(refreshToken => refreshToken.Id);

        builder.Property(refreshToken => refreshToken.Token)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(refreshToken => refreshToken.ReplacedByToken)
            .HasMaxLength(512);

        builder.HasIndex(refreshToken => refreshToken.Token)
            .IsUnique();

        builder.HasIndex(refreshToken => new { refreshToken.UserId, refreshToken.ExpiresAt });

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(refreshToken => refreshToken.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(refreshToken => refreshToken.IsRevoked);
        builder.Ignore(refreshToken => refreshToken.IsExpired);
        builder.Ignore(refreshToken => refreshToken.IsActive);
    }
}
