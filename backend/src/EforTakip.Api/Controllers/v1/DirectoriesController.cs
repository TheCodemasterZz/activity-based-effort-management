using Asp.Versioning;
using EforTakip.Api.Authorization;
using EforTakip.Application.Common.Models;
using EforTakip.Application.Directories.Commands.CreateDirectory;
using EforTakip.Application.Directories.Commands.DeleteDirectory;
using EforTakip.Application.Directories.Commands.SyncDirectory;
using EforTakip.Application.Directories.Commands.TestDirectoryConnection;
using EforTakip.Application.Directories.Commands.UpdateDirectory;
using EforTakip.Application.Directories.Dtos;
using EforTakip.Application.Directories.Ldap;
using EforTakip.Application.Directories.Queries.GetDirectories;
using EforTakip.Application.Directories.Queries.GetDirectoryById;
using EforTakip.Application.Directories.Queries.GetOrgChart;
using EforTakip.Domain.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EforTakip.Api.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class DirectoriesController(ISender mediator) : ControllerBase
{
    [RequirePermission(Permissions.Directory.Manage)]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateDirectoryCommand command, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id, version = "1.0" }, null);
    }

    [RequirePermission(Permissions.Directory.Read)]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DirectoryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DirectoryDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new GetDirectoryByIdQuery(id), cancellationToken));

    [RequirePermission(Permissions.Directory.Read)]
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<DirectoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<DirectoryDto>>> GetAll(
        [FromQuery] GetDirectoriesQuery query, CancellationToken cancellationToken)
        => Ok(await mediator.Send(query, cancellationToken));

    [RequirePermission(Permissions.Directory.Manage)]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(Guid id, UpdateDirectoryCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id)
            return BadRequest("Route ve gövde kimlikleri eşleşmiyor.");
        await mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [RequirePermission(Permissions.Directory.Manage)]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteDirectoryCommand(id), cancellationToken);
        return NoContent();
    }

    [RequirePermission(Permissions.Directory.Manage)]
    [HttpPost("{id:guid}/sync")]
    [ProducesResponseType(typeof(DirectorySyncResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DirectorySyncResultDto>> Sync(Guid id, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new SyncDirectoryCommand(id), cancellationToken));

    [RequirePermission(Permissions.Directory.Manage)]
    [HttpPost("{id:guid}/test-connection")]
    [ProducesResponseType(typeof(LdapConnectionTestResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<LdapConnectionTestResult>> TestConnection(
        Guid id, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new TestDirectoryConnectionCommand(id), cancellationToken));

    [RequirePermission(Permissions.Directory.Read)]
    [HttpGet("{id:guid}/org-chart")]
    [ProducesResponseType(typeof(OrgChartResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<OrgChartResultDto>> GetOrgChart(Guid id, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new GetOrgChartQuery(id), cancellationToken));
}
