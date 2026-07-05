using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgenticPlatform.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderSpecificAIKeysAndProviders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeepSeekApiKey",
                table: "AISettings",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GeminiApiKey",
                table: "AISettings",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GroqApiKey",
                table: "AISettings",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OpenRouterApiKey",
                table: "AISettings",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.Sql("""
UPDATE [AISettings]
SET [GeminiApiKey] = CASE WHEN [Provider] = N'Gemini' THEN [ApiKey] ELSE [GeminiApiKey] END,
    [OpenRouterApiKey] = CASE WHEN [Provider] = N'OpenRouter' THEN [ApiKey] ELSE [OpenRouterApiKey] END,
    [GroqApiKey] = CASE WHEN [Provider] = N'Groq' THEN [ApiKey] ELSE [GroqApiKey] END,
    [DeepSeekApiKey] = CASE WHEN [Provider] = N'DeepSeek' THEN [ApiKey] ELSE [DeepSeekApiKey] END
WHERE [ApiKey] IS NOT NULL AND LTRIM(RTRIM([ApiKey])) <> N''
""");

            migrationBuilder.UpdateData(
                table: "AISettings",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "DeepSeekApiKey", "GeminiApiKey", "GroqApiKey", "OpenRouterApiKey" },
                values: new object[] { null, null, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeepSeekApiKey",
                table: "AISettings");

            migrationBuilder.DropColumn(
                name: "GeminiApiKey",
                table: "AISettings");

            migrationBuilder.DropColumn(
                name: "GroqApiKey",
                table: "AISettings");

            migrationBuilder.DropColumn(
                name: "OpenRouterApiKey",
                table: "AISettings");
        }
    }
}
