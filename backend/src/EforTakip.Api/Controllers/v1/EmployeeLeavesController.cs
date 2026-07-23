using Asp.Versioning;
using EforTakip.Api.Authorization;
using EforTakip.Api.Contracts.EmployeeLeaves;
using EforTakip.Application.Common.Models;
using EforTakip.Application.EmployeeLeaves.Commands.CreateEmployeeLeave;
using EforTakip.Application.EmployeeLeaves.Commands.DeleteEmployeeLeave;
using EforTakip.Application.EmployeeLeaves.Dtos;
using EforTakip.Application.EmployeeLeaves.Queries.GetEmployeeLeaves;
using EforTakip.Domain.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EforTakip.Api.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class EmployeeLeavesController(ISender mediator) : ControllerBase
{
    [RequirePermission(Permissions.Employee.Manage)]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateEmployeeLeaveRequestBody body, CancellationToken cancellationToken)
    {
        var command = new CreateEmployeeLeaveCommand(
            body.EmployeeId, body.StartDate, body.EndDate, body.IsFullDay, body.StartTime, body.EndTime, body.Description);
        var id = await mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new { id });
    }

    [RequirePermission(Permissions.Employee.Read)]
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<EmployeeLeaveDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<EmployeeLeaveDto>>> GetAll(
        [FromQuery] GetEmployeeLeavesQuery query, CancellationToken cancellationToken)
        => Ok(await mediator.Send(query, cancellationToken));

    [RequirePermission(Permissions.Employee.Manage)]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteEmployeeLeaveCommand(id), cancellationToken);
        return NoContent();
    }
}
