using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgenticPlatform.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnsupportedLlmProviders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE [AISettings]
                SET [Provider] = N'Gemini',
                    [Model] = N'gemini-2.5-flash',
                    [BaseUrl] = N'https://generativelanguage.googleapis.com/v1beta'
                WHERE [Provider] IN (N'Ollama', N'DeepSeek')
                """);

            migrationBuilder.Sql("""
                UPDATE [Agents]
                SET [AIProvider] = N'Gemini',
                    [AIModel] = N'gemini-2.5-flash',
                    [AIBaseUrl] = N'https://generativelanguage.googleapis.com/v1beta'
                WHERE [AIProvider] IN (N'Ollama', N'DeepSeek')
                """);

            migrationBuilder.UpdateData(
                table: "AISettings",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "BaseUrl", "Model", "Provider" },
                values: new object[] { "https://generativelanguage.googleapis.com/v1beta", "gemini-2.5-flash", "Gemini" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AISettings",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "BaseUrl", "Model", "Provider" },
                values: new object[] { "http://localhost:11434", "llama3.1", "Ollama" });
        }
    }
}
