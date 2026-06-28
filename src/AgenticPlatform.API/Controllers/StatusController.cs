using AgenticPlatform.Core.Common;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace AgenticPlatform.API.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/status")]
public sealed class StatusController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<object>> Get()
    {
        var status = new
        {
            Service = "Agentic AI Platform API",
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
            ServerTimeUtc = DateTimeOffset.UtcNow
        };

        return Ok(ApiResponse<object>.Ok(status));
    }
}
