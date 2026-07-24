using EforTakip.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.Users.Commands.BulkAssignWorkCalendar;

public sealed class BulkAssignWorkCalendarCommandHandler(
    IApplicationDbContext db,
    IUnitOfWork unitOfWork)
    : IRequestHandler<BulkAssignWorkCalendarCommand>
{
    public async Task Handle(BulkAssignWorkCalendarCommand request, CancellationToken cancellationToken)
    {
        var users = await db.Users
            .Where(u => request.UserIds.Contains(u.Id))
            .ToListAsync(cancellationToken);

        foreach (var user in users)
            user.AssignWorkCalendar(request.WorkCalendarId);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
