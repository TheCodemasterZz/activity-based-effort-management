using Asp.Versioning;
using EforTakip.Application.Common.Models;
using EforTakip.Application.Employees.Commands.CreateEmployee;
using EforTakip.Application.Employees.Dtos;
using EforTakip.Application.Employees.Queries.GetEmployeeById;
using EforTakip.Application.Employees.Queries.GetEmployees;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EforTakip.Api.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class EmployeesController(ISender mediator) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateEmployeeCommand command, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id, version = "1.0" }, null);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EmployeeDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<EmployeeDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new GetEmployeeByIdQuery(id), cancellationToken));

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<EmployeeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<EmployeeDto>>> GetAll(
        [FromQuery] GetEmployeesQuery query, CancellationToken cancellationToken)
        => Ok(await mediator.Send(query, cancellationToken));
}
