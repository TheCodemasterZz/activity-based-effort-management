using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Common.Models;
using EforTakip.Application.Holidays.Dtos;
using EforTakip.Domain.Holidays;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.Holidays.Queries.GetHolidays;

public sealed class GetHolidaysQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetHolidaysQuery, PagedResult<HolidayDto>>
{
    public async Task<PagedResult<HolidayDto>> Handle(GetHolidaysQuery request, CancellationToken cancellationToken)
    {
        IQueryable<Holiday> query = db.Holidays.AsNoTracking();

        if (request.Year is { } year)
            query = query.Where(h => h.Date.Year == year);

        query = query.OrderBy(h => h.Date);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ProjectToType<HolidayDto>()
            .ToListAsync(cancellationToken);

        return new PagedResult<HolidayDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
