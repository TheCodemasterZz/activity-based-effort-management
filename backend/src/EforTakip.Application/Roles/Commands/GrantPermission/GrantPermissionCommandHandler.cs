using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Authorization;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.Roles;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.Roles.Commands.GrantPermission;

public sealed class GrantPermissionCommandHandler(IApplicationDbContext db, IUnitOfWork unitOfWork)
    : IRequestHandler<GrantPermissionCommand>
{
    public async Task Handle(GrantPermissionCommand request, CancellationToken cancellationToken)
    {
        var role = await db.Roles
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken)
            ?? throw new NotFoundException(nameof(Role), request.RoleId);

        if (!Permissions.IsValidGrant(request.PermissionKey))
            throw new BusinessRuleValidationException($"'{request.PermissionKey}' geçerli bir izin anahtarı değil.");

        var created = role.GrantPermission(request.PermissionKey);
        if (created is not null)
            db.RolePermissions.Add(created);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
