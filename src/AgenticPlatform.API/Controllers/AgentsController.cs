using AgenticPlatform.Core.Common;
using AgenticPlatform.Core.Constants;
using AgenticPlatform.Core.DTOs.Agents;
using AgenticPlatform.Core.Entities;
using AgenticPlatform.Core.Interfaces;
using AgenticPlatform.Core.Queries;
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
[Route("api/v{version:apiVersion}/agents")]
public sealed class AgentsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public AgentsController(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    [HttpGet]
    [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Any, VaryByQueryKeys = ["*"])]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<AgentDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<PagedResult<AgentDto>>>> GetAgents(
        [FromQuery] AgentQueryParameters queryParameters,
        CancellationToken cancellationToken)
    {
        var query = _unitOfWork.Agents.Query().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(queryParameters.Name))
        {
            query = query.Where(agent => agent.Name.Contains(queryParameters.Name));
        }

        if (queryParameters.Status.HasValue)
        {
            query = query.Where(agent => agent.Status == queryParameters.Status.Value);
        }

        query = ApplySorting(query, queryParameters.SortBy, queryParameters.SortDirection);

        var totalCount = await query.CountAsync(cancellationToken);
        var agents = await query
            .Skip((queryParameters.PageNumber - 1) * queryParameters.PageSize)
            .Take(queryParameters.PageSize)
            .ProjectTo<AgentDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        var result = new PagedResult<AgentDto>
        {
            Items = agents,
            PageNumber = queryParameters.PageNumber,
            PageSize = queryParameters.PageSize,
            TotalCount = totalCount
        };

        return Ok(ApiResponse<PagedResult<AgentDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(typeof(ApiResponse<AgentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AgentDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<AgentDto>>> GetAgent(Guid id, CancellationToken cancellationToken)
    {
        var agent = await _unitOfWork.Agents.GetByIdAsync(id, cancellationToken);
        if (agent is null)
        {
            return NotFound(ApiResponse<AgentDto>.Fail("Agent was not found."));
        }

        return Ok(ApiResponse<AgentDto>.Ok(_mapper.Map<AgentDto>(agent)));
    }

    [HttpPost]
    [Authorize(Roles = $"{ApplicationRoles.Admin},{ApplicationRoles.Developer}")]
    [ProducesResponseType(typeof(ApiResponse<AgentDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<AgentDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<AgentDto>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<AgentDto>>> CreateAgent(
        CreateAgentDto request,
        CancellationToken cancellationToken)
    {
        if (await _unitOfWork.Agents.AnyAsync(agent => agent.Name == request.Name, cancellationToken))
        {
            return Conflict(ApiResponse<AgentDto>.Fail("An agent with this name already exists."));
        }

        var agent = _mapper.Map<Agent>(request);
        await _unitOfWork.Agents.AddAsync(agent, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = _mapper.Map<AgentDto>(agent);
        return CreatedAtAction(
            nameof(GetAgent),
            new { id = agent.Id, version = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0" },
            ApiResponse<AgentDto>.Ok(dto, "Agent created successfully."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{ApplicationRoles.Admin},{ApplicationRoles.Developer}")]
    [ProducesResponseType(typeof(ApiResponse<AgentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AgentDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<AgentDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<AgentDto>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<AgentDto>>> UpdateAgent(
        Guid id,
        UpdateAgentDto request,
        CancellationToken cancellationToken)
    {
        var agent = await _unitOfWork.Agents.GetByIdAsync(id, cancellationToken);
        if (agent is null)
        {
            return NotFound(ApiResponse<AgentDto>.Fail("Agent was not found."));
        }

        var nameConflict = await _unitOfWork.Agents.AnyAsync(
            existingAgent => existingAgent.Id != id && existingAgent.Name == request.Name,
            cancellationToken);

        if (nameConflict)
        {
            return Conflict(ApiResponse<AgentDto>.Fail("Another agent with this name already exists."));
        }

        _mapper.Map(request, agent);
        _unitOfWork.Agents.Update(agent);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<AgentDto>.Ok(_mapper.Map<AgentDto>(agent), "Agent updated successfully."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = ApplicationRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteAgent(Guid id, CancellationToken cancellationToken)
    {
        var agent = await _unitOfWork.Agents.GetByIdAsync(id, cancellationToken);
        if (agent is null)
        {
            return NotFound(ApiResponse<object>.Fail("Agent was not found."));
        }

        _unitOfWork.Agents.Remove(agent);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static IQueryable<Agent> ApplySorting(
        IQueryable<Agent> query,
        string? sortBy,
        string? sortDirection)
    {
        var descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        return sortBy?.Trim().ToLowerInvariant() switch
        {
            "name" => descending ? query.OrderByDescending(agent => agent.Name) : query.OrderBy(agent => agent.Name),
            "createdat" => descending
                ? query.OrderByDescending(agent => agent.CreatedAt)
                : query.OrderBy(agent => agent.CreatedAt),
            _ => query.OrderByDescending(agent => agent.CreatedAt)
        };
    }
}
