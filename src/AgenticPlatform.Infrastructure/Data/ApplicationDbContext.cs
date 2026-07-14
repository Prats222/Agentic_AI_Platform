using AgenticPlatform.Core.Entities;
using AgenticPlatform.Infrastructure.Data.Seed;
using AgenticPlatform.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AgenticPlatform.Infrastructure.Data;

public sealed class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Agent> Agents => Set<Agent>();
    public DbSet<Realm> Realms => Set<Realm>();
    public DbSet<Workflow> Workflows => Set<Workflow>();
    public DbSet<WorkflowStep> WorkflowSteps => Set<WorkflowStep>();
    public DbSet<Tool> Tools => Set<Tool>();
    public DbSet<Execution> Executions => Set<Execution>();
    public DbSet<ExecutionLog> ExecutionLogs => Set<ExecutionLog>();
    public DbSet<ContextDocument> ContextDocuments => Set<ContextDocument>();
    public DbSet<HumanApprovalRequest> HumanApprovalRequests => Set<HumanApprovalRequest>();
    public DbSet<ArenaChallenge> ArenaChallenges => Set<ArenaChallenge>();
    public DbSet<ArenaEntry> ArenaEntries => Set<ArenaEntry>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AISettings> AISettings => Set<AISettings>();
    public DbSet<ChatConversation> ChatConversations => Set<ChatConversation>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(user => user.DisplayName)
                .HasMaxLength(150)
                .IsRequired();
        });

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        IdentitySeed.Seed(builder);
        DemoSeed.Seed(builder);
    }
}
