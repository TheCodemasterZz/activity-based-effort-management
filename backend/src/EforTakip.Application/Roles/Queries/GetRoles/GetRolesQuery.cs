using EforTakip.Application.Roles.Dtos;
using MediatR;

namespace EforTakip.Application.Roles.Queries.GetRoles;

public sealed record GetRolesQuery : IRequest<IReadOnlyCollection<RoleDto>>;
