using EforTakip.Domain.Common;
using EforTakip.Domain.Exceptions;

namespace EforTakip.Domain.Roles;

public sealed class RolePermission : Entity
{
    public Guid RoleId { get; private set; }
    public string PermissionKey { get; private set; } = default!;

    private RolePermission()
    {
        // EF Core
    }

    public static RolePermission Create(Guid roleId, string permissionKey)
    {
        if (roleId == Guid.Empty)
            throw new BusinessRuleValidationException("İzin bir role bağlı olmalıdır.");
        if (string.IsNullOrWhiteSpace(permissionKey))
            throw new BusinessRuleValidationException("İzin anahtarı boş olamaz.");

        return new RolePermission
        {
            RoleId = roleId,
            PermissionKey = permissionKey.Trim()
        };
    }
}
