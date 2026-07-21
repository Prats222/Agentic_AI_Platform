using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgenticPlatform.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddArtifactVisibilityAndLongAgentDescriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Visibility",
                table: "Workflows",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Realm");

            migrationBuilder.AddColumn<string>(
                name: "Visibility",
                table: "Tools",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Realm");

            migrationBuilder.AddColumn<string>(
                name: "Visibility",
                table: "ContextDocuments",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Realm");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Agents",
                type: "nvarchar(max)",
                maxLength: 8000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Visibility",
                table: "Agents",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Realm");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Visibility",
                table: "Workflows");

            migrationBuilder.DropColumn(
                name: "Visibility",
                table: "Tools");

            migrationBuilder.DropColumn(
                name: "Visibility",
                table: "ContextDocuments");

            migrationBuilder.DropColumn(
                name: "Visibility",
                table: "Agents");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Agents",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldMaxLength: 8000,
                oldNullable: true);
        }
    }
}
