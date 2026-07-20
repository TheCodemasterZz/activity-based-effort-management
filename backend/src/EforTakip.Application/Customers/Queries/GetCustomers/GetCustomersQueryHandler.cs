using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Common.Models;
using EforTakip.Application.Customers.Dtos;
using EforTakip.Domain.Customers;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.Customers.Queries.GetCustomers;

public sealed class GetCustomersQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetCustomersQuery, PagedResult<CustomerDto>>
{
    public async Task<PagedResult<CustomerDto>> Handle(GetCustomersQuery request, CancellationToken cancellationToken)
    {
        IQueryable<Customer> query = db.Customers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.NameFilter))
        {
            var nameFilter = request.NameFilter.ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(nameFilter));
        }

        if (request.ProjectId is { } projectId)
        {
            var assignedCustomerIds = db.ProjectCustomerAssignments
                .Where(a => a.ProjectId == projectId)
                .Select(a => a.CustomerId);
            query = query.Where(c => assignedCustomerIds.Contains(c.Id));
        }

        query = request.SortBy switch
        {
            "name" => request.Descending ? query.OrderByDescending(c => c.Name) : query.OrderBy(c => c.Name),
            _ => query.OrderByDescending(c => c.Id)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ProjectToType<CustomerDto>()
            .ToListAsync(cancellationToken);

        return new PagedResult<CustomerDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
