using Asp.Versioning;
using EforTakip.Application.Common.Models;
using EforTakip.Application.Directories.Commands.CreateDirectory;
using EforTakip.Application.Directories.Commands.DeleteDirectory;
using EforTakip.Application.Directories.Commands.UpdateDirectory;
using EforTakip.Application.Directories.Dtos;
using EforTakip.Application.Directories.Queries.GetDirectories;
using EforTakip.Application.Directories.Queries.GetDirectoryById;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EforTakip.Api.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class DirectoriesController(ISender mediator) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateDirectoryCommand command, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id, version = "1.0" }, null);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DirectoryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DirectoryDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new GetDirectoryByIdQuery(id), cancellationToken));

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<DirectoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<DirectoryDto>>> GetAll(
        [FromQuery] GetDirectoriesQuery query, CancellationToken cancellationToken)
        => Ok(await mediator.Send(query, cancellationToken));

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(Guid id, UpdateDirectoryCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id)
            return BadRequest("Route ve gövde kimlikleri eşleşmiyor.");
        await mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteDirectoryCommand(id), cancellationToken);
        return NoContent();
    }
}
