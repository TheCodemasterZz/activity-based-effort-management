using Microsoft.AspNetCore.Authorization;

namespace EforTakip.Api.Authorization;

public sealed class PermissionRequirement(string permissionKey) : IAuthorizationRequirement
{
    public string PermissionKey { get; } = permissionKey;
}
