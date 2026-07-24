using Asp.Versioning;
using EforTakip.Api.Authorization;
using EforTakip.Api.Contracts.ProjectRisks;
using EforTakip.Application.Common.Models;
using EforTakip.Application.Projects.Commands.CreateProjectRisk;
using EforTakip.Application.Projects.Commands.DeleteProjectRisk;
using EforTakip.Application.Projects.Commands.UpdateProjectRisk;
using EforTakip.Application.Projects.Commands.UpdateProjectRiskStatus;
using EforTakip.Application.Projects.Dtos;
using EforTakip.Application.Projects.Queries.GetProjectRisks;
using EforTakip.Domain.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EforTakip.Api.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class ProjectRisksController(ISender mediator) : ControllerBase
{
    [RequirePermission(Permissions.Project.Update)]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateProjectRiskCommand command, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { version = "1.0" }, new { id });
    }

    [RequirePermission(Permissions.Project.Read)]
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProjectRiskDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ProjectRiskDto>>> GetAll(
        [FromQuery] GetProjectRisksQuery query, CancellationToken cancellationToken)
        => Ok(await mediator.Send(query, cancellationToken));

    [RequirePermission(Permissions.Project.Update)]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(Guid id, UpdateProjectRiskRequestBody body, CancellationToken cancellationToken)
    {
        await mediator.Send(
            new UpdateProjectRiskCommand(
                id, body.Title, body.Description, body.Probability, body.Impact,
                body.MitigationPlan, body.OwnerUserId, body.IdentifiedDate),
            cancellationToken);
        return NoContent();
    }

    [RequirePermission(Permissions.Project.Update)]
    [HttpPut("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateStatus(Guid id, UpdateProjectRiskStatusRequestBody body, CancellationToken cancellationToken)
    {
        await mediator.Send(new UpdateProjectRiskStatusCommand(id, body.Status), cancellationToken);
        return NoContent();
    }

    [RequirePermission(Permissions.Project.Update)]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteProjectRiskCommand(id), cancellationToken);
        return NoContent();
    }
}
