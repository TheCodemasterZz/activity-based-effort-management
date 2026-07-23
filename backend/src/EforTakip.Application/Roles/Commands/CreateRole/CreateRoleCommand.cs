using MediatR;

namespace EforTakip.Application.Roles.Commands.CreateRole;

public sealed record CreateRoleCommand(string Name, string? Description) : IRequest<Guid>;
