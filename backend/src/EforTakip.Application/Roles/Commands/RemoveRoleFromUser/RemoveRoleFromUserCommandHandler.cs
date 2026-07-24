using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Directories;
using EforTakip.Domain.Users;
using EforTakip.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.Roles.Commands.RemoveRoleFromUser;

public sealed class RemoveRoleFromUserCommandHandler(IApplicationDbContext db, IUnitOfWork unitOfWork)
    : IRequestHandler<RemoveRoleFromUserCommand>
{
    public async Task Handle(RemoveRoleFromUserCommand request, CancellationToken cancellationToken)
    {
        var user = await db.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        user.RemoveRole(request.RoleId);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
