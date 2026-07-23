using MediatR;

namespace EforTakip.Application.Roles.Commands.AssignRoleToUser;

public sealed record AssignRoleToUserCommand(Guid UserId, Guid RoleId) : IRequest;
