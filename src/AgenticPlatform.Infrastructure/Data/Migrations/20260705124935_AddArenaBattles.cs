using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgenticPlatform.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddArenaBattles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ArenaChallenges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RealmId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValue: new Guid("11111111-1111-1111-1111-111111111111")),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByDisplayName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    TaskPrompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rules = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ExpectedOutput = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    JudgeCriteria = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    WinnerEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    JudgeSummary = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ScorecardJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArenaChallenges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArenaChallenges_Realms_RealmId",
                        column: x => x.RealmId,
                        principalTable: "Realms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ArenaEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChallengeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmittedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmittedByDisplayName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    AgentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgentName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Output = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Score = table.Column<double>(type: "float", nullable: true),
                    Feedback = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    DurationMs = table.Column<double>(type: "float", nullable: true),
                    Provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Model = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArenaEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArenaEntries_Agents_AgentId",
                        column: x => x.AgentId,
                        principalTable: "Agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ArenaEntries_ArenaChallenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "ArenaChallenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArenaChallenges_CreatedAt",
                table: "ArenaChallenges",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ArenaChallenges_RealmId",
                table: "ArenaChallenges",
                column: "RealmId");

            migrationBuilder.CreateIndex(
                name: "IX_ArenaChallenges_Status",
                table: "ArenaChallenges",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ArenaEntries_AgentId",
                table: "ArenaEntries",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_ArenaEntries_ChallengeId_AgentId",
                table: "ArenaEntries",
                columns: new[] { "ChallengeId", "AgentId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArenaEntries");

            migrationBuilder.DropTable(
                name: "ArenaChallenges");
        }
    }
}
