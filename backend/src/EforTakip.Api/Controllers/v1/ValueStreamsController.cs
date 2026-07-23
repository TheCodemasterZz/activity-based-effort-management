using Asp.Versioning;
using EforTakip.Api.Authorization;
using EforTakip.Api.Contracts.ValueStreams;
using EforTakip.Application.Common.Models;
using EforTakip.Application.ValueStreams.Commands.AddStage;
using EforTakip.Application.ValueStreams.Commands.AssignActivityToStage;
using EforTakip.Application.ValueStreams.Commands.CreateValueStream;
using EforTakip.Application.ValueStreams.Dtos;
using EforTakip.Application.ValueStreams.Queries.GetValueStreamById;
using EforTakip.Application.ValueStreams.Queries.GetValueStreams;
using EforTakip.Domain.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EforTakip.Api.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class ValueStreamsController(ISender mediator) : ControllerBase
{
    [RequirePermission(Permissions.ValueStream.Manage)]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateValueStreamCommand command, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id, version = "1.0" }, null);
    }

    [RequirePermission(Permissions.ValueStream.Read)]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ValueStreamDetailDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ValueStreamDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new GetValueStreamByIdQuery(id), cancellationToken));

    [RequirePermission(Permissions.ValueStream.Read)]
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ValueStreamDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ValueStreamDto>>> GetAll(
        [FromQuery] GetValueStreamsQuery query, CancellationToken cancellationToken)
        => Ok(await mediator.Send(query, cancellationToken));

    [RequirePermission(Permissions.ValueStream.Manage)]
    [HttpPost("{id:guid}/stages")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> AddStage(Guid id, AddStageRequestBody body, CancellationToken cancellationToken)
    {
        var stageId = await mediator.Send(new AddStageCommand(id, body.Name, body.Order), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id, version = "1.0" }, new { stageId });
    }

    [RequirePermission(Permissions.ValueStream.Manage)]
    [HttpPost("{id:guid}/stages/{stageId:guid}/activities")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AssignActivity(
        Guid id, Guid stageId, AssignActivityRequestBody body, CancellationToken cancellationToken)
    {
        await mediator.Send(new AssignActivityToStageCommand(stageId, body.ActivityId), cancellationToken);
        return NoContent();
    }
}
