using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Common.Models;
using EforTakip.Application.EmployeeLeaves.Dtos;
using EforTakip.Domain.EmployeeLeaves;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.EmployeeLeaves.Queries.GetEmployeeLeaves;

public sealed class GetEmployeeLeavesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetEmployeeLeavesQuery, PagedResult<EmployeeLeaveDto>>
{
    public async Task<PagedResult<EmployeeLeaveDto>> Handle(
        GetEmployeeLeavesQuery request, CancellationToken cancellationToken)
    {
        IQueryable<EmployeeLeave> query = db.EmployeeLeaves.AsNoTracking();

        if (request.EmployeeId is { } employeeId)
            query = query.Where(l => l.EmployeeId == employeeId);

        if (request.DateFrom is { } dateFrom)
            query = query.Where(l => l.EndDate >= dateFrom);

        if (request.DateTo is { } dateTo)
            query = query.Where(l => l.StartDate <= dateTo);

        query = query.OrderByDescending(l => l.StartDate);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ProjectToType<EmployeeLeaveDto>()
            .ToListAsync(cancellationToken);

        return new PagedResult<EmployeeLeaveDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
