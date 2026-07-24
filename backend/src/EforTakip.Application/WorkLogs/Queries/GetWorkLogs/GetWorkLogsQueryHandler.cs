using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Common.Models;
using EforTakip.Application.WorkLogs.Dtos;
using EforTakip.Domain.WorkLogs;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.WorkLogs.Queries.GetWorkLogs;

public sealed class GetWorkLogsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetWorkLogsQuery, PagedResult<WorkLogDto>>
{
    public async Task<PagedResult<WorkLogDto>> Handle(
        GetWorkLogsQuery request, CancellationToken cancellationToken)
    {
        IQueryable<WorkLog> query = db.WorkLogs.AsNoTracking()
            .Where(l => l.EntryType == request.EntryType);

        if (request.UserId is { } userId)
            query = query.Where(l => l.UserId == userId);

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
            .ProjectToType<WorkLogDto>()
            .ToListAsync(cancellationToken);

        return new PagedResult<WorkLogDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
