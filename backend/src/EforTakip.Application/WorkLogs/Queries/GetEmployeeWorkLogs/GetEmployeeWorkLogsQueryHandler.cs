using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Common.Models;
using EforTakip.Application.WorkLogs.Dtos;
using EforTakip.Domain.WorkLogs;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.WorkLogs.Queries.GetEmployeeWorkLogs;

public sealed class GetEmployeeWorkLogsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetEmployeeWorkLogsQuery, PagedResult<EmployeeWorkLogDto>>
{
    public async Task<PagedResult<EmployeeWorkLogDto>> Handle(
        GetEmployeeWorkLogsQuery request, CancellationToken cancellationToken)
    {
        IQueryable<EmployeeWorkLog> query = db.EmployeeWorkLogs.AsNoTracking();

        if (request.EmployeeId is { } employeeId)
            query = query.Where(l => l.EmployeeId == employeeId);

        if (request.ProjectId is { } projectId)
            query = query.Where(l => l.ProjectId == projectId);

        if (request.DateFrom is { } dateFrom)
            query = query.Where(l => l.WorkDate >= dateFrom);

        if (request.DateTo is { } dateTo)
            query = query.Where(l => l.WorkDate <= dateTo);

        query = request.SortBy switch
        {
            "workDate" => request.Descending ? query.OrderByDescending(l => l.WorkDate) : query.OrderBy(l => l.WorkDate),
            _ => query.OrderByDescending(l => l.WorkDate)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ProjectToType<EmployeeWorkLogDto>()
            .ToListAsync(cancellationToken);

        return new PagedResult<EmployeeWorkLogDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
