using Asp.Versioning;
using EforTakip.Api.Authorization;
using EforTakip.Api.Contracts.Projects;
using EforTakip.Application.Common.Models;
using EforTakip.Application.Projects.Commands.AssignCustomerToProject;
using EforTakip.Application.Projects.Commands.AssignEmployeeToProject;
using EforTakip.Application.Projects.Commands.CreateProject;
using EforTakip.Application.Projects.Commands.DeleteProject;
using EforTakip.Application.Projects.Commands.UpdateProject;
using EforTakip.Application.Projects.Commands.UpdateProjectHealth;
using EforTakip.Application.Projects.Dtos;
using EforTakip.Application.Projects.Queries.GetProjectById;
using EforTakip.Application.Projects.Queries.GetProjects;
using EforTakip.Domain.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EforTakip.Api.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class ProjectsController(ISender mediator) : ControllerBase
{
    [RequirePermission(Permissions.Project.Create)]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateProjectCommand command, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id, version = "1.0" }, null);
    }

    [RequirePermission(Permissions.Project.Read)]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProjectDetailDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProjectDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new GetProjectByIdQuery(id), cancellationToken));

    [RequirePermission(Permissions.Project.Read)]
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProjectDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ProjectDto>>> GetAll(
        [FromQuery] GetProjectsQuery query, CancellationToken cancellationToken)
        => Ok(await mediator.Send(query, cancellationToken));

    [RequirePermission(Permissions.Project.Update)]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(Guid id, UpdateProjectRequestBody body, CancellationToken cancellationToken)
    {
        await mediator.Send(
            new UpdateProjectCommand(
                id, body.Name, body.Description, body.StartDate, body.EndDate,
                body.Sponsor, body.ProjectManagerEmployeeId, body.Priority, body.StrategicGoal),
            cancellationToken);
        return NoContent();
    }

    [RequirePermission(Permissions.Project.Update)]
    [HttpPut("{id:guid}/health")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateHealth(Guid id, UpdateProjectHealthRequestBody body, CancellationToken cancellationToken)
    {
        await mediator.Send(new UpdateProjectHealthCommand(id, body.HealthStatus), cancellationToken);
        return NoContent();
    }

    [RequirePermission(Permissions.Project.Delete)]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteProjectCommand(id), cancellationToken);
        return NoContent();
    }

    [RequirePermission(Permissions.Project.Update)]
    [HttpPost("{id:guid}/customers")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AssignCustomer(Guid id, AssignCustomerRequestBody body, CancellationToken cancellationToken)
    {
        await mediator.Send(new AssignCustomerToProjectCommand(id, body.CustomerId), cancellationToken);
        return NoContent();
    }

    [RequirePermission(Permissions.Project.Update)]
    [HttpPost("{id:guid}/employees")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AssignEmployee(Guid id, AssignEmployeeRequestBody body, CancellationToken cancellationToken)
    {
        await mediator.Send(new AssignEmployeeToProjectCommand(id, body.EmployeeId), cancellationToken);
        return NoContent();
    }
}
