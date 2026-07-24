using Asp.Versioning;
using EforTakip.Api.Authorization;
using EforTakip.Application.Common.Models;
using EforTakip.Application.Users.Commands.AssignWorkCalendar;
using EforTakip.Application.Users.Commands.BulkAssignWorkCalendar;
using EforTakip.Application.Users.Commands.CreateInternalUser;
using EforTakip.Application.Users.Commands.ResetInternalUserPassword;
using EforTakip.Application.Users.Dtos;
using EforTakip.Application.Users.Queries.GetUserById;
using EforTakip.Application.Users.Queries.GetUsers;
using EforTakip.Domain.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EforTakip.Api.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class UsersController(ISender mediator) : ControllerBase
{
    [RequirePermission(Permissions.User.Read)]
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<UserDto>>> GetAll(
        [FromQuery] GetUsersQuery query, CancellationToken cancellationToken)
        => Ok(await mediator.Send(query, cancellationToken));

    [RequirePermission(Permissions.User.Read)]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserDetailDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new GetUserByIdQuery(id), cancellationToken));

    [RequirePermission(Permissions.User.Manage)]
    [HttpPost("internal")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateInternal(
        CreateInternalUserCommand command, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id, version = "1.0" }, null);
    }

    [RequirePermission(Permissions.User.Manage)]
    [HttpPost("{id:guid}/reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ResetPassword(
        Guid id, ResetInternalUserPasswordCommand command, CancellationToken cancellationToken)
    {
        if (id != command.UserId)
            return BadRequest("Route ve gövde kimlikleri eşleşmiyor.");
        await mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [RequirePermission(Permissions.User.Manage)]
    [HttpPost("{id:guid}/work-calendar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AssignWorkCalendar(
        Guid id, AssignWorkCalendarCommand command, CancellationToken cancellationToken)
    {
        if (id != command.UserId)
            return BadRequest("Route ve gövde kimlikleri eşleşmiyor.");
        await mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [RequirePermission(Permissions.User.Manage)]
    [HttpPost("work-calendar/bulk")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> BulkAssignWorkCalendar(
        BulkAssignWorkCalendarCommand command, CancellationToken cancellationToken)
    {
        await mediator.Send(command, cancellationToken);
        return NoContent();
    }
}
