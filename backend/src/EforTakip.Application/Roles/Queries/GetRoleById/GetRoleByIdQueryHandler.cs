using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Roles.Dtos;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.Roles;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.Roles.Queries.GetRoleById;

public sealed class GetRoleByIdQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetRoleByIdQuery, RoleDetailDto>
{
    public async Task<RoleDetailDto> Handle(GetRoleByIdQuery request, CancellationToken cancellationToken)
    {
        var role = await db.Roles
            .AsNoTracking()
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken)
            ?? throw new NotFoundException(nameof(Role), request.RoleId);

        var assignedUsers = await (
            from userRole in db.UserRoles.AsNoTracking()
            join user in db.Users.AsNoTracking() on userRole.UserId equals user.Id
            where userRole.RoleId == request.RoleId
            orderby user.Username
            select new RoleAssignedUserDto
            {
                Id = user.Id,
                Username = user.Username,
                DisplayName = user.DisplayName
            }).ToListAsync(cancellationToken);

        return new RoleDetailDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            IsSystemAdmin = role.IsSystemAdmin,
            Permissions = role.Permissions.Select(p => p.PermissionKey).ToList(),
            AssignedUsers = assignedUsers
        };
    }
}
