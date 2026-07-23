using MediatR;

namespace EforTakip.Application.Roles.Commands.RemoveRoleFromUser;

public sealed record RemoveRoleFromUserCommand(Guid UserId, Guid RoleId) : IRequest;
