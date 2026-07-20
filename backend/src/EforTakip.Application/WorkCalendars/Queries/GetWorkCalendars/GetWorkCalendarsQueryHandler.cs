using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Common.Models;
using EforTakip.Application.WorkCalendars.Dtos;
using EforTakip.Domain.WorkCalendars;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.WorkCalendars.Queries.GetWorkCalendars;

public sealed class GetWorkCalendarsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetWorkCalendarsQuery, PagedResult<WorkCalendarDto>>
{
    public async Task<PagedResult<WorkCalendarDto>> Handle(
        GetWorkCalendarsQuery request, CancellationToken cancellationToken)
    {
        IQueryable<WorkCalendar> query = db.WorkCalendars.AsNoTracking().OrderBy(c => c.Name);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ProjectToType<WorkCalendarDto>()
            .ToListAsync(cancellationToken);

        return new PagedResult<WorkCalendarDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
