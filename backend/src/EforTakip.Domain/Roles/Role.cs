using EforTakip.Domain.Common;
using EforTakip.Domain.Exceptions;

namespace EforTakip.Domain.Roles;

public sealed class Role : Entity, IAggregateRoot
{
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }

    /// <summary>true ise HasPermission her zaman true döner — hiçbir izin kaydına ihtiyaç duymaz.</summary>
    public bool IsSystemAdmin { get; private set; }

    private readonly List<RolePermission> _permissions = [];
    public IReadOnlyCollection<RolePermission> Permissions => _permissions.AsReadOnly();

    private Role()
    {
        // EF Core
    }

    public static Role Create(string name, string? description, bool isSystemAdmin)
    {
        ValidateName(name);

        return new Role
        {
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            IsSystemAdmin = isSystemAdmin
        };
    }

    public void Rename(string name)
    {
        ValidateName(name);
        Name = name.Trim();
    }

    public void UpdateDescription(string? description)
        => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    /// <summary>
    /// Zaten çağıranın _permissions'ı önceden (Include ile) yüklemiş olması gerekir; aksi halde
    /// yinelenen kontrol her zaman "yok" der ve tekrarlanan satır eklenmeye çalışılabilir (bkz.
    /// DirectoryUser.SetAttribute ile aynı desen). Yeni oluşturulan varlığı geri döner — çağıran
    /// taraf context'e açıkça eklemelidir.
    /// </summary>
    public RolePermission? GrantPermission(string permissionKey)
    {
        if (_permissions.Any(p => p.PermissionKey == permissionKey))
            return null;

        var created = RolePermission.Create(Id, permissionKey);
        _permissions.Add(created);
        return created;
    }

    public void RevokePermission(string permissionKey)
    {
        var existing = _permissions.FirstOrDefault(p => p.PermissionKey == permissionKey);
        if (existing is not null)
            _permissions.Remove(existing);
    }

    public bool HasPermission(string permissionKey)
    {
        if (IsSystemAdmin)
            return true;

        foreach (var granted in _permissions)
        {
            if (granted.PermissionKey == permissionKey)
                return true;

            if (granted.PermissionKey.EndsWith(":*", StringComparison.Ordinal))
            {
                var modulePrefix = granted.PermissionKey[..^1];
                if (permissionKey.StartsWith(modulePrefix, StringComparison.Ordinal))
                    return true;
            }
        }

        return false;
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessRuleValidationException("Rol adı boş olamaz.");
    }
}
