using MediatR;

namespace EforTakip.Application.Roles.Commands.UpdateRole;

public sealed record UpdateRoleCommand(Guid Id, string Name, string? Description) : IRequest;
