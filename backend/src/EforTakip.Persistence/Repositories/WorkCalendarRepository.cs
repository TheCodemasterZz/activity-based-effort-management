using EforTakip.Application.WorkCalendars;
using EforTakip.Domain.WorkCalendars;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Persistence.Repositories;

public sealed class WorkCalendarRepository(EforTakipDbContext context)
    : RepositoryBase<WorkCalendar>(context), IWorkCalendarRepository
{
    public override async Task<WorkCalendar?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await Context.WorkCalendars
            .Include(c => c.Days)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
}
