using Asp.Versioning;
using EforTakip.Application.Directories.Commands.CreateAttributeMapping;
using EforTakip.Application.Directories.Commands.DeleteAttributeMapping;
using EforTakip.Application.Directories.Commands.UpdateAttributeMapping;
using EforTakip.Application.Directories.Dtos;
using EforTakip.Application.Directories.Queries.GetAttributeMappings;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EforTakip.Api.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class DirectoryAttributeMappingsController(ISender mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<DirectoryAttributeMappingDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<DirectoryAttributeMappingDto>>> GetAll(
        [FromQuery] Guid directoryId, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new GetAttributeMappingsQuery(directoryId), cancellationToken));

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        CreateAttributeMappingCommand command, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { version = "1.0" }, new { id });
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(
        Guid id, UpdateAttributeMappingCommand command, CancellationToken cancellationToken)
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
        await mediator.Send(new DeleteAttributeMappingCommand(id), cancellationToken);
        return NoContent();
    }
}
