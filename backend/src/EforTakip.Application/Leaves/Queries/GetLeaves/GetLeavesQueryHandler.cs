using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Common.Models;
using EforTakip.Application.Leaves.Dtos;
using EforTakip.Domain.Leaves;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.Leaves.Queries.GetLeaves;

public sealed class GetLeavesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetLeavesQuery, PagedResult<LeaveDto>>
{
    public async Task<PagedResult<LeaveDto>> Handle(
        GetLeavesQuery request, CancellationToken cancellationToken)
    {
        IQueryable<Leave> query = db.Leaves.AsNoTracking();

        if (request.UserId is { } userId)
            query = query.Where(l => l.UserId == userId);

        if (request.DateFrom is { } dateFrom)
            query = query.Where(l => l.EndDate >= dateFrom);

        if (request.DateTo is { } dateTo)
            query = query.Where(l => l.StartDate <= dateTo);

        query = query.OrderByDescending(l => l.StartDate);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ProjectToType<LeaveDto>()
            .ToListAsync(cancellationToken);

        return new PagedResult<LeaveDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
