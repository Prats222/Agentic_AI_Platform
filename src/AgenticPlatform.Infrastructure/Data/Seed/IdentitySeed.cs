using AgenticPlatform.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AgenticPlatform.Infrastructure.Data.Seed;

public static class IdentitySeed
{
    public static readonly Guid AdminRoleId = Guid.Parse("2e04c1db-5c39-4e95-a2c2-14b5bfe1e0e1");
    public static readonly Guid DeveloperRoleId = Guid.Parse("1582149a-cb4f-4281-aac8-97f327368835");
    public static readonly Guid ViewerRoleId = Guid.Parse("bc879923-4e2a-42e5-934c-cfbbf45898e8");
    public static readonly Guid AdminUserId = Guid.Parse("2dfbba0f-350f-4e38-a303-b0a58836cc75");

    public static void Seed(ModelBuilder builder)
    {
        builder.Entity<IdentityRole<Guid>>().HasData(
            CreateRole(AdminRoleId, "Admin"),
            CreateRole(DeveloperRoleId, "Developer"),
            CreateRole(ViewerRoleId, "Viewer"));

        builder.Entity<ApplicationUser>().HasData(new ApplicationUser
        {
            Id = AdminUserId,
            UserName = "admin@agenticplatform.local",
            NormalizedUserName = "ADMIN@AGENTICPLATFORM.LOCAL",
            Email = "admin@agenticplatform.local",
            NormalizedEmail = "ADMIN@AGENTICPLATFORM.LOCAL",
            EmailConfirmed = true,
            DisplayName = "Platform Administrator",
            CreatedAt = DateTimeOffset.Parse("2026-01-01T00:00:00+00:00"),
            PasswordHash = "AQAAAAIAAYagAAAAECtnZbvfijw0FQgbaq9JbAqYfDG5CTQZ95PWx0sBWqYYuz13G6l0ScdpZ5i3eAu7/w==",
            SecurityStamp = "96BA56FA-4F9A-4E8F-A070-40D04FB043DF",
            ConcurrencyStamp = "C7623289-0452-4BA7-A1D3-8C56A2F130BC"
        });

        builder.Entity<IdentityUserRole<Guid>>().HasData(new IdentityUserRole<Guid>
        {
            UserId = AdminUserId,
            RoleId = AdminRoleId
        });
    }

    private static IdentityRole<Guid> CreateRole(Guid id, string name)
    {
        return new IdentityRole<Guid>
        {
            Id = id,
            Name = name,
            NormalizedName = name.ToUpperInvariant(),
            ConcurrencyStamp = id.ToString()
        };
    }
}
