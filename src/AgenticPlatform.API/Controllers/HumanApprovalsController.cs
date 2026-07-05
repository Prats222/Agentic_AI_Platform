using AgenticPlatform.Core.Common;
using AgenticPlatform.Core.Constants;
using AgenticPlatform.Core.DTOs.HumanApprovals;
using AgenticPlatform.Core.Enums;
using AgenticPlatform.Core.Interfaces;
using AgenticPlatform.Infrastructure.Data;
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
[Route("api/v{version:apiVersion}/human-approvals")]
public sealed class HumanApprovalsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IExecutionQueue _executionQueue;
    private readonly IMapper _mapper;

    public HumanApprovalsController(ApplicationDbContext dbContext, IExecutionQueue executionQueue, IMapper mapper)
    {
        _dbContext = dbContext;
        _executionQueue = executionQueue;
        _mapper = mapper;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<HumanApprovalRequestDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<HumanApprovalRequestDto>>>> GetApprovals(
        [FromQuery] bool pendingOnly = false,
        CancellationToken cancellationToken = default)
    {
        var realmId = RealmAccess.ResolveRealmId(this);
        if (!RealmAccess.CanAccessRealm(this, realmId))
        {
            return Forbid();
        }

        var query = _dbContext.HumanApprovalRequests
            .AsNoTracking()
            .Where(item => item.Execution.RealmId == realmId)
            .OrderByDescending(item => item.CreatedAt)
            .AsQueryable();

        if (pendingOnly)
        {
            query = query.Where(item => !item.IsApproved && !item.IsRejected);
        }

        var approvals = await query
            .Take(100)
            .ProjectTo<HumanApprovalRequestDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<IReadOnlyCollection<HumanApprovalRequestDto>>.Ok(approvals));
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = $"{ApplicationRoles.Admin},{ApplicationRoles.Developer}")]
    [ProducesResponseType(typeof(ApiResponse<HumanApprovalRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<HumanApprovalRequestDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<HumanApprovalRequestDto>>> Approve(
        Guid id,
        ReviewHumanApprovalDto request,
        CancellationToken cancellationToken)
    {
        var approval = await _dbContext.HumanApprovalRequests
            .Include(item => item.Execution)
            .FirstOrDefaultAsync(item => item.Id == id && item.Execution.RealmId == RealmAccess.ResolveRealmId(this), cancellationToken);

        if (approval is null)
        {
            return NotFound(ApiResponse<HumanApprovalRequestDto>.Fail("Approval request was not found."));
        }

        if (approval.IsRejected)
        {
            return BadRequest(ApiResponse<HumanApprovalRequestDto>.Fail("Rejected approval requests cannot be approved."));
        }

        approval.IsApproved = true;
        approval.ReviewerComment = request.Comment;
        approval.ReviewedAt = DateTimeOffset.UtcNow;

        if (approval.Execution is not null)
        {
            approval.Execution.Status = ExecutionStatus.Pending;
            approval.Execution.CompletedAt = null;
            approval.Execution.ErrorMessage = null;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _executionQueue.QueueAsync(approval.ExecutionId, cancellationToken);

        return Ok(ApiResponse<HumanApprovalRequestDto>.Ok(_mapper.Map<HumanApprovalRequestDto>(approval), "Approval accepted. Execution was resumed."));
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = $"{ApplicationRoles.Admin},{ApplicationRoles.Developer}")]
    [ProducesResponseType(typeof(ApiResponse<HumanApprovalRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<HumanApprovalRequestDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<HumanApprovalRequestDto>>> Reject(
        Guid id,
        ReviewHumanApprovalDto request,
        CancellationToken cancellationToken)
    {
        var approval = await _dbContext.HumanApprovalRequests
            .Include(item => item.Execution)
            .FirstOrDefaultAsync(item => item.Id == id && item.Execution.RealmId == RealmAccess.ResolveRealmId(this), cancellationToken);

        if (approval is null)
        {
            return NotFound(ApiResponse<HumanApprovalRequestDto>.Fail("Approval request was not found."));
        }

        if (approval.IsApproved)
        {
            return BadRequest(ApiResponse<HumanApprovalRequestDto>.Fail("Approved requests cannot be rejected."));
        }

        approval.IsRejected = true;
        approval.ReviewerComment = request.Comment;
        approval.ReviewedAt = DateTimeOffset.UtcNow;

        if (approval.Execution is not null)
        {
            approval.Execution.Status = ExecutionStatus.Failed;
            approval.Execution.CompletedAt = DateTimeOffset.UtcNow;
            approval.Execution.ErrorMessage = "Human approval rejected.";
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<HumanApprovalRequestDto>.Ok(_mapper.Map<HumanApprovalRequestDto>(approval), "Approval rejected. Execution was stopped."));
    }
}
