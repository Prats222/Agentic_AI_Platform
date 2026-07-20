using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgenticPlatform.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddArtifactPublishing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ContextDocuments_RealmId",
                table: "ContextDocuments");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PublishedAt",
                table: "Workflows",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PublishedByDisplayName",
                table: "Workflows",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PublishedByUserId",
                table: "Workflows",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PublishedFromArtifactId",
                table: "Workflows",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PublishedAt",
                table: "Tools",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PublishedByDisplayName",
                table: "Tools",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PublishedByUserId",
                table: "Tools",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PublishedFromArtifactId",
                table: "Tools",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PublishedAt",
                table: "ContextDocuments",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PublishedByDisplayName",
                table: "ContextDocuments",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PublishedByUserId",
                table: "ContextDocuments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PublishedFromArtifactId",
                table: "ContextDocuments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PublishedAt",
                table: "Agents",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PublishedByDisplayName",
                table: "Agents",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PublishedByUserId",
                table: "Agents",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PublishedFromArtifactId",
                table: "Agents",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Agents",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-0000-0000-0000-000000000001"),
                columns: new[] { "PublishedAt", "PublishedByDisplayName", "PublishedByUserId", "PublishedFromArtifactId" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Agents",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-0000-0000-0000-000000000002"),
                columns: new[] { "PublishedAt", "PublishedByDisplayName", "PublishedByUserId", "PublishedFromArtifactId" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Tools",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-0000-0000-0000-000000000001"),
                columns: new[] { "PublishedAt", "PublishedByDisplayName", "PublishedByUserId", "PublishedFromArtifactId" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Tools",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-0000-0000-0000-000000000002"),
                columns: new[] { "PublishedAt", "PublishedByDisplayName", "PublishedByUserId", "PublishedFromArtifactId" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Tools",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-0000-0000-0000-000000000003"),
                columns: new[] { "PublishedAt", "PublishedByDisplayName", "PublishedByUserId", "PublishedFromArtifactId" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Workflows",
                keyColumn: "Id",
                keyValue: new Guid("cccccccc-0000-0000-0000-000000000001"),
                columns: new[] { "PublishedAt", "PublishedByDisplayName", "PublishedByUserId", "PublishedFromArtifactId" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Workflows",
                keyColumn: "Id",
                keyValue: new Guid("cccccccc-0000-0000-0000-000000000002"),
                columns: new[] { "PublishedAt", "PublishedByDisplayName", "PublishedByUserId", "PublishedFromArtifactId" },
                values: new object[] { null, null, null, null });

            migrationBuilder.CreateIndex(
                name: "IX_Workflows_RealmId_PublishedFromArtifactId",
                table: "Workflows",
                columns: new[] { "RealmId", "PublishedFromArtifactId" });

            migrationBuilder.CreateIndex(
                name: "IX_Tools_RealmId_PublishedFromArtifactId",
                table: "Tools",
                columns: new[] { "RealmId", "PublishedFromArtifactId" });

            migrationBuilder.CreateIndex(
                name: "IX_ContextDocuments_RealmId_PublishedFromArtifactId",
                table: "ContextDocuments",
                columns: new[] { "RealmId", "PublishedFromArtifactId" });

            migrationBuilder.CreateIndex(
                name: "IX_Agents_RealmId_PublishedFromArtifactId",
                table: "Agents",
                columns: new[] { "RealmId", "PublishedFromArtifactId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Workflows_RealmId_PublishedFromArtifactId",
                table: "Workflows");

            migrationBuilder.DropIndex(
                name: "IX_Tools_RealmId_PublishedFromArtifactId",
                table: "Tools");

            migrationBuilder.DropIndex(
                name: "IX_ContextDocuments_RealmId_PublishedFromArtifactId",
                table: "ContextDocuments");

            migrationBuilder.DropIndex(
                name: "IX_Agents_RealmId_PublishedFromArtifactId",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "PublishedAt",
                table: "Workflows");

            migrationBuilder.DropColumn(
                name: "PublishedByDisplayName",
                table: "Workflows");

            migrationBuilder.DropColumn(
                name: "PublishedByUserId",
                table: "Workflows");

            migrationBuilder.DropColumn(
                name: "PublishedFromArtifactId",
                table: "Workflows");

            migrationBuilder.DropColumn(
                name: "PublishedAt",
                table: "Tools");

            migrationBuilder.DropColumn(
                name: "PublishedByDisplayName",
                table: "Tools");

            migrationBuilder.DropColumn(
                name: "PublishedByUserId",
                table: "Tools");

            migrationBuilder.DropColumn(
                name: "PublishedFromArtifactId",
                table: "Tools");

            migrationBuilder.DropColumn(
                name: "PublishedAt",
                table: "ContextDocuments");

            migrationBuilder.DropColumn(
                name: "PublishedByDisplayName",
                table: "ContextDocuments");

            migrationBuilder.DropColumn(
                name: "PublishedByUserId",
                table: "ContextDocuments");

            migrationBuilder.DropColumn(
                name: "PublishedFromArtifactId",
                table: "ContextDocuments");

            migrationBuilder.DropColumn(
                name: "PublishedAt",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "PublishedByDisplayName",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "PublishedByUserId",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "PublishedFromArtifactId",
                table: "Agents");

            migrationBuilder.CreateIndex(
                name: "IX_ContextDocuments_RealmId",
                table: "ContextDocuments",
                column: "RealmId");
        }
    }
}
