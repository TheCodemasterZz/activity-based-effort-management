using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Common.Models;
using EforTakip.Application.Employees.Dtos;
using EforTakip.Domain.Employees;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.Employees.Queries.GetEmployees;

public sealed class GetEmployeesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetEmployeesQuery, PagedResult<EmployeeDto>>
{
    public async Task<PagedResult<EmployeeDto>> Handle(GetEmployeesQuery request, CancellationToken cancellationToken)
    {
        IQueryable<Employee> query = db.Employees.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.NameFilter))
        {
            var nameFilter = request.NameFilter.ToLower();
            query = query.Where(e => e.Name.ToLower().Contains(nameFilter));
        }

        query = request.SortBy switch
        {
            "name" => request.Descending ? query.OrderByDescending(e => e.Name) : query.OrderBy(e => e.Name),
            _ => query.OrderByDescending(e => e.Id)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ProjectToType<EmployeeDto>()
            .ToListAsync(cancellationToken);

        return new PagedResult<EmployeeDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
