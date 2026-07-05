using AgenticPlatform.Core.Common;
using AgenticPlatform.Core.Constants;
using AgenticPlatform.Core.DTOs.Workflows;
using AgenticPlatform.Core.DTOs.WorkflowSteps;
using AgenticPlatform.Core.Entities;
using AgenticPlatform.Core.Enums;
using AgenticPlatform.Core.Interfaces;
using AgenticPlatform.Core.Queries;
using AgenticPlatform.API.Realms;
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
[Route("api/v{version:apiVersion}/workflows")]
public sealed class WorkflowsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public WorkflowsController(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    [HttpGet]
    [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Any, VaryByQueryKeys = ["*"], VaryByHeader = RealmAccess.HeaderName)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<WorkflowDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<WorkflowDto>>>> GetWorkflows(
        [FromQuery] WorkflowQueryParameters queryParameters,
        CancellationToken cancellationToken)
    {
        var realmId = RealmAccess.ResolveRealmId(this);
        if (!RealmAccess.CanAccessRealm(this, realmId))
        {
            return Forbid();
        }

        var query = _unitOfWork.Workflows.Query().AsNoTracking().InRealm(realmId);

        if (!string.IsNullOrWhiteSpace(queryParameters.Name))
        {
            query = query.Where(workflow => workflow.Name.Contains(queryParameters.Name));
        }

        if (queryParameters.Status.HasValue)
        {
            query = query.Where(workflow => workflow.Status == queryParameters.Status.Value);
        }

        query = ApplySorting(query, queryParameters.SortBy, queryParameters.SortDirection);

        var totalCount = await query.CountAsync(cancellationToken);
        var workflows = await query
            .Skip((queryParameters.PageNumber - 1) * queryParameters.PageSize)
            .Take(queryParameters.PageSize)
            .ProjectTo<WorkflowDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<PagedResult<WorkflowDto>>.Ok(new PagedResult<WorkflowDto>
        {
            Items = workflows,
            PageNumber = queryParameters.PageNumber,
            PageSize = queryParameters.PageSize,
            TotalCount = totalCount
        }));
    }

    [HttpGet("{id:guid}")]
    [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Any, VaryByHeader = RealmAccess.HeaderName)]
    [ProducesResponseType(typeof(ApiResponse<WorkflowDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<WorkflowDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<WorkflowDto>>> GetWorkflow(Guid id, CancellationToken cancellationToken)
    {
        var realmId = RealmAccess.ResolveRealmId(this);
        if (!RealmAccess.CanAccessRealm(this, realmId))
        {
            return Forbid();
        }

        var workflow = await _unitOfWork.Workflows.Query()
            .Include(item => item.Steps.OrderBy(step => step.Order))
            .FirstOrDefaultAsync(item => item.Id == id && item.RealmId == realmId, cancellationToken);
        if (workflow is null)
        {
            return NotFound(ApiResponse<WorkflowDto>.Fail("Workflow was not found."));
        }

        return Ok(ApiResponse<WorkflowDto>.Ok(_mapper.Map<WorkflowDto>(workflow)));
    }

    [HttpPost]
    [Authorize(Roles = $"{ApplicationRoles.Admin},{ApplicationRoles.Developer}")]
    [ProducesResponseType(typeof(ApiResponse<WorkflowDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<WorkflowDto>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<WorkflowDto>>> CreateWorkflow(
        CreateWorkflowDto request,
        CancellationToken cancellationToken)
    {
        var realmId = RealmAccess.ResolveRealmId(this);
        if (!RealmAccess.CanAccessRealm(this, realmId))
        {
            return Forbid();
        }

        if (await _unitOfWork.Workflows.AnyAsync(workflow => workflow.RealmId == realmId && workflow.Name == request.Name, cancellationToken))
        {
            return Conflict(ApiResponse<WorkflowDto>.Fail("A workflow with this name already exists."));
        }

        var workflow = _mapper.Map<Workflow>(request);
        workflow.RealmId = realmId;
        await _unitOfWork.Workflows.AddAsync(workflow, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetWorkflow),
            new { id = workflow.Id, version = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0" },
            ApiResponse<WorkflowDto>.Ok(_mapper.Map<WorkflowDto>(workflow), "Workflow created successfully."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{ApplicationRoles.Admin},{ApplicationRoles.Developer}")]
    [ProducesResponseType(typeof(ApiResponse<WorkflowDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<WorkflowDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<WorkflowDto>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<WorkflowDto>>> UpdateWorkflow(
        Guid id,
        UpdateWorkflowDto request,
        CancellationToken cancellationToken)
    {
        var workflow = await _unitOfWork.Workflows.Query()
            .Include(item => item.Agents)
            .FirstOrDefaultAsync(item => item.Id == id && item.RealmId == RealmAccess.ResolveRealmId(this), cancellationToken);
        if (workflow is null)
        {
            return NotFound(ApiResponse<WorkflowDto>.Fail("Workflow was not found."));
        }

        var nameConflict = await _unitOfWork.Workflows.AnyAsync(
            existingWorkflow => existingWorkflow.RealmId == workflow.RealmId && existingWorkflow.Id != id && existingWorkflow.Name == request.Name,
            cancellationToken);

        if (nameConflict)
        {
            return Conflict(ApiResponse<WorkflowDto>.Fail("Another workflow with this name already exists."));
        }

        _mapper.Map(request, workflow);
        _unitOfWork.Workflows.Update(workflow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<WorkflowDto>.Ok(_mapper.Map<WorkflowDto>(workflow), "Workflow updated successfully."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = ApplicationRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteWorkflow(Guid id, CancellationToken cancellationToken)
    {
        var workflow = await _unitOfWork.Workflows.Query()
            .FirstOrDefaultAsync(item => item.Id == id && item.RealmId == RealmAccess.ResolveRealmId(this), cancellationToken);
        if (workflow is null)
        {
            return NotFound(ApiResponse<object>.Fail("Workflow was not found."));
        }

        var steps = await _unitOfWork.Repository<WorkflowStep>()
            .Query()
            .Where(step => step.WorkflowId == id)
            .ToListAsync(cancellationToken);

        var executions = await _unitOfWork.Repository<Execution>()
            .Query()
            .Where(execution => execution.WorkflowId == id)
            .ToListAsync(cancellationToken);

        var executionIds = executions.Select(execution => execution.Id).ToArray();
        var stepIds = steps.Select(step => step.Id).ToArray();

        var approvalRequests = stepIds.Length == 0 && executionIds.Length == 0
            ? new List<HumanApprovalRequest>()
            : await _unitOfWork.Repository<HumanApprovalRequest>()
                .Query()
                .Where(approval => stepIds.Contains(approval.WorkflowStepId) || executionIds.Contains(approval.ExecutionId))
                .ToListAsync(cancellationToken);

        var executionLogs = executionIds.Length == 0
            ? new List<ExecutionLog>()
            : await _unitOfWork.Repository<ExecutionLog>()
                .Query()
                .Where(log => executionIds.Contains(log.ExecutionId))
                .ToListAsync(cancellationToken);

        _unitOfWork.Repository<HumanApprovalRequest>().RemoveRange(approvalRequests);
        _unitOfWork.Repository<ExecutionLog>().RemoveRange(executionLogs);
        _unitOfWork.Repository<Execution>().RemoveRange(executions);
        _unitOfWork.Repository<WorkflowStep>().RemoveRange(steps);
        workflow.Agents.Clear();
        _unitOfWork.Workflows.Remove(workflow);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict(ApiResponse<object>.Fail("Workflow cannot be deleted because it is still referenced by related records."));
        }

        return NoContent();
    }

    [HttpPost("{workflowId:guid}/steps")]
    [Authorize(Roles = $"{ApplicationRoles.Admin},{ApplicationRoles.Developer}")]
    [ProducesResponseType(typeof(ApiResponse<WorkflowStepDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<WorkflowStepDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<WorkflowStepDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<WorkflowStepDto>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<WorkflowStepDto>>> CreateWorkflowStep(
        Guid workflowId,
        CreateWorkflowStepDto request,
        CancellationToken cancellationToken)
    {
        var realmId = RealmAccess.ResolveRealmId(this);
        if (!RealmAccess.CanAccessRealm(this, realmId))
        {
            return Forbid();
        }

        if (!await _unitOfWork.Workflows.AnyAsync(workflow => workflow.Id == workflowId && workflow.RealmId == realmId, cancellationToken))
        {
            return NotFound(ApiResponse<WorkflowStepDto>.Fail("Workflow was not found."));
        }

        var targetError = await ValidateStepTargetAsync(request.StepType, request.ToolId, request.AgentId, cancellationToken);
        if (targetError is not null)
        {
            return BadRequest(ApiResponse<WorkflowStepDto>.Fail(targetError));
        }

        var orderConflict = await _unitOfWork.Repository<WorkflowStep>().AnyAsync(
            step => step.WorkflowId == workflowId && step.Order == request.Order,
            cancellationToken);

        if (orderConflict)
        {
            return Conflict(ApiResponse<WorkflowStepDto>.Fail("A workflow step with this order already exists."));
        }

        var step = _mapper.Map<WorkflowStep>(request);
        step.WorkflowId = workflowId;

        await _unitOfWork.Repository<WorkflowStep>().AddAsync(step, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetWorkflowStep),
            new
            {
                workflowId,
                stepId = step.Id,
                version = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0"
            },
            ApiResponse<WorkflowStepDto>.Ok(_mapper.Map<WorkflowStepDto>(step), "Workflow step created successfully."));
    }

    [HttpGet("{workflowId:guid}/steps/{stepId:guid}")]
    [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Any, VaryByHeader = RealmAccess.HeaderName)]
    [ProducesResponseType(typeof(ApiResponse<WorkflowStepDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<WorkflowStepDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<WorkflowStepDto>>> GetWorkflowStep(
        Guid workflowId,
        Guid stepId,
        CancellationToken cancellationToken)
    {
        var step = await _unitOfWork.Repository<WorkflowStep>().FirstOrDefaultAsync(
            workflowStep => workflowStep.WorkflowId == workflowId && workflowStep.Id == stepId && workflowStep.Workflow!.RealmId == RealmAccess.ResolveRealmId(this),
            cancellationToken);

        if (step is null)
        {
            return NotFound(ApiResponse<WorkflowStepDto>.Fail("Workflow step was not found."));
        }

        return Ok(ApiResponse<WorkflowStepDto>.Ok(_mapper.Map<WorkflowStepDto>(step)));
    }

    [HttpPut("{workflowId:guid}/steps/{stepId:guid}")]
    [Authorize(Roles = $"{ApplicationRoles.Admin},{ApplicationRoles.Developer}")]
    [ProducesResponseType(typeof(ApiResponse<WorkflowStepDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<WorkflowStepDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<WorkflowStepDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<WorkflowStepDto>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<WorkflowStepDto>>> UpdateWorkflowStep(
        Guid workflowId,
        Guid stepId,
        UpdateWorkflowStepDto request,
        CancellationToken cancellationToken)
    {
        var step = await _unitOfWork.Repository<WorkflowStep>().FirstOrDefaultAsync(
            workflowStep => workflowStep.WorkflowId == workflowId && workflowStep.Id == stepId && workflowStep.Workflow!.RealmId == RealmAccess.ResolveRealmId(this),
            cancellationToken);

        if (step is null)
        {
            return NotFound(ApiResponse<WorkflowStepDto>.Fail("Workflow step was not found."));
        }

        var targetError = await ValidateStepTargetAsync(request.StepType, request.ToolId, request.AgentId, cancellationToken);
        if (targetError is not null)
        {
            return BadRequest(ApiResponse<WorkflowStepDto>.Fail(targetError));
        }

        var orderConflict = await _unitOfWork.Repository<WorkflowStep>().AnyAsync(
            workflowStep => workflowStep.WorkflowId == workflowId
                && workflowStep.Id != stepId
                && workflowStep.Order == request.Order,
            cancellationToken);

        if (orderConflict)
        {
            return Conflict(ApiResponse<WorkflowStepDto>.Fail("Another workflow step with this order already exists."));
        }

        _mapper.Map(request, step);
        _unitOfWork.Repository<WorkflowStep>().Update(step);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<WorkflowStepDto>.Ok(_mapper.Map<WorkflowStepDto>(step), "Workflow step updated successfully."));
    }

    [HttpDelete("{workflowId:guid}/steps/{stepId:guid}")]
    [Authorize(Roles = $"{ApplicationRoles.Admin},{ApplicationRoles.Developer}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteWorkflowStep(
        Guid workflowId,
        Guid stepId,
        CancellationToken cancellationToken)
    {
        var step = await _unitOfWork.Repository<WorkflowStep>().FirstOrDefaultAsync(
            workflowStep => workflowStep.WorkflowId == workflowId && workflowStep.Id == stepId && workflowStep.Workflow!.RealmId == RealmAccess.ResolveRealmId(this),
            cancellationToken);

        if (step is null)
        {
            return NotFound(ApiResponse<object>.Fail("Workflow step was not found."));
        }

        var approvalRequests = await _unitOfWork.Repository<HumanApprovalRequest>()
            .Query()
            .Where(approval => approval.WorkflowStepId == stepId)
            .ToListAsync(cancellationToken);

        _unitOfWork.Repository<HumanApprovalRequest>().RemoveRange(approvalRequests);
        _unitOfWork.Repository<WorkflowStep>().Remove(step);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private async Task<string?> ValidateStepTargetAsync(
        WorkflowStepType stepType,
        Guid? toolId,
        Guid? agentId,
        CancellationToken cancellationToken)
    {
        if (stepType == WorkflowStepType.Tool
            && (!toolId.HasValue || !await _unitOfWork.Tools.AnyAsync(tool => tool.Id == toolId.Value && tool.RealmId == RealmAccess.ResolveRealmId(this), cancellationToken)))
        {
            return "Tool target was not found.";
        }

        if (stepType == WorkflowStepType.Agent
            && (!agentId.HasValue || !await _unitOfWork.Agents.AnyAsync(agent => agent.Id == agentId.Value && agent.RealmId == RealmAccess.ResolveRealmId(this), cancellationToken)))
        {
            return "Agent target was not found.";
        }

        return null;
    }

    private static IQueryable<Workflow> ApplySorting(
        IQueryable<Workflow> query,
        string? sortBy,
        string? sortDirection)
    {
        var descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        return sortBy?.Trim().ToLowerInvariant() switch
        {
            "name" => descending ? query.OrderByDescending(workflow => workflow.Name) : query.OrderBy(workflow => workflow.Name),
            "createdat" => descending
                ? query.OrderByDescending(workflow => workflow.CreatedAt)
                : query.OrderBy(workflow => workflow.CreatedAt),
            _ => query.OrderByDescending(workflow => workflow.CreatedAt)
        };
    }
}
