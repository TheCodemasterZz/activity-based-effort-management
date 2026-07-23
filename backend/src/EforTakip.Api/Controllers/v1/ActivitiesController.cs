using Asp.Versioning;
using EforTakip.Api.Authorization;
using EforTakip.Application.Activities.Commands.CreateActivity;
using EforTakip.Application.Activities.Dtos;
using EforTakip.Application.Activities.Queries.GetActivities;
using EforTakip.Application.Activities.Queries.GetActivityById;
using EforTakip.Application.Common.Models;
using EforTakip.Domain.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EforTakip.Api.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class ActivitiesController(ISender mediator) : ControllerBase
{
    [RequirePermission(Permissions.Activity.Manage)]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateActivityCommand command, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id, version = "1.0" }, null);
    }

    [RequirePermission(Permissions.Activity.Read)]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ActivityDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ActivityDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new GetActivityByIdQuery(id), cancellationToken));

    [RequirePermission(Permissions.Activity.Read)]
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ActivityDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ActivityDto>>> GetAll(
        [FromQuery] GetActivitiesQuery query, CancellationToken cancellationToken)
        => Ok(await mediator.Send(query, cancellationToken));
}
