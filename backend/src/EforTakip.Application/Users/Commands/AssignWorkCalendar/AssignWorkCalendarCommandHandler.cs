using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.Users.Commands.AssignWorkCalendar;

public sealed class AssignWorkCalendarCommandHandler(
    IApplicationDbContext db,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AssignWorkCalendarCommand>
{
    public async Task Handle(AssignWorkCalendarCommand request, CancellationToken cancellationToken)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        user.AssignWorkCalendar(request.WorkCalendarId);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
