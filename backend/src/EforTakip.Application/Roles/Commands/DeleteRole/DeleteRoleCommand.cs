using MediatR;

namespace EforTakip.Application.Roles.Commands.DeleteRole;

public sealed record DeleteRoleCommand(Guid Id) : IRequest;
