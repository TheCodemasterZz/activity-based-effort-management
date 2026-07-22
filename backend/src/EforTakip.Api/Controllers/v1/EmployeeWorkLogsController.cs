using Asp.Versioning;
using EforTakip.Api.Contracts.WorkLogs;
using EforTakip.Application.Common.Models;
using EforTakip.Application.WorkLogs.Commands.DeleteWorkLog;
using EforTakip.Application.WorkLogs.Commands.LogWork;
using EforTakip.Application.WorkLogs.Commands.UpdateWorkLog;
using EforTakip.Application.WorkLogs.Dtos;
using EforTakip.Application.WorkLogs.Queries.GetEmployeeWorkLogs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EforTakip.Api.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class EmployeeWorkLogsController(ISender mediator) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> LogWork(LogWorkRequestBody body, CancellationToken cancellationToken)
    {
        var command = new LogWorkCommand(
            body.EmployeeId, body.ProjectId,
            body.ActivityL1Id, body.ActivityL2Id, body.StartDate, body.EndDate, body.Hours, body.Description,
            body.EntryType);

        var ids = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { version = "1.0" }, new { ids });
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<EmployeeWorkLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<EmployeeWorkLogDto>>> GetAll(
        [FromQuery] GetEmployeeWorkLogsQuery query, CancellationToken cancellationToken)
        => Ok(await mediator.Send(query, cancellationToken));

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(Guid id, UpdateWorkLogRequestBody body, CancellationToken cancellationToken)
    {
        var command = new UpdateWorkLogCommand(
            id, body.EmployeeId, body.ProjectId,
            body.ActivityL1Id, body.ActivityL2Id, body.WorkDate, body.Hours, body.Description);

        await mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteWorkLogCommand(id), cancellationToken);
        return NoContent();
    }
}
