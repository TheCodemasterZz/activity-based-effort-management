using Microsoft.AspNetCore.Authorization;

namespace EforTakip.Api.Authorization;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.HasClaim("is_system_admin", "true"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        foreach (var grantedKey in context.User.FindAll("permission").Select(c => c.Value))
        {
            if (grantedKey == requirement.PermissionKey)
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            if (grantedKey.EndsWith(":*", StringComparison.Ordinal))
            {
                var modulePrefix = grantedKey[..^1];
                if (requirement.PermissionKey.StartsWith(modulePrefix, StringComparison.Ordinal))
                {
                    context.Succeed(requirement);
                    return Task.CompletedTask;
                }
            }
        }

        return Task.CompletedTask;
    }
}
