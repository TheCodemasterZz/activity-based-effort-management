using EforTakip.Application.Activities.Dtos;
using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Common.Models;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using DomainActivity = EforTakip.Domain.Activities.Activity;

namespace EforTakip.Application.Activities.Queries.GetActivities;

public sealed class GetActivitiesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetActivitiesQuery, PagedResult<ActivityDto>>
{
    public async Task<PagedResult<ActivityDto>> Handle(GetActivitiesQuery request, CancellationToken cancellationToken)
    {
        IQueryable<DomainActivity> query = db.Activities.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.NameFilter))
        {
            var nameFilter = request.NameFilter.ToLower();
            query = query.Where(a => a.Name.ToLower().Contains(nameFilter));
        }

        if (request.OnlyTopLevel)
            query = query.Where(a => a.ParentActivityId == null);
        else if (request.ParentActivityId is { } parentId)
            query = query.Where(a => a.ParentActivityId == parentId);

        query = request.SortBy switch
        {
            "name" => request.Descending ? query.OrderByDescending(a => a.Name) : query.OrderBy(a => a.Name),
            _ => query.OrderByDescending(a => a.Id)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ProjectToType<ActivityDto>()
            .ToListAsync(cancellationToken);

        return new PagedResult<ActivityDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
