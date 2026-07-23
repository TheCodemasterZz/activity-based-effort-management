using Asp.Versioning;
using EforTakip.Api.Authorization;
using EforTakip.Application.Directories.Commands.CreateAttributeMapping;
using EforTakip.Application.Directories.Commands.DeleteAttributeMapping;
using EforTakip.Application.Directories.Commands.UpdateAttributeMapping;
using EforTakip.Application.Directories.Dtos;
using EforTakip.Application.Directories.Queries.GetAttributeMappings;
using EforTakip.Domain.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EforTakip.Api.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/directories/{directoryId:guid}/attribute-mappings")]
public sealed class DirectoryAttributeMappingsController(ISender mediator) : ControllerBase
{
    public sealed record CreateAttributeMappingRequest(
        string AdAttributeName, string SystemFieldName, string FieldType, bool IsSynced, int SortOrder);

    [RequirePermission(Permissions.Directory.Read)]
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<DirectoryAttributeMappingDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<DirectoryAttributeMappingDto>>> GetAll(
        Guid directoryId, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new GetAttributeMappingsQuery(directoryId), cancellationToken));

    [RequirePermission(Permissions.Directory.Manage)]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        Guid directoryId, CreateAttributeMappingRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateAttributeMappingCommand(
            directoryId, request.AdAttributeName, request.SystemFieldName, request.FieldType,
            request.IsSynced, request.SortOrder);
        var id = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { version = "1.0", directoryId }, new { id });
    }

    [RequirePermission(Permissions.Directory.Manage)]
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

    [RequirePermission(Permissions.Directory.Manage)]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteAttributeMappingCommand(id), cancellationToken);
        return NoContent();
    }
}
