using Asp.Versioning;
using EforTakip.Application.Common.Models;
using EforTakip.Application.Directories.Commands.CreateInternalUser;
using EforTakip.Application.Directories.Commands.ResetInternalUserPassword;
using EforTakip.Application.Directories.Dtos;
using EforTakip.Application.Directories.Queries.GetDirectoryUserById;
using EforTakip.Application.Directories.Queries.GetDirectoryUsers;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EforTakip.Api.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class DirectoryUsersController(ISender mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<DirectoryUserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<DirectoryUserDto>>> GetAll(
        [FromQuery] GetDirectoryUsersQuery query, CancellationToken cancellationToken)
        => Ok(await mediator.Send(query, cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DirectoryUserDetailDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DirectoryUserDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new GetDirectoryUserByIdQuery(id), cancellationToken));

    [HttpPost("internal")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateInternal(
        CreateInternalUserCommand command, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id, version = "1.0" }, null);
    }

    [HttpPost("{id:guid}/reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ResetPassword(
        Guid id, ResetInternalUserPasswordCommand command, CancellationToken cancellationToken)
    {
        if (id != command.DirectoryUserId)
            return BadRequest("Route ve gövde kimlikleri eşleşmiyor.");
        await mediator.Send(command, cancellationToken);
        return NoContent();
    }
}
