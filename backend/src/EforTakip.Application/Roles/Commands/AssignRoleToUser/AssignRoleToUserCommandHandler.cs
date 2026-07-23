using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Directories;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.Roles;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.Roles.Commands.AssignRoleToUser;

public sealed class AssignRoleToUserCommandHandler(IApplicationDbContext db, IUnitOfWork unitOfWork)
    : IRequestHandler<AssignRoleToUserCommand>
{
    public async Task Handle(AssignRoleToUserCommand request, CancellationToken cancellationToken)
    {
        var user = await db.DirectoryUsers
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(DirectoryUser), request.UserId);

        var roleExists = await db.Roles.AnyAsync(r => r.Id == request.RoleId, cancellationToken);
        if (!roleExists)
            throw new NotFoundException(nameof(Role), request.RoleId);

        var created = user.AssignRole(request.RoleId);
        if (created is not null)
            db.DirectoryUserRoles.Add(created);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
