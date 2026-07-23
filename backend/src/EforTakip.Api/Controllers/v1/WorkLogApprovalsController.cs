using Asp.Versioning;
using EforTakip.Api.Authorization;
using EforTakip.Api.Contracts.WorkLogApprovals;
using EforTakip.Application.Common.Models;
using EforTakip.Application.WorkLogApprovals.Commands.CreateWorkLogApproval;
using EforTakip.Application.WorkLogApprovals.Dtos;
using EforTakip.Application.WorkLogApprovals.Queries.GetWorkLogApprovals;
using EforTakip.Domain.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EforTakip.Api.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class WorkLogApprovalsController(ISender mediator) : ControllerBase
{
    [RequirePermission(Permissions.WorkLog.Approve)]
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateWorkLogApprovalRequestBody body, CancellationToken cancellationToken)
    {
        var command = new CreateWorkLogApprovalCommand(
            body.EmployeeId, body.PeriodType, body.PeriodStart, body.PeriodEnd, body.Description, body.EntryType);
        var id = await mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new { id });
    }

    [RequirePermission(Permissions.WorkLog.Read)]
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<WorkLogApprovalDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<WorkLogApprovalDto>>> GetAll(
        [FromQuery] GetWorkLogApprovalsQuery query, CancellationToken cancellationToken)
        => Ok(await mediator.Send(query, cancellationToken));
}
