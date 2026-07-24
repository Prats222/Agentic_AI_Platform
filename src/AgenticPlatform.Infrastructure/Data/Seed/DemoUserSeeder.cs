using AgenticPlatform.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AgenticPlatform.Infrastructure.Data.Seed;

public static class DemoUserSeeder
{
    private const int MaximumDemoUsers = 100;
    private const string EmailDomain = "pratspilot.local";

    private static readonly string[] FirstNames =
    [
        "Aarav", "Vivaan", "Aditya", "Arjun", "Rohan",
        "Rahul", "Kunal", "Siddharth", "Ananya", "Diya",
        "Isha", "Kavya", "Meera", "Nisha", "Pooja",
        "Priya", "Riya", "Sneha", "Tanvi", "Aditi"
    ];

    private static readonly string[] LastNames =
    [
        "Sharma", "Patel", "Singh", "Gupta", "Iyer"
    ];

    public static async Task<int> SeedAsync(
        ApplicationDbContext dbContext,
        int requestedCount,
        CancellationToken cancellationToken = default)
    {
        var count = Math.Clamp(requestedCount, 0, MaximumDemoUsers);
        if (count == 0)
        {
            return 0;
        }

        var expectedEmails = Enumerable.Range(1, count)
            .Select(GetEmail)
            .ToArray();
        var existingEmailList = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Email != null && expectedEmails.Contains(user.Email))
            .Select(user => user.Email!)
            .ToListAsync(cancellationToken);
        var existingEmails = new HashSet<string>(
            existingEmailList,
            StringComparer.OrdinalIgnoreCase);

        var now = DateTimeOffset.UtcNow;
        var users = new List<ApplicationUser>();
        var roleAssignments = new List<IdentityUserRole<Guid>>();

        for (var index = 1; index <= count; index++)
        {
            var email = GetEmail(index);
            if (existingEmails.Contains(email))
            {
                continue;
            }

            var userId = GetUserId(index);
            var user = new ApplicationUser
            {
                Id = userId,
                UserName = email,
                NormalizedUserName = email.ToUpperInvariant(),
                Email = email,
                NormalizedEmail = email.ToUpperInvariant(),
                EmailConfirmed = false,
                DisplayName = GetDisplayName(index),
                CreatedAt = GetCreatedAt(index, now),
                SecurityStamp = $"DEMO-SECURITY-{index:000}",
                ConcurrencyStamp = $"DEMO-CONCURRENCY-{index:000}",
                LockoutEnabled = true
            };

            users.Add(user);
            roleAssignments.Add(new IdentityUserRole<Guid>
            {
                UserId = userId,
                RoleId = IdentitySeed.DeveloperRoleId
            });
        }

        if (users.Count == 0)
        {
            return 0;
        }

        dbContext.Users.AddRange(users);
        dbContext.UserRoles.AddRange(roleAssignments);
        await dbContext.SaveChangesAsync(cancellationToken);
        return users.Count;
    }

    public static bool IsDemoEmail(string? email)
    {
        return email?.EndsWith($"@{EmailDomain}", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static string GetEmail(int index) => $"demo.{index:000}@{EmailDomain}";

    private static Guid GetUserId(int index) =>
        Guid.Parse($"33333333-3333-3333-3333-{index:000000000000}");

    private static string GetDisplayName(int index)
    {
        var zeroBasedIndex = index - 1;
        var firstName = FirstNames[zeroBasedIndex % FirstNames.Length];
        var lastName = LastNames[(zeroBasedIndex / FirstNames.Length) % LastNames.Length];
        return $"{firstName} {lastName}";
    }

    private static DateTimeOffset GetCreatedAt(int index, DateTimeOffset now)
    {
        if (index <= 5)
        {
            return now.AddMinutes(-index * 6);
        }

        var daysAgo = 1 + ((index - 6) % 45);
        var minutesAgo = (index * 17) % 720;
        return now.AddDays(-daysAgo).AddMinutes(-minutesAgo);
    }
}
