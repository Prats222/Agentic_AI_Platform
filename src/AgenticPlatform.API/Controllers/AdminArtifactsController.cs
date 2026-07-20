using AgenticPlatform.API.Extensions;
using AgenticPlatform.Core.Common;
using AgenticPlatform.Core.Constants;
using AgenticPlatform.Core.DTOs.Admin;
using AgenticPlatform.Core.Enums;
using AgenticPlatform.Core.Interfaces;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgenticPlatform.API.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Authorize(Roles = ApplicationRoles.Admin)]
[Route("api/v{version:apiVersion}/admin/artifacts")]
public sealed class AdminArtifactsController : ControllerBase
{
    private readonly IArtifactPublishingService _publishingService;

    public AdminArtifactsController(IArtifactPublishingService publishingService)
    {
        _publishingService = publishingService;
    }

    [HttpPost("{artifactType}/{id:guid}/publish")]
    [ProducesResponseType(typeof(ApiResponse<ArtifactPublishResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ArtifactPublishResultDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ArtifactPublishResultDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ArtifactPublishResultDto>>> Publish(
        string artifactType,
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryParseArtifactType(artifactType, out var parsedType))
        {
            return BadRequest(ApiResponse<ArtifactPublishResultDto>.Fail(
                "Unsupported artifact type. Use agent, workflow, tool, or context-document."));
        }

        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _publishingService.PublishAsync(
            parsedType,
            id,
            userId.Value,
            User.GetDisplayName(),
            cancellationToken);
        if (result is null)
        {
            return NotFound(ApiResponse<ArtifactPublishResultDto>.Fail(
                "The artifact was not found in Admin Realm."));
        }

        var action = result.WasCreated ? "published" : "re-published";
        var dependencyMessage = result.PublishedDependencyCount > 0
            ? $" {result.PublishedDependencyCount} required artifact(s) were published with it."
            : string.Empty;
        return Ok(ApiResponse<ArtifactPublishResultDto>.Ok(
            result,
            $"{result.Name} was {action} to User Realm as Admin Verified.{dependencyMessage}"));
    }

    private static bool TryParseArtifactType(string value, out ArtifactType artifactType)
    {
        var normalized = value.Replace("-", string.Empty, StringComparison.Ordinal).Trim();
        return Enum.TryParse(normalized, true, out artifactType);
    }
}
