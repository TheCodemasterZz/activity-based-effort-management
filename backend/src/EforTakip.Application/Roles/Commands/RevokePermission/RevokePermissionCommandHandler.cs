using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.Roles;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.Roles.Commands.RevokePermission;

public sealed class RevokePermissionCommandHandler(IApplicationDbContext db, IUnitOfWork unitOfWork)
    : IRequestHandler<RevokePermissionCommand>
{
    public async Task Handle(RevokePermissionCommand request, CancellationToken cancellationToken)
    {
        var role = await db.Roles
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken)
            ?? throw new NotFoundException(nameof(Role), request.RoleId);

        role.RevokePermission(request.PermissionKey);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
