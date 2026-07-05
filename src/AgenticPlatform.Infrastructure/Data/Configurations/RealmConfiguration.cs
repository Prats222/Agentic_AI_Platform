using AgenticPlatform.Core.Constants;
using AgenticPlatform.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgenticPlatform.Infrastructure.Data.Configurations;

public sealed class RealmConfiguration : IEntityTypeConfiguration<Realm>
{
    public void Configure(EntityTypeBuilder<Realm> builder)
    {
        builder.ToTable("Realms");

        builder.HasKey(realm => realm.Id);

        builder.Property(realm => realm.Name)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(realm => realm.Description)
            .HasMaxLength(500)
            .IsRequired();

        builder.HasIndex(realm => realm.Name)
            .IsUnique();

        builder.HasData(
            new Realm
            {
                Id = ApplicationRealms.UserRealmId,
                Name = ApplicationRealms.UserRealmName,
                Description = "Shared workspace visible to all users and admins.",
                IsAdminOnly = false,
                CreatedAt = new DateTimeOffset(2026, 7, 5, 0, 0, 0, TimeSpan.Zero)
            },
            new Realm
            {
                Id = ApplicationRealms.AdminRealmId,
                Name = ApplicationRealms.AdminRealmName,
                Description = "Private administrative workspace for admins only.",
                IsAdminOnly = true,
                CreatedAt = new DateTimeOffset(2026, 7, 5, 0, 0, 0, TimeSpan.Zero)
            });
    }
}
