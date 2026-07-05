using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AgenticPlatform.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRealmsAndAdminAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Workflows_Name",
                table: "Workflows");

            migrationBuilder.DropIndex(
                name: "IX_Tools_Name",
                table: "Tools");

            migrationBuilder.DropIndex(
                name: "IX_Agents_Name",
                table: "Agents");

            migrationBuilder.AddColumn<Guid>(
                name: "RealmId",
                table: "Workflows",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.AddColumn<Guid>(
                name: "RealmId",
                table: "Tools",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.AddColumn<Guid>(
                name: "RealmId",
                table: "Executions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.AddColumn<Guid>(
                name: "RealmId",
                table: "ContextDocuments",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.AddColumn<Guid>(
                name: "RealmId",
                table: "Agents",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.CreateTable(
                name: "Realms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsAdminOnly = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Realms", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "Agents",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-0000-0000-0000-000000000001"),
                column: "RealmId",
                value: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.UpdateData(
                table: "Agents",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-0000-0000-0000-000000000002"),
                column: "RealmId",
                value: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.InsertData(
                table: "Realms",
                columns: new[] { "Id", "CreatedAt", "Description", "IsAdminOnly", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), new DateTimeOffset(new DateTime(2026, 7, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Shared workspace visible to all users and admins.", false, "User Realm", null },
                    { new Guid("22222222-2222-2222-2222-222222222222"), new DateTimeOffset(new DateTime(2026, 7, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Private administrative workspace for admins only.", true, "Admin Realm", null }
                });

            migrationBuilder.UpdateData(
                table: "Tools",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-0000-0000-0000-000000000001"),
                column: "RealmId",
                value: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.UpdateData(
                table: "Tools",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-0000-0000-0000-000000000002"),
                column: "RealmId",
                value: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.UpdateData(
                table: "Tools",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-0000-0000-0000-000000000003"),
                column: "RealmId",
                value: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.UpdateData(
                table: "Workflows",
                keyColumn: "Id",
                keyValue: new Guid("cccccccc-0000-0000-0000-000000000001"),
                column: "RealmId",
                value: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.UpdateData(
                table: "Workflows",
                keyColumn: "Id",
                keyValue: new Guid("cccccccc-0000-0000-0000-000000000002"),
                column: "RealmId",
                value: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.CreateIndex(
                name: "IX_Workflows_RealmId_Name",
                table: "Workflows",
                columns: new[] { "RealmId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tools_RealmId_Name",
                table: "Tools",
                columns: new[] { "RealmId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Executions_RealmId",
                table: "Executions",
                column: "RealmId");

            migrationBuilder.CreateIndex(
                name: "IX_ContextDocuments_RealmId",
                table: "ContextDocuments",
                column: "RealmId");

            migrationBuilder.CreateIndex(
                name: "IX_Agents_RealmId_Name",
                table: "Agents",
                columns: new[] { "RealmId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Realms_Name",
                table: "Realms",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Agents_Realms_RealmId",
                table: "Agents",
                column: "RealmId",
                principalTable: "Realms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ContextDocuments_Realms_RealmId",
                table: "ContextDocuments",
                column: "RealmId",
                principalTable: "Realms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Executions_Realms_RealmId",
                table: "Executions",
                column: "RealmId",
                principalTable: "Realms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tools_Realms_RealmId",
                table: "Tools",
                column: "RealmId",
                principalTable: "Realms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Workflows_Realms_RealmId",
                table: "Workflows",
                column: "RealmId",
                principalTable: "Realms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Agents_Realms_RealmId",
                table: "Agents");

            migrationBuilder.DropForeignKey(
                name: "FK_ContextDocuments_Realms_RealmId",
                table: "ContextDocuments");

            migrationBuilder.DropForeignKey(
                name: "FK_Executions_Realms_RealmId",
                table: "Executions");

            migrationBuilder.DropForeignKey(
                name: "FK_Tools_Realms_RealmId",
                table: "Tools");

            migrationBuilder.DropForeignKey(
                name: "FK_Workflows_Realms_RealmId",
                table: "Workflows");

            migrationBuilder.DropTable(
                name: "Realms");

            migrationBuilder.DropIndex(
                name: "IX_Workflows_RealmId_Name",
                table: "Workflows");

            migrationBuilder.DropIndex(
                name: "IX_Tools_RealmId_Name",
                table: "Tools");

            migrationBuilder.DropIndex(
                name: "IX_Executions_RealmId",
                table: "Executions");

            migrationBuilder.DropIndex(
                name: "IX_ContextDocuments_RealmId",
                table: "ContextDocuments");

            migrationBuilder.DropIndex(
                name: "IX_Agents_RealmId_Name",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "RealmId",
                table: "Workflows");

            migrationBuilder.DropColumn(
                name: "RealmId",
                table: "Tools");

            migrationBuilder.DropColumn(
                name: "RealmId",
                table: "Executions");

            migrationBuilder.DropColumn(
                name: "RealmId",
                table: "ContextDocuments");

            migrationBuilder.DropColumn(
                name: "RealmId",
                table: "Agents");

            migrationBuilder.CreateIndex(
                name: "IX_Workflows_Name",
                table: "Workflows",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tools_Name",
                table: "Tools",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Agents_Name",
                table: "Agents",
                column: "Name",
                unique: true);
        }
    }
}
