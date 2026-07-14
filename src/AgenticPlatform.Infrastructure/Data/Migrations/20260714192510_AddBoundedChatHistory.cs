using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgenticPlatform.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBoundedChatHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedByDisplayName",
                table: "WorkflowSteps",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "WorkflowSteps",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByDisplayName",
                table: "Workflows",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "Workflows",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByDisplayName",
                table: "Tools",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "Tools",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecretJson",
                table: "Tools",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<string>(
                name: "CreatedByDisplayName",
                table: "RefreshTokens",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "RefreshTokens",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByDisplayName",
                table: "Realms",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "Realms",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByDisplayName",
                table: "HumanApprovalRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "HumanApprovalRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByDisplayName",
                table: "Executions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "Executions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByDisplayName",
                table: "ExecutionLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "ExecutionLogs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByDisplayName",
                table: "ContextDocuments",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "ContextDocuments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByDisplayName",
                table: "ArenaEntries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "ArenaEntries",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByDisplayName",
                table: "AISettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "AISettings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByDisplayName",
                table: "Agents",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "Agents",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ChatConversations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatConversations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChatMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatMessages_ChatConversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "ChatConversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "AISettings",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedByDisplayName", "CreatedByUserId" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Agents",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedByDisplayName", "CreatedByUserId" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Agents",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedByDisplayName", "CreatedByUserId" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Realms",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedByDisplayName", "CreatedByUserId" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Realms",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedByDisplayName", "CreatedByUserId" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Tools",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedByDisplayName", "CreatedByUserId", "SecretJson" },
                values: new object[] { null, null, "{}" });

            migrationBuilder.UpdateData(
                table: "Tools",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedByDisplayName", "CreatedByUserId", "SecretJson" },
                values: new object[] { null, null, "{}" });

            migrationBuilder.UpdateData(
                table: "Tools",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedByDisplayName", "CreatedByUserId", "SecretJson" },
                values: new object[] { null, null, "{}" });

            migrationBuilder.UpdateData(
                table: "WorkflowSteps",
                keyColumn: "Id",
                keyValue: new Guid("dddddddd-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedByDisplayName", "CreatedByUserId" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "WorkflowSteps",
                keyColumn: "Id",
                keyValue: new Guid("dddddddd-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedByDisplayName", "CreatedByUserId" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "WorkflowSteps",
                keyColumn: "Id",
                keyValue: new Guid("dddddddd-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedByDisplayName", "CreatedByUserId" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "WorkflowSteps",
                keyColumn: "Id",
                keyValue: new Guid("dddddddd-0000-0000-0000-000000000004"),
                columns: new[] { "CreatedByDisplayName", "CreatedByUserId" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Workflows",
                keyColumn: "Id",
                keyValue: new Guid("cccccccc-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedByDisplayName", "CreatedByUserId" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Workflows",
                keyColumn: "Id",
                keyValue: new Guid("cccccccc-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedByDisplayName", "CreatedByUserId" },
                values: new object[] { null, null });

            migrationBuilder.CreateIndex(
                name: "IX_ChatConversations_UserId_UpdatedAt",
                table: "ChatConversations",
                columns: new[] { "UserId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ConversationId_CreatedAt",
                table: "ChatMessages",
                columns: new[] { "ConversationId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatMessages");

            migrationBuilder.DropTable(
                name: "ChatConversations");

            migrationBuilder.DropColumn(
                name: "CreatedByDisplayName",
                table: "WorkflowSteps");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "WorkflowSteps");

            migrationBuilder.DropColumn(
                name: "CreatedByDisplayName",
                table: "Workflows");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Workflows");

            migrationBuilder.DropColumn(
                name: "CreatedByDisplayName",
                table: "Tools");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Tools");

            migrationBuilder.DropColumn(
                name: "SecretJson",
                table: "Tools");

            migrationBuilder.DropColumn(
                name: "CreatedByDisplayName",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "CreatedByDisplayName",
                table: "Realms");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Realms");

            migrationBuilder.DropColumn(
                name: "CreatedByDisplayName",
                table: "HumanApprovalRequests");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "HumanApprovalRequests");

            migrationBuilder.DropColumn(
                name: "CreatedByDisplayName",
                table: "Executions");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Executions");

            migrationBuilder.DropColumn(
                name: "CreatedByDisplayName",
                table: "ExecutionLogs");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "ExecutionLogs");

            migrationBuilder.DropColumn(
                name: "CreatedByDisplayName",
                table: "ContextDocuments");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "ContextDocuments");

            migrationBuilder.DropColumn(
                name: "CreatedByDisplayName",
                table: "ArenaEntries");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "ArenaEntries");

            migrationBuilder.DropColumn(
                name: "CreatedByDisplayName",
                table: "AISettings");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "AISettings");

            migrationBuilder.DropColumn(
                name: "CreatedByDisplayName",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Agents");
        }
    }
}
