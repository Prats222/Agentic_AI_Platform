using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgenticPlatform.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAISettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AIApiKey",
                table: "Agents",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AIBaseUrl",
                table: "Agents",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AIMaxTokens",
                table: "Agents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AIModel",
                table: "Agents",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AIProvider",
                table: "Agents",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AISystemPrompt",
                table: "Agents",
                type: "nvarchar(max)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "AITemperature",
                table: "Agents",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "AITopP",
                table: "Agents",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "UseGlobalAISettings",
                table: "Agents",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "AISettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Temperature = table.Column<double>(type: "float", nullable: false),
                    MaxTokens = table.Column<int>(type: "int", nullable: false),
                    TopP = table.Column<double>(type: "float", nullable: false),
                    SystemPrompt = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    ApiKey = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    BaseUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AISettings", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "AISettings",
                columns: new[] { "Id", "ApiKey", "BaseUrl", "CreatedAt", "MaxTokens", "Model", "Provider", "SystemPrompt", "Temperature", "TopP", "UpdatedAt" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), null, "http://localhost:11434", new DateTimeOffset(new DateTime(2026, 6, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 2048, "llama3.1", "Ollama", "You are a helpful AI agent.", 0.20000000000000001, 0.90000000000000002, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AISettings");

            migrationBuilder.DropColumn(
                name: "AIApiKey",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "AIBaseUrl",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "AIMaxTokens",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "AIModel",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "AIProvider",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "AISystemPrompt",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "AITemperature",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "AITopP",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "UseGlobalAISettings",
                table: "Agents");
        }
    }
}
