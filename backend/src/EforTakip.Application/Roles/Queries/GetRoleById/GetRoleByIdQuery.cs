using EforTakip.Application.Roles.Dtos;
using MediatR;

namespace EforTakip.Application.Roles.Queries.GetRoleById;

public sealed record GetRoleByIdQuery(Guid RoleId) : IRequest<RoleDetailDto>;
