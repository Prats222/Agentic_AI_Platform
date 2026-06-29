using AgenticPlatform.Core.Common;
using AgenticPlatform.Core.Constants;
using AgenticPlatform.Core.DTOs.Demo;
using AgenticPlatform.Infrastructure.Data.Seed;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgenticPlatform.API.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Authorize(Roles = $"{ApplicationRoles.Admin},{ApplicationRoles.Developer},{ApplicationRoles.Viewer}")]
[Route("api/v{version:apiVersion}/demo")]
public sealed class DemoController : ControllerBase
{
    [HttpGet("catalog")]
    [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(typeof(ApiResponse<DemoCatalogDto>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<DemoCatalogDto>> GetCatalog()
    {
        return Ok(ApiResponse<DemoCatalogDto>.Ok(new DemoCatalogDto
        {
            Tools =
            [
                new DemoToolDto
                {
                    Id = DemoSeed.CalculatorToolId,
                    Name = "Demo Calculator",
                    Category = "Calculator",
                    SampleInputJson = "{\"expression\":\"(8 + 2) * 3\"}"
                },
                new DemoToolDto
                {
                    Id = DemoSeed.WebSearchToolId,
                    Name = "Demo Web Search",
                    Category = "WebSearch",
                    SampleInputJson = "{\"query\":\"Gemini API\"}"
                },
                new DemoToolDto
                {
                    Id = DemoSeed.FileReaderToolId,
                    Name = "Demo File Reader",
                    Category = "FileReader",
                    SampleInputJson = "{\"path\":\"appsettings.json\"}"
                }
            ],
            Agents =
            [
                new DemoAgentDto
                {
                    Id = DemoSeed.ResearchAgentId,
                    Name = "Demo Research Agent",
                    SampleInputJson = "{\"prompt\":\"Explain agentic AI in two simple sentences.\"}"
                },
                new DemoAgentDto
                {
                    Id = DemoSeed.SummaryAgentId,
                    Name = "Demo Summary Agent",
                    SampleInputJson = "{\"prompt\":\"Summarize this: Agentic systems can plan, use tools, and complete multi-step tasks.\"}"
                }
            ],
            Workflows =
            [
                new DemoWorkflowDto
                {
                    Id = DemoSeed.CalculatorWorkflowId,
                    Name = "Demo Calculator Chain",
                    SampleInputJson = "{\"expression\":\"(8 + 2) * 3\"}"
                },
                new DemoWorkflowDto
                {
                    Id = DemoSeed.ResearchWorkflowId,
                    Name = "Demo Research And Summary",
                    SampleInputJson = "{\"prompt\":\"Explain how AI agents use tools in software platforms.\"}"
                }
            ]
        }));
    }
}
