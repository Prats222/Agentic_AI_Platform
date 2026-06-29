using System.Security.Claims;
using AgenticPlatform.Core.Common;
using AgenticPlatform.Core.Constants;
using AgenticPlatform.Core.DTOs.Executions;
using AgenticPlatform.Core.Entities;
using AgenticPlatform.Core.Enums;
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
[Route("api/v{version:apiVersion}/executions")]
public sealed class ExecutionsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IExecutionQueue _executionQueue;
    private readonly IMapper _mapper;

    public ExecutionsController(IUnitOfWork unitOfWork, IExecutionQueue executionQueue, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _executionQueue = executionQueue;
        _mapper = mapper;
    }

    [HttpPost]
    [Authorize(Roles = $"{ApplicationRoles.Admin},{ApplicationRoles.Developer}")]
    [ProducesResponseType(typeof(ApiResponse<ExecutionDto>), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ApiResponse<ExecutionDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ExecutionDto>>> StartExecution(
        CreateExecutionDto request,
        CancellationToken cancellationToken)
    {
        var targetExists = request.TargetType == ExecutionTargetType.Agent
            ? await _unitOfWork.Agents.AnyAsync(agent => agent.Id == request.TargetId, cancellationToken)
            : await _unitOfWork.Workflows.AnyAsync(workflow => workflow.Id == request.TargetId, cancellationToken);

        if (!targetExists)
        {
            return NotFound(ApiResponse<ExecutionDto>.Fail("Execution target was not found."));
        }

        var execution = new Execution
        {
            TargetType = request.TargetType,
            Status = ExecutionStatus.Pending,
            AgentId = request.TargetType == ExecutionTargetType.Agent ? request.TargetId : null,
            WorkflowId = request.TargetType == ExecutionTargetType.Workflow ? request.TargetId : null,
            TriggeredByUserId = GetCurrentUserId(),
            InputJson = request.InputJson
        };

        await _unitOfWork.Executions.AddAsync(execution, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _executionQueue.QueueAsync(execution.Id, cancellationToken);

        return AcceptedAtAction(
            nameof(GetExecution),
            new { id = execution.Id, version = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0" },
            ApiResponse<ExecutionDto>.Ok(_mapper.Map<ExecutionDto>(execution), "Execution queued successfully."));
    }

    [HttpPost("{id:guid}/retry")]
    [Authorize(Roles = $"{ApplicationRoles.Admin},{ApplicationRoles.Developer}")]
    [ProducesResponseType(typeof(ApiResponse<ExecutionDto>), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ApiResponse<ExecutionDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ExecutionDto>>> RetryExecution(Guid id, CancellationToken cancellationToken)
    {
        var previousExecution = await _unitOfWork.Executions.GetByIdAsync(id, cancellationToken);
        if (previousExecution is null)
        {
            return NotFound(ApiResponse<ExecutionDto>.Fail("Execution was not found."));
        }

        var retry = new Execution
        {
            TargetType = previousExecution.TargetType,
            Status = ExecutionStatus.Pending,
            AgentId = previousExecution.AgentId,
            WorkflowId = previousExecution.WorkflowId,
            TriggeredByUserId = GetCurrentUserId(),
            InputJson = previousExecution.InputJson
        };

        await _unitOfWork.Executions.AddAsync(retry, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _executionQueue.QueueAsync(retry.Id, cancellationToken);

        return AcceptedAtAction(
            nameof(GetExecution),
            new { id = retry.Id, version = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0" },
            ApiResponse<ExecutionDto>.Ok(_mapper.Map<ExecutionDto>(retry), "Execution retry queued successfully."));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ExecutionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ExecutionDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ExecutionDto>>> GetExecution(Guid id, CancellationToken cancellationToken)
    {
        var execution = await _unitOfWork.Executions.GetWithLogsAsync(id, cancellationToken);
        if (execution is null)
        {
            return NotFound(ApiResponse<ExecutionDto>.Fail("Execution was not found."));
        }

        return Ok(ApiResponse<ExecutionDto>.Ok(_mapper.Map<ExecutionDto>(execution)));
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ExecutionDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<ExecutionDto>>>> GetExecutions(
        [FromQuery] ExecutionQueryParameters queryParameters,
        CancellationToken cancellationToken)
    {
        var query = _unitOfWork.Executions.Query()
            .AsNoTracking()
            .OrderByDescending(execution => execution.CreatedAt)
            .AsQueryable();

        if (queryParameters.Status.HasValue)
        {
            query = query.Where(execution => execution.Status == queryParameters.Status.Value);
        }

        if (queryParameters.TargetType.HasValue)
        {
            query = query.Where(execution => execution.TargetType == queryParameters.TargetType.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var executions = await query
            .Skip((queryParameters.PageNumber - 1) * queryParameters.PageSize)
            .Take(queryParameters.PageSize)
            .ProjectTo<ExecutionDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<PagedResult<ExecutionDto>>.Ok(new PagedResult<ExecutionDto>
        {
            Items = executions,
            PageNumber = queryParameters.PageNumber,
            PageSize = queryParameters.PageSize,
            TotalCount = totalCount
        }));
    }

    private Guid? GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userId, out var parsedUserId) ? parsedUserId : null;
    }
}
