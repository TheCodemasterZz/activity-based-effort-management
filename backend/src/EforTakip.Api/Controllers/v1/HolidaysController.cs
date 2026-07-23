using Asp.Versioning;
using EforTakip.Api.Authorization;
using EforTakip.Application.Common.Models;
using EforTakip.Application.Holidays.Commands.CreateHoliday;
using EforTakip.Application.Holidays.Dtos;
using EforTakip.Application.Holidays.Queries.GetHolidays;
using EforTakip.Domain.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EforTakip.Api.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class HolidaysController(ISender mediator) : ControllerBase
{
    [RequirePermission(Permissions.Calendar.Manage)]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateHolidayCommand command, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { version = "1.0" }, new { id });
    }

    [RequirePermission(Permissions.Calendar.Read)]
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<HolidayDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<HolidayDto>>> GetAll(
        [FromQuery] GetHolidaysQuery query, CancellationToken cancellationToken)
        => Ok(await mediator.Send(query, cancellationToken));
}
