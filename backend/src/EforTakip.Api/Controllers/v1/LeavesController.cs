using Asp.Versioning;
using EforTakip.Api.Authorization;
using EforTakip.Api.Contracts.Leaves;
using EforTakip.Application.Common.Models;
using EforTakip.Application.Leaves.Commands.CreateLeave;
using EforTakip.Application.Leaves.Commands.DeleteLeave;
using EforTakip.Application.Leaves.Dtos;
using EforTakip.Application.Leaves.Queries.GetLeaves;
using EforTakip.Domain.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EforTakip.Api.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class LeavesController(ISender mediator) : ControllerBase
{
    [RequirePermission(Permissions.Employee.Manage)]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateLeaveRequestBody body, CancellationToken cancellationToken)
    {
        var command = new CreateLeaveCommand(
            body.UserId, body.StartDate, body.EndDate, body.IsFullDay, body.StartTime, body.EndTime, body.Description);
        var id = await mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new { id });
    }

    [RequirePermission(Permissions.Employee.Read)]
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<LeaveDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<LeaveDto>>> GetAll(
        [FromQuery] GetLeavesQuery query, CancellationToken cancellationToken)
        => Ok(await mediator.Send(query, cancellationToken));

    [RequirePermission(Permissions.Employee.Manage)]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteLeaveCommand(id), cancellationToken);
        return NoContent();
    }
}
