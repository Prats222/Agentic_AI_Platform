using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgenticPlatform.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddContextDocumentsApprovalsAndObservability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "DurationMs",
                table: "Executions",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedCostUsd",
                table: "Executions",
                type: "decimal(18,8)",
                precision: 18,
                scale: 8,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EstimatedInputTokens",
                table: "Executions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EstimatedOutputTokens",
                table: "Executions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Model",
                table: "Executions",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Provider",
                table: "Executions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InputSchemaJson",
                table: "Agents",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.CreateTable(
                name: "ContextDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    FileExtension = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    ExtractedText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StoragePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContextDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HumanApprovalRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowStepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Instructions = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    IsRejected = table.Column<bool>(type: "bit", nullable: false),
                    ReviewerComment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HumanApprovalRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HumanApprovalRequests_Executions_ExecutionId",
                        column: x => x.ExecutionId,
                        principalTable: "Executions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HumanApprovalRequests_WorkflowSteps_WorkflowStepId",
                        column: x => x.WorkflowStepId,
                        principalTable: "WorkflowSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AgentContextDocuments",
                columns: table => new
                {
                    AgentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContextDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentContextDocuments", x => new { x.AgentId, x.ContextDocumentId });
                    table.ForeignKey(
                        name: "FK_AgentContextDocuments_Agents_AgentId",
                        column: x => x.AgentId,
                        principalTable: "Agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AgentContextDocuments_ContextDocuments_ContextDocumentId",
                        column: x => x.ContextDocumentId,
                        principalTable: "ContextDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Agents",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-0000-0000-0000-000000000001"),
                column: "InputSchemaJson",
                value: "{}");

            migrationBuilder.UpdateData(
                table: "Agents",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-0000-0000-0000-000000000002"),
                column: "InputSchemaJson",
                value: "{}");

            migrationBuilder.CreateIndex(
                name: "IX_AgentContextDocuments_ContextDocumentId",
                table: "AgentContextDocuments",
                column: "ContextDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_HumanApprovalRequests_ExecutionId",
                table: "HumanApprovalRequests",
                column: "ExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_HumanApprovalRequests_WorkflowStepId",
                table: "HumanApprovalRequests",
                column: "WorkflowStepId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentContextDocuments");

            migrationBuilder.DropTable(
                name: "HumanApprovalRequests");

            migrationBuilder.DropTable(
                name: "ContextDocuments");

            migrationBuilder.DropColumn(
                name: "DurationMs",
                table: "Executions");

            migrationBuilder.DropColumn(
                name: "EstimatedCostUsd",
                table: "Executions");

            migrationBuilder.DropColumn(
                name: "EstimatedInputTokens",
                table: "Executions");

            migrationBuilder.DropColumn(
                name: "EstimatedOutputTokens",
                table: "Executions");

            migrationBuilder.DropColumn(
                name: "Model",
                table: "Executions");

            migrationBuilder.DropColumn(
                name: "Provider",
                table: "Executions");

            migrationBuilder.DropColumn(
                name: "InputSchemaJson",
                table: "Agents");
        }
    }
}
