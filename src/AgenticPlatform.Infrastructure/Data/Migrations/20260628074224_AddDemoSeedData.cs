using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AgenticPlatform.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDemoSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Agents",
                columns: new[] { "Id", "AIApiKey", "AIBaseUrl", "AIMaxTokens", "AIModel", "AIProvider", "AISystemPrompt", "AITemperature", "AITopP", "CreatedAt", "Description", "ModelConfigJson", "ModelName", "ModelProvider", "Name", "Status", "UpdatedAt", "UseGlobalAISettings" },
                values: new object[,]
                {
                    { new Guid("bbbbbbbb-0000-0000-0000-000000000001"), null, null, null, null, null, null, null, null, new DateTimeOffset(new DateTime(2026, 6, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Uses global AI settings to answer research-style prompts.", "{}", "Global default", "Global", "Demo Research Agent", "Active", null, true },
                    { new Guid("bbbbbbbb-0000-0000-0000-000000000002"), null, null, null, null, null, "You are a concise summarization agent. Return clear, practical summaries.", null, null, new DateTimeOffset(new DateTime(2026, 6, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Uses global AI settings to turn prior workflow output into a concise summary.", "{}", "Global default", "Global", "Demo Summary Agent", "Active", null, true }
                });

            migrationBuilder.InsertData(
                table: "Tools",
                columns: new[] { "Id", "Category", "CreatedAt", "Description", "EndpointUrl", "InputSchemaJson", "IsEnabled", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("aaaaaaaa-0000-0000-0000-000000000001"), "Calculator", new DateTimeOffset(new DateTime(2026, 6, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Built-in arithmetic calculator. Input: { \"expression\": \"(2 + 3) * 4\" }.", "builtin://calculator", "{\"type\":\"object\",\"properties\":{\"expression\":{\"type\":\"string\"}},\"required\":[\"expression\"]}", true, "Demo Calculator", null },
                    { new Guid("aaaaaaaa-0000-0000-0000-000000000002"), "WebSearch", new DateTimeOffset(new DateTime(2026, 6, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Built-in DuckDuckGo instant-answer search. Input: { \"query\": \"Gemini API\" }.", "builtin://web-search", "{\"type\":\"object\",\"properties\":{\"query\":{\"type\":\"string\"}},\"required\":[\"query\"]}", true, "Demo Web Search", null },
                    { new Guid("aaaaaaaa-0000-0000-0000-000000000003"), "FileReader", new DateTimeOffset(new DateTime(2026, 6, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Reads small files under the API content root. Input: { \"path\": \"appsettings.json\" }.", "builtin://file-reader", "{\"type\":\"object\",\"properties\":{\"path\":{\"type\":\"string\"}},\"required\":[\"path\"]}", true, "Demo File Reader", null }
                });

            migrationBuilder.InsertData(
                table: "Workflows",
                columns: new[] { "Id", "CreatedAt", "Description", "Name", "Status", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("cccccccc-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2026, 6, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Two-step workflow. Step 1 calculates the input expression; step 2 maps the result into a second calculation.", "Demo Calculator Chain", "Active", null },
                    { new Guid("cccccccc-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2026, 6, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Agent-to-agent workflow. Research agent answers the prompt; summary agent summarizes the prior output.", "Demo Research And Summary", "Active", null }
                });

            migrationBuilder.InsertData(
                table: "WorkflowSteps",
                columns: new[] { "Id", "AgentId", "ConfigurationJson", "ContinueOnError", "CreatedAt", "Description", "InputMappingJson", "Name", "Order", "StepType", "ToolId", "UpdatedAt", "WorkflowId" },
                values: new object[,]
                {
                    { new Guid("dddddddd-0000-0000-0000-000000000001"), null, "{}", false, new DateTimeOffset(new DateTime(2026, 6, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Runs the calculator against the workflow input.", "{}", "Calculate original expression", 1, "Tool", new Guid("aaaaaaaa-0000-0000-0000-000000000001"), null, new Guid("cccccccc-0000-0000-0000-000000000001") },
                    { new Guid("dddddddd-0000-0000-0000-000000000002"), null, "{}", false, new DateTimeOffset(new DateTime(2026, 6, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Uses the prior result to calculate result + 12.", "{\"template\":\"{{previous.result}} + 12\",\"wrapAs\":\"expression\"}", "Add twelve", 2, "Tool", new Guid("aaaaaaaa-0000-0000-0000-000000000001"), null, new Guid("cccccccc-0000-0000-0000-000000000001") },
                    { new Guid("dddddddd-0000-0000-0000-000000000003"), new Guid("bbbbbbbb-0000-0000-0000-000000000001"), "{}", false, new DateTimeOffset(new DateTime(2026, 6, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Passes the original prompt to the research agent.", "{\"source\":\"original\"}", "Research answer", 1, "Agent", null, null, new Guid("cccccccc-0000-0000-0000-000000000002") },
                    { new Guid("dddddddd-0000-0000-0000-000000000004"), new Guid("bbbbbbbb-0000-0000-0000-000000000002"), "{}", false, new DateTimeOffset(new DateTime(2026, 6, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Summarizes the prior agent output.", "{\"template\":\"Summarize this clearly for a product demo: {{previous.output}}\",\"wrapAs\":\"prompt\"}", "Summarize answer", 2, "Agent", null, null, new Guid("cccccccc-0000-0000-0000-000000000002") }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Tools",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "Tools",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "WorkflowSteps",
                keyColumn: "Id",
                keyValue: new Guid("dddddddd-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "WorkflowSteps",
                keyColumn: "Id",
                keyValue: new Guid("dddddddd-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "WorkflowSteps",
                keyColumn: "Id",
                keyValue: new Guid("dddddddd-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "WorkflowSteps",
                keyColumn: "Id",
                keyValue: new Guid("dddddddd-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                table: "Agents",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "Agents",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "Tools",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "Workflows",
                keyColumn: "Id",
                keyValue: new Guid("cccccccc-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "Workflows",
                keyColumn: "Id",
                keyValue: new Guid("cccccccc-0000-0000-0000-000000000002"));
        }
    }
}
