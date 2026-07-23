using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace EforTakip.Api.Authorization;

/// <summary>
/// "Permission:" önekiyle başlayan policy adlarını PermissionRequirement'a çevirir; başka her
/// policy adı için varsayılan sağlayıcıya (fallback policy dahil) devreder.
/// </summary>
public sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallbackProvider = new(options);

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallbackProvider.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallbackProvider.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(RequirePermissionAttribute.PolicyPrefix, StringComparison.Ordinal))
        {
            var permissionKey = policyName[RequirePermissionAttribute.PolicyPrefix.Length..];
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(permissionKey))
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _fallbackProvider.GetPolicyAsync(policyName);
    }
}
