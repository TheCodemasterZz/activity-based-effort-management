using Asp.Versioning;
using EforTakip.Application.Common.Models;
using EforTakip.Application.WorkCalendars.Commands.CreateWorkCalendar;
using EforTakip.Application.WorkCalendars.Dtos;
using EforTakip.Application.WorkCalendars.Queries.GetWorkCalendarById;
using EforTakip.Application.WorkCalendars.Queries.GetWorkCalendars;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EforTakip.Api.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class WorkCalendarsController(ISender mediator) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateWorkCalendarCommand command, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id, version = "1.0" }, null);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(WorkCalendarDetailDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<WorkCalendarDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new GetWorkCalendarByIdQuery(id), cancellationToken));

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<WorkCalendarDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<WorkCalendarDto>>> GetAll(
        [FromQuery] GetWorkCalendarsQuery query, CancellationToken cancellationToken)
        => Ok(await mediator.Send(query, cancellationToken));
}
