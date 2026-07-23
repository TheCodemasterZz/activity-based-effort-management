using Asp.Versioning;
using EforTakip.Api.Authorization;
using EforTakip.Api.Contracts.Roles;
using EforTakip.Application.Roles.Commands.AssignRoleToUser;
using EforTakip.Application.Roles.Commands.CreateRole;
using EforTakip.Application.Roles.Commands.DeleteRole;
using EforTakip.Application.Roles.Commands.GrantPermission;
using EforTakip.Application.Roles.Commands.RemoveRoleFromUser;
using EforTakip.Application.Roles.Commands.RevokePermission;
using EforTakip.Application.Roles.Commands.UpdateRole;
using EforTakip.Application.Roles.Dtos;
using EforTakip.Application.Roles.Queries.GetPermissionCatalog;
using EforTakip.Application.Roles.Queries.GetRoleById;
using EforTakip.Application.Roles.Queries.GetRoles;
using EforTakip.Domain.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EforTakip.Api.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class RolesController(ISender mediator) : ControllerBase
{
    public sealed record GrantPermissionRequestBody(string PermissionKey);
    public sealed record RevokePermissionRequestBody(string PermissionKey);
    public sealed record AssignUserRequestBody(Guid UserId);

    [RequirePermission(Permissions.Role.Read)]
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<RoleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<RoleDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await mediator.Send(new GetRolesQuery(), cancellationToken));

    [RequirePermission(Permissions.Role.Read)]
    [HttpGet("permission-catalog")]
    [ProducesResponseType(typeof(IReadOnlyCollection<PermissionDescriptor>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<PermissionDescriptor>>> GetPermissionCatalog(CancellationToken cancellationToken)
        => Ok(await mediator.Send(new GetPermissionCatalogQuery(), cancellationToken));

    [RequirePermission(Permissions.Role.Read)]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(RoleDetailDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<RoleDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new GetRoleByIdQuery(id), cancellationToken));

    [RequirePermission(Permissions.Role.Manage)]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateRoleCommand command, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { version = "1.0", id }, new { id });
    }

    [RequirePermission(Permissions.Role.Manage)]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(Guid id, UpdateRoleRequestBody body, CancellationToken cancellationToken)
    {
        await mediator.Send(new UpdateRoleCommand(id, body.Name, body.Description), cancellationToken);
        return NoContent();
    }

    [RequirePermission(Permissions.Role.Manage)]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteRoleCommand(id), cancellationToken);
        return NoContent();
    }

    [RequirePermission(Permissions.Role.Manage)]
    [HttpPost("{id:guid}/permissions")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GrantPermission(
        Guid id, GrantPermissionRequestBody body, CancellationToken cancellationToken)
    {
        await mediator.Send(new GrantPermissionCommand(id, body.PermissionKey), cancellationToken);
        return NoContent();
    }

    [RequirePermission(Permissions.Role.Manage)]
    [HttpPost("{id:guid}/permissions/revoke")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokePermission(
        Guid id, RevokePermissionRequestBody body, CancellationToken cancellationToken)
    {
        await mediator.Send(new RevokePermissionCommand(id, body.PermissionKey), cancellationToken);
        return NoContent();
    }

    [RequirePermission(Permissions.Role.Manage)]
    [HttpPost("{id:guid}/users")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AssignUser(Guid id, AssignUserRequestBody body, CancellationToken cancellationToken)
    {
        await mediator.Send(new AssignRoleToUserCommand(body.UserId, id), cancellationToken);
        return NoContent();
    }

    [RequirePermission(Permissions.Role.Manage)]
    [HttpDelete("{id:guid}/users/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveUser(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        await mediator.Send(new RemoveRoleFromUserCommand(userId, id), cancellationToken);
        return NoContent();
    }
}
