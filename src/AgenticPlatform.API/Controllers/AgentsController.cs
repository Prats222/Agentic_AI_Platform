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
            .Include(agent => agent.Tools)
            .Skip((queryParameters.PageNumber - 1) * queryParameters.PageSize)
            .Take(queryParameters.PageSize)
            .ToListAsync(cancellationToken);

        var result = new PagedResult<AgentDto>
        {
            Items = _mapper.Map<IReadOnlyList<AgentDto>>(agents),
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
        var agent = await _unitOfWork.Agents.Query()
            .Include(item => item.Tools)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
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
        var agent = await _unitOfWork.Agents.Query()
            .Include(item => item.Tools)
            .Include(item => item.Workflows)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
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

    [HttpPut("{id:guid}/tools")]
    [Authorize(Roles = $"{ApplicationRoles.Admin},{ApplicationRoles.Developer}")]
    [ProducesResponseType(typeof(ApiResponse<AgentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AgentDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<AgentDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<AgentDto>>> SetAgentTools(
        Guid id,
        SetAgentToolsDto request,
        CancellationToken cancellationToken)
    {
        var agent = await _unitOfWork.Agents.Query()
            .Include(item => item.Tools)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (agent is null)
        {
            return NotFound(ApiResponse<AgentDto>.Fail("Agent was not found."));
        }

        var distinctToolIds = request.ToolIds.Distinct().ToArray();
        var tools = distinctToolIds.Length == 0
            ? new List<Tool>()
            : await _unitOfWork.Tools.Query()
                .Where(tool => distinctToolIds.Contains(tool.Id))
                .ToListAsync(cancellationToken);

        if (tools.Count != distinctToolIds.Length)
        {
            return BadRequest(ApiResponse<AgentDto>.Fail("One or more selected tools were not found."));
        }

        agent.Tools.Clear();
        foreach (var tool in tools)
        {
            agent.Tools.Add(tool);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<AgentDto>.Ok(_mapper.Map<AgentDto>(agent), "Agent tools updated successfully."));
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

        var workflowSteps = await _unitOfWork.Repository<WorkflowStep>()
            .Query()
            .Where(step => step.AgentId == id)
            .ToListAsync(cancellationToken);

        var executions = await _unitOfWork.Repository<Execution>()
            .Query()
            .Where(execution => execution.AgentId == id)
            .ToListAsync(cancellationToken);

        var executionIds = executions.Select(execution => execution.Id).ToArray();
        var executionLogs = executionIds.Length == 0
            ? new List<ExecutionLog>()
            : await _unitOfWork.Repository<ExecutionLog>()
                .Query()
                .Where(log => executionIds.Contains(log.ExecutionId))
                .ToListAsync(cancellationToken);

        _unitOfWork.Repository<ExecutionLog>().RemoveRange(executionLogs);
        _unitOfWork.Repository<Execution>().RemoveRange(executions);
        _unitOfWork.Repository<WorkflowStep>().RemoveRange(workflowSteps);
        agent.Tools.Clear();
        agent.Workflows.Clear();
        _unitOfWork.Agents.Remove(agent);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict(ApiResponse<object>.Fail("Agent cannot be deleted because it is still referenced by related records."));
        }

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
