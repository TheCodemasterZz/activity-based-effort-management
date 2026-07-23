using Microsoft.AspNetCore.Authorization;

namespace EforTakip.Api.Authorization;

/// <summary>
/// Bir controller action'ının belirli bir izni gerektirdiğini işaretler. Policy adı
/// "Permission:" öneki + izin anahtarıyla oluşturulur; PermissionPolicyProvider bu policy'yi
/// çalışma zamanında dinamik olarak PermissionRequirement'a çevirir — Program.cs'de her izin
/// için elle policy tanımlamaya gerek yoktur.
/// </summary>
public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "Permission:";

    public RequirePermissionAttribute(string permissionKey)
    {
        Policy = PolicyPrefix + permissionKey;
    }
}
