using MediatR;

namespace EforTakip.Application.Roles.Commands.RevokePermission;

public sealed record RevokePermissionCommand(Guid RoleId, string PermissionKey) : IRequest;
