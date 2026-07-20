using EforTakip.Application.Common.Models;
using EforTakip.Application.Customers.Dtos;
using MediatR;

namespace EforTakip.Application.Customers.Queries.GetCustomers;

public sealed class GetCustomersQuery : PaginationParams, IRequest<PagedResult<CustomerDto>>
{
    public string? NameFilter { get; set; }

    /// <summary>Doluysa sadece bu projeye atanmış müşteriler döner.</summary>
    public Guid? ProjectId { get; set; }
}
