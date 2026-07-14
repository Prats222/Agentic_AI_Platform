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

            CREATE TABLE IF NOT EXISTS "ChatConversations" (
                "Id" uuid NOT NULL,
                "UserId" uuid NOT NULL,
                "Title" character varying(120) NOT NULL,
                "Provider" character varying(50) NOT NULL,
                "Model" character varying(150) NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NOT NULL,
                CONSTRAINT "PK_ChatConversations" PRIMARY KEY ("Id")
            );

            CREATE TABLE IF NOT EXISTS "ChatMessages" (
                "Id" uuid NOT NULL,
                "ConversationId" uuid NOT NULL,
                "Role" character varying(20) NOT NULL,
                "Content" character varying(8000) NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                CONSTRAINT "PK_ChatMessages" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_ChatMessages_ChatConversations_ConversationId"
                    FOREIGN KEY ("ConversationId") REFERENCES "ChatConversations" ("Id") ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS "IX_ChatConversations_UserId_UpdatedAt"
                ON "ChatConversations" ("UserId", "UpdatedAt");
            CREATE INDEX IF NOT EXISTS "IX_ChatMessages_ConversationId_CreatedAt"
                ON "ChatMessages" ("ConversationId", "CreatedAt");
            """,
            cancellationToken);
    }
}
