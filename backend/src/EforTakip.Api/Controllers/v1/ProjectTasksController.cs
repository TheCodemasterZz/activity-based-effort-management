using Asp.Versioning;
using EforTakip.Api.Authorization;
using EforTakip.Api.Contracts.ProjectTasks;
using EforTakip.Application.Common.Models;
using EforTakip.Application.Projects.Commands.CreateProjectTask;
using EforTakip.Application.Projects.Commands.DeleteProjectTask;
using EforTakip.Application.Projects.Commands.UpdateProjectTask;
using EforTakip.Application.Projects.Commands.UpdateProjectTaskStatus;
using EforTakip.Application.Projects.Dtos;
using EforTakip.Application.Projects.Queries.GetProjectTasks;
using EforTakip.Domain.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EforTakip.Api.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class ProjectTasksController(ISender mediator) : ControllerBase
{
    [RequirePermission(Permissions.Project.Update)]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateProjectTaskCommand command, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { version = "1.0" }, new { id });
    }

    [RequirePermission(Permissions.Project.Read)]
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProjectTaskDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ProjectTaskDto>>> GetAll(
        [FromQuery] GetProjectTasksQuery query, CancellationToken cancellationToken)
        => Ok(await mediator.Send(query, cancellationToken));

    [RequirePermission(Permissions.Project.Update)]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(Guid id, UpdateProjectTaskRequestBody body, CancellationToken cancellationToken)
    {
        await mediator.Send(
            new UpdateProjectTaskCommand(
                id, body.Name, body.StartDate, body.EndDate, body.EstimatedEffortHours, body.IsMilestone,
                body.ParentTaskId, body.DependsOnTaskId, body.AssignedUserId),
            cancellationToken);
        return NoContent();
    }

    [RequirePermission(Permissions.Project.Update)]
    [HttpPut("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateStatus(Guid id, UpdateProjectTaskStatusRequestBody body, CancellationToken cancellationToken)
    {
        await mediator.Send(new UpdateProjectTaskStatusCommand(id, body.Status), cancellationToken);
        return NoContent();
    }

    [RequirePermission(Permissions.Project.Update)]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteProjectTaskCommand(id), cancellationToken);
        return NoContent();
    }
}
