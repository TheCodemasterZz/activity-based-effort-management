using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Roles.Dtos;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.Roles.Queries.GetRoles;

public sealed class GetRolesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetRolesQuery, IReadOnlyCollection<RoleDto>>
{
    public async Task<IReadOnlyCollection<RoleDto>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
        => await db.Roles
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .ProjectToType<RoleDto>()
            .ToListAsync(cancellationToken);
}
