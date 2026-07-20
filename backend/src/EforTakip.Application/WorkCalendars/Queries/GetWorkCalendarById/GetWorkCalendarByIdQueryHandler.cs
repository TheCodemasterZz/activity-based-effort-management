using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.WorkCalendars.Dtos;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.WorkCalendars;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.WorkCalendars.Queries.GetWorkCalendarById;

public sealed class GetWorkCalendarByIdQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetWorkCalendarByIdQuery, WorkCalendarDetailDto>
{
    public async Task<WorkCalendarDetailDto> Handle(
        GetWorkCalendarByIdQuery request, CancellationToken cancellationToken)
    {
        var calendar = await db.WorkCalendars
            .AsNoTracking()
            .Include(c => c.Days)
            .FirstOrDefaultAsync(c => c.Id == request.WorkCalendarId, cancellationToken)
            ?? throw new NotFoundException(nameof(WorkCalendar), request.WorkCalendarId);

        return calendar.Adapt<WorkCalendarDetailDto>();
    }
}
