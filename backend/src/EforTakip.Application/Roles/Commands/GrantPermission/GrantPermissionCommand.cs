using MediatR;

namespace EforTakip.Application.Roles.Commands.GrantPermission;

public sealed record GrantPermissionCommand(Guid RoleId, string PermissionKey) : IRequest;
