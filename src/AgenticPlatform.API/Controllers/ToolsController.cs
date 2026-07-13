using AgenticPlatform.Core.Common;
using AgenticPlatform.Core.Constants;
using AgenticPlatform.Core.DTOs.Tools;
using AgenticPlatform.Core.Entities;
using AgenticPlatform.Core.Interfaces;
using AgenticPlatform.Core.Queries;
using AgenticPlatform.API.Realms;
using AgenticPlatform.API.Extensions;
using Asp.Versioning;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgenticPlatform.API.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Authorize(Roles = $"{ApplicationRoles.Admin},{ApplicationRoles.Developer},{ApplicationRoles.Viewer}")]
[Route("api/v{version:apiVersion}/tools")]
public sealed class ToolsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IToolExecutionService _toolExecutionService;
    private readonly IMapper _mapper;

    public ToolsController(
        IUnitOfWork unitOfWork,
        IToolExecutionService toolExecutionService,
        IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _toolExecutionService = toolExecutionService;
        _mapper = mapper;
    }

    [HttpGet]
    [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Any, VaryByQueryKeys = ["*"], VaryByHeader = RealmAccess.HeaderName)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ToolDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<ToolDto>>>> GetTools(
        [FromQuery] ToolQueryParameters queryParameters,
        CancellationToken cancellationToken)
    {
        var realmId = RealmAccess.ResolveRealmId(this);
        if (!RealmAccess.CanAccessRealm(this, realmId))
        {
            return Forbid();
        }

        var query = _unitOfWork.Tools.Query().AsNoTracking().InRealm(realmId);

        if (!string.IsNullOrWhiteSpace(queryParameters.Name))
        {
            query = query.Where(tool => tool.Name.Contains(queryParameters.Name));
        }

        if (!string.IsNullOrWhiteSpace(queryParameters.Category))
        {
            query = query.Where(tool => tool.Category == queryParameters.Category);
        }

        if (queryParameters.IsEnabled.HasValue)
        {
            query = query.Where(tool => tool.IsEnabled == queryParameters.IsEnabled.Value);
        }

        query = ApplySorting(query, queryParameters.SortBy, queryParameters.SortDirection);

        var totalCount = await query.CountAsync(cancellationToken);
        var tools = await query
            .Skip((queryParameters.PageNumber - 1) * queryParameters.PageSize)
            .Take(queryParameters.PageSize)
            .ProjectTo<ToolDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<PagedResult<ToolDto>>.Ok(new PagedResult<ToolDto>
        {
            Items = tools,
            PageNumber = queryParameters.PageNumber,
            PageSize = queryParameters.PageSize,
            TotalCount = totalCount
        }));
    }

    [HttpGet("{id:guid}")]
    [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Any, VaryByHeader = RealmAccess.HeaderName)]
    [ProducesResponseType(typeof(ApiResponse<ToolDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ToolDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ToolDto>>> GetTool(Guid id, CancellationToken cancellationToken)
    {
        var tool = await _unitOfWork.Tools.Query().FirstOrDefaultAsync(item => item.Id == id && item.RealmId == RealmAccess.ResolveRealmId(this), cancellationToken);
        if (tool is null)
        {
            return NotFound(ApiResponse<ToolDto>.Fail("Tool was not found."));
        }

        return Ok(ApiResponse<ToolDto>.Ok(_mapper.Map<ToolDto>(tool)));
    }

    [HttpPost("{id:guid}/execute")]
    [Authorize(Roles = $"{ApplicationRoles.Admin},{ApplicationRoles.Developer}")]
    [ProducesResponseType(typeof(ApiResponse<ToolExecutionResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ToolExecutionResultDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ToolExecutionResultDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<ToolExecutionResultDto>>> ExecuteTool(
        Guid id,
        ExecuteToolDto request,
        CancellationToken cancellationToken)
    {
        var realmId = RealmAccess.ResolveRealmId(this);
        if (!await _unitOfWork.Tools.AnyAsync(tool => tool.Id == id && tool.RealmId == realmId, cancellationToken))
        {
            return NotFound(ApiResponse<ToolExecutionResultDto>.Fail("Tool was not found."));
        }

        var result = await _toolExecutionService.ExecuteAsync(id, request.InputJson, cancellationToken);
        if (result is null)
        {
            return NotFound(ApiResponse<ToolExecutionResultDto>.Fail("Tool was not found."));
        }

        var dto = new ToolExecutionResultDto
        {
            ToolId = result.ToolId,
            ToolName = result.ToolName,
            ExecutorName = result.ExecutorName,
            Succeeded = result.Succeeded,
            ResultJson = result.ResultJson,
            ErrorMessage = result.ErrorMessage,
            StartedAt = result.StartedAt,
            CompletedAt = result.CompletedAt,
            DurationMs = result.DurationMs
        };

        if (!result.Succeeded)
        {
            return BadRequest(ApiResponse<ToolExecutionResultDto>.Fail("Tool execution failed.", [result.ErrorMessage ?? "Unknown tool execution error."]));
        }

        return Ok(ApiResponse<ToolExecutionResultDto>.Ok(dto, "Tool executed successfully."));
    }

    [HttpPost]
    [Authorize(Roles = $"{ApplicationRoles.Admin},{ApplicationRoles.Developer}")]
    [ProducesResponseType(typeof(ApiResponse<ToolDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<ToolDto>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ToolDto>>> CreateTool(
        CreateToolDto request,
        CancellationToken cancellationToken)
    {
        var realmId = RealmAccess.ResolveRealmId(this);
        if (!RealmAccess.CanAccessRealm(this, realmId))
        {
            return Forbid();
        }

        if (await _unitOfWork.Tools.AnyAsync(tool => tool.RealmId == realmId && tool.Name == request.Name, cancellationToken))
        {
            return Conflict(ApiResponse<ToolDto>.Fail("A tool with this name already exists."));
        }

        var tool = _mapper.Map<Tool>(request);
        tool.RealmId = realmId;
        tool.CreatedByUserId = User.GetUserId();
        tool.CreatedByDisplayName = User.GetDisplayName();
        await _unitOfWork.Tools.AddAsync(tool, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetTool),
            new { id = tool.Id, version = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0" },
            ApiResponse<ToolDto>.Ok(_mapper.Map<ToolDto>(tool), "Tool registered successfully."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{ApplicationRoles.Admin},{ApplicationRoles.Developer}")]
    [ProducesResponseType(typeof(ApiResponse<ToolDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ToolDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ToolDto>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ToolDto>>> UpdateTool(
        Guid id,
        UpdateToolDto request,
        CancellationToken cancellationToken)
    {
        var tool = await _unitOfWork.Tools.Query().FirstOrDefaultAsync(item => item.Id == id && item.RealmId == RealmAccess.ResolveRealmId(this), cancellationToken);
        if (tool is null)
        {
            return NotFound(ApiResponse<ToolDto>.Fail("Tool was not found."));
        }
        if (!User.CanModifyArtifact(tool.CreatedByUserId))
        {
            return Forbid();
        }

        var nameConflict = await _unitOfWork.Tools.AnyAsync(
            existingTool => existingTool.RealmId == tool.RealmId && existingTool.Id != id && existingTool.Name == request.Name,
            cancellationToken);

        if (nameConflict)
        {
            return Conflict(ApiResponse<ToolDto>.Fail("Another tool with this name already exists."));
        }

        _mapper.Map(request, tool);
        _unitOfWork.Tools.Update(tool);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<ToolDto>.Ok(_mapper.Map<ToolDto>(tool), "Tool updated successfully."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = $"{ApplicationRoles.Admin},{ApplicationRoles.Developer}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTool(Guid id, CancellationToken cancellationToken)
    {
        var tool = await _unitOfWork.Tools.Query().FirstOrDefaultAsync(item => item.Id == id && item.RealmId == RealmAccess.ResolveRealmId(this), cancellationToken);
        if (tool is null)
        {
            return NotFound(ApiResponse<object>.Fail("Tool was not found."));
        }
        if (!User.CanModifyArtifact(tool.CreatedByUserId))
        {
            return Forbid();
        }

        _unitOfWork.Tools.Remove(tool);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static IQueryable<Tool> ApplySorting(IQueryable<Tool> query, string? sortBy, string? sortDirection)
    {
        var descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        return sortBy?.Trim().ToLowerInvariant() switch
        {
            "name" => descending ? query.OrderByDescending(tool => tool.Name) : query.OrderBy(tool => tool.Name),
            "category" => descending ? query.OrderByDescending(tool => tool.Category) : query.OrderBy(tool => tool.Category),
            "createdat" => descending ? query.OrderByDescending(tool => tool.CreatedAt) : query.OrderBy(tool => tool.CreatedAt),
            _ => query.OrderByDescending(tool => tool.CreatedAt)
        };
    }
}
