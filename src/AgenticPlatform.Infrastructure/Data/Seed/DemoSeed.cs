using AgenticPlatform.Core.Entities;
using AgenticPlatform.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace AgenticPlatform.Infrastructure.Data.Seed;

public static class DemoSeed
{
    public static readonly Guid CalculatorToolId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    public static readonly Guid WebSearchToolId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002");
    public static readonly Guid FileReaderToolId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000003");

    public static readonly Guid ResearchAgentId = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001");
    public static readonly Guid SummaryAgentId = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");

    public static readonly Guid CalculatorWorkflowId = Guid.Parse("cccccccc-0000-0000-0000-000000000001");
    public static readonly Guid ResearchWorkflowId = Guid.Parse("cccccccc-0000-0000-0000-000000000002");

    public static readonly Guid CalculatorWorkflowStep1Id = Guid.Parse("dddddddd-0000-0000-0000-000000000001");
    public static readonly Guid CalculatorWorkflowStep2Id = Guid.Parse("dddddddd-0000-0000-0000-000000000002");
    public static readonly Guid ResearchWorkflowStep1Id = Guid.Parse("dddddddd-0000-0000-0000-000000000003");
    public static readonly Guid ResearchWorkflowStep2Id = Guid.Parse("dddddddd-0000-0000-0000-000000000004");

    private static readonly DateTimeOffset CreatedAt = new(2026, 6, 28, 0, 0, 0, TimeSpan.Zero);

    public static void Seed(ModelBuilder builder)
    {
        builder.Entity<Tool>().HasData(
            new Tool
            {
                Id = CalculatorToolId,
                Name = "Demo Calculator",
                Description = "Built-in arithmetic calculator. Input: { \"expression\": \"(2 + 3) * 4\" }.",
                Category = "Calculator",
                InputSchemaJson = "{\"type\":\"object\",\"properties\":{\"expression\":{\"type\":\"string\"}},\"required\":[\"expression\"]}",
                EndpointUrl = "builtin://calculator",
                IsEnabled = true,
                CreatedAt = CreatedAt
            },
            new Tool
            {
                Id = WebSearchToolId,
                Name = "Demo Web Search",
                Description = "Built-in DuckDuckGo instant-answer search. Input: { \"query\": \"Gemini API\" }.",
                Category = "WebSearch",
                InputSchemaJson = "{\"type\":\"object\",\"properties\":{\"query\":{\"type\":\"string\"}},\"required\":[\"query\"]}",
                EndpointUrl = "builtin://web-search",
                IsEnabled = true,
                CreatedAt = CreatedAt
            },
            new Tool
            {
                Id = FileReaderToolId,
                Name = "Demo File Reader",
                Description = "Reads small files under the API content root. Input: { \"path\": \"appsettings.json\" }.",
                Category = "FileReader",
                InputSchemaJson = "{\"type\":\"object\",\"properties\":{\"path\":{\"type\":\"string\"}},\"required\":[\"path\"]}",
                EndpointUrl = "builtin://file-reader",
                IsEnabled = true,
                CreatedAt = CreatedAt
            });

        builder.Entity<Agent>().HasData(
            new Agent
            {
                Id = ResearchAgentId,
                Name = "Demo Research Agent",
                Description = "Uses global AI settings to answer research-style prompts.",
                ModelProvider = "Global",
                ModelName = "Global default",
                ModelConfigJson = "{}",
                UseGlobalAISettings = true,
                Status = AgentStatus.Active,
                CreatedAt = CreatedAt
            },
            new Agent
            {
                Id = SummaryAgentId,
                Name = "Demo Summary Agent",
                Description = "Uses global AI settings to turn prior workflow output into a concise summary.",
                ModelProvider = "Global",
                ModelName = "Global default",
                ModelConfigJson = "{}",
                UseGlobalAISettings = true,
                AISystemPrompt = "You are a concise summarization agent. Return clear, practical summaries.",
                Status = AgentStatus.Active,
                CreatedAt = CreatedAt
            });

        builder.Entity<Workflow>().HasData(
            new Workflow
            {
                Id = CalculatorWorkflowId,
                Name = "Demo Calculator Chain",
                Description = "Two-step workflow. Step 1 calculates the input expression; step 2 maps the result into a second calculation.",
                Status = WorkflowStatus.Active,
                CreatedAt = CreatedAt
            },
            new Workflow
            {
                Id = ResearchWorkflowId,
                Name = "Demo Research And Summary",
                Description = "Agent-to-agent workflow. Research agent answers the prompt; summary agent summarizes the prior output.",
                Status = WorkflowStatus.Active,
                CreatedAt = CreatedAt
            });

        builder.Entity<WorkflowStep>().HasData(
            new WorkflowStep
            {
                Id = CalculatorWorkflowStep1Id,
                WorkflowId = CalculatorWorkflowId,
                Name = "Calculate original expression",
                Description = "Runs the calculator against the workflow input.",
                Order = 1,
                StepType = WorkflowStepType.Tool,
                ToolId = CalculatorToolId,
                InputMappingJson = "{}",
                ConfigurationJson = "{}",
                ContinueOnError = false,
                CreatedAt = CreatedAt
            },
            new WorkflowStep
            {
                Id = CalculatorWorkflowStep2Id,
                WorkflowId = CalculatorWorkflowId,
                Name = "Add twelve",
                Description = "Uses the prior result to calculate result + 12.",
                Order = 2,
                StepType = WorkflowStepType.Tool,
                ToolId = CalculatorToolId,
                InputMappingJson = "{\"template\":\"{{previous.result}} + 12\",\"wrapAs\":\"expression\"}",
                ConfigurationJson = "{}",
                ContinueOnError = false,
                CreatedAt = CreatedAt
            },
            new WorkflowStep
            {
                Id = ResearchWorkflowStep1Id,
                WorkflowId = ResearchWorkflowId,
                Name = "Research answer",
                Description = "Passes the original prompt to the research agent.",
                Order = 1,
                StepType = WorkflowStepType.Agent,
                AgentId = ResearchAgentId,
                InputMappingJson = "{\"source\":\"original\"}",
                ConfigurationJson = "{}",
                ContinueOnError = false,
                CreatedAt = CreatedAt
            },
            new WorkflowStep
            {
                Id = ResearchWorkflowStep2Id,
                WorkflowId = ResearchWorkflowId,
                Name = "Summarize answer",
                Description = "Summarizes the prior agent output.",
                Order = 2,
                StepType = WorkflowStepType.Agent,
                AgentId = SummaryAgentId,
                InputMappingJson = "{\"template\":\"Summarize this clearly for a product demo: {{previous.output}}\",\"wrapAs\":\"prompt\"}",
                ConfigurationJson = "{}",
                ContinueOnError = false,
                CreatedAt = CreatedAt
            });
    }
}
