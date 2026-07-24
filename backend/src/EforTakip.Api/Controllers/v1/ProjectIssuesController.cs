using Asp.Versioning;
using EforTakip.Api.Authorization;
using EforTakip.Api.Contracts.ProjectIssues;
using EforTakip.Application.Common.Models;
using EforTakip.Application.Projects.Commands.CreateProjectIssue;
using EforTakip.Application.Projects.Commands.DeleteProjectIssue;
using EforTakip.Application.Projects.Commands.UpdateProjectIssue;
using EforTakip.Application.Projects.Commands.UpdateProjectIssueStatus;
using EforTakip.Application.Projects.Dtos;
using EforTakip.Application.Projects.Queries.GetProjectIssues;
using EforTakip.Domain.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EforTakip.Api.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class ProjectIssuesController(ISender mediator) : ControllerBase
{
    [RequirePermission(Permissions.Project.Update)]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateProjectIssueCommand command, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { version = "1.0" }, new { id });
    }

    [RequirePermission(Permissions.Project.Read)]
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProjectIssueDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ProjectIssueDto>>> GetAll(
        [FromQuery] GetProjectIssuesQuery query, CancellationToken cancellationToken)
        => Ok(await mediator.Send(query, cancellationToken));

    [RequirePermission(Permissions.Project.Update)]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(Guid id, UpdateProjectIssueRequestBody body, CancellationToken cancellationToken)
    {
        await mediator.Send(
            new UpdateProjectIssueCommand(
                id, body.Title, body.Description, body.Priority,
                body.OwnerUserId, body.DueDate, body.Resolution),
            cancellationToken);
        return NoContent();
    }

    [RequirePermission(Permissions.Project.Update)]
    [HttpPut("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateStatus(Guid id, UpdateProjectIssueStatusRequestBody body, CancellationToken cancellationToken)
    {
        await mediator.Send(new UpdateProjectIssueStatusCommand(id, body.Status), cancellationToken);
        return NoContent();
    }

    [RequirePermission(Permissions.Project.Update)]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteProjectIssueCommand(id), cancellationToken);
        return NoContent();
    }
}
