using Asp.Versioning;
using EforTakip.Application.Common.Models;
using EforTakip.Application.Customers.Commands.CreateCustomer;
using EforTakip.Application.Customers.Dtos;
using EforTakip.Application.Customers.Queries.GetCustomerById;
using EforTakip.Application.Customers.Queries.GetCustomers;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EforTakip.Api.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class CustomersController(ISender mediator) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateCustomerCommand command, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id, version = "1.0" }, null);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomerDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new GetCustomerByIdQuery(id), cancellationToken));

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CustomerDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CustomerDto>>> GetAll(
        [FromQuery] GetCustomersQuery query, CancellationToken cancellationToken)
        => Ok(await mediator.Send(query, cancellationToken));
}
