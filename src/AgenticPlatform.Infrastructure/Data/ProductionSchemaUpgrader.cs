using Microsoft.EntityFrameworkCore;

namespace AgenticPlatform.Infrastructure.Data;

public static class ProductionSchemaUpgrader
{
    public static async Task UpgradeAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (!dbContext.Database.IsNpgsql())
        {
            return;
        }

        var tables = new[]
        {
            "Agents",
            "AISettings",
            "ArenaChallenges",
            "ArenaEntries",
            "ContextDocuments",
            "Executions",
            "ExecutionLogs",
            "HumanApprovalRequests",
            "Realms",
            "RefreshTokens",
            "Tools",
            "Workflows",
            "WorkflowSteps"
        };
        foreach (var table in tables)
        {
#pragma warning disable EF1002
            await dbContext.Database.ExecuteSqlRawAsync(
                $"""
                ALTER TABLE "{table}" ADD COLUMN IF NOT EXISTS "CreatedByUserId" uuid NULL;
                ALTER TABLE "{table}" ADD COLUMN IF NOT EXISTS "CreatedByDisplayName" character varying(150) NULL;
                """,
                cancellationToken);
#pragma warning restore EF1002
        }

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            ALTER TABLE "Tools" ADD COLUMN IF NOT EXISTS "SecretJson" text NOT NULL DEFAULT '{{}}';
            """,
            cancellationToken);
    }
}
