using System.Reflection;

namespace EforTakip.Domain.Authorization;

/// <summary>
/// İzin kataloğu veritabanında değil burada, kodda yaşar. Yeni bir feature eklerken tek yapılması
/// gereken buraya bir sabit eklemek ve ilgili controller action'ına
/// [RequirePermission(Permissions.Modül.İzin)] koymaktır — migration/seed gerekmez. Bir role
/// "modül:*" verilirse o modüldeki (bu dosyada tanımlı) mevcut ve gelecekteki tüm izinler
/// otomatik kapsanır (bkz. Role.HasPermission).
/// </summary>
public static class Permissions
{
    public static class Role
    {
        public const string Read = "role:read";
        public const string Manage = "role:manage";
    }

    public static class User
    {
        public const string Read = "user:read";
        public const string Manage = "user:manage";
    }

    public static class Directory
    {
        public const string Manage = "directory:manage";
    }

    public static class Employee
    {
        public const string Read = "employee:read";
        public const string Manage = "employee:manage";
    }

    public static class Project
    {
        public const string Read = "project:read";
        public const string Create = "project:create";
        public const string Update = "project:update";
        public const string Delete = "project:delete";
    }

    public static class WorkLog
    {
        public const string Read = "worklog:read";
        public const string Create = "worklog:create";
        public const string Delete = "worklog:delete";
        public const string Approve = "worklog:approve";
    }

    public static class ValueStream
    {
        public const string Read = "valuestream:read";
        public const string Manage = "valuestream:manage";
    }

    public static class Activity
    {
        public const string Read = "activity:read";
        public const string Manage = "activity:manage";
    }

    public static class Calendar
    {
        public const string Manage = "calendar:manage";
    }

    public static IReadOnlyCollection<string> All { get; } = CollectAll();

    /// <summary>"modül:*" biçiminde bir wildcard mı, yoksa katalogdaki tam bir izin anahtarı mı — geçerli bir grant girdisi mi kontrol eder.</summary>
    public static bool IsValidGrant(string permissionKey)
    {
        if (string.IsNullOrWhiteSpace(permissionKey))
            return false;

        if (All.Contains(permissionKey))
            return true;

        if (permissionKey.EndsWith(":*", StringComparison.Ordinal))
        {
            var modulePrefix = permissionKey[..^2];
            return All.Any(key => key.StartsWith(modulePrefix + ":", StringComparison.Ordinal));
        }

        return false;
    }

    private static IReadOnlyCollection<string> CollectAll()
    {
        var keys = new List<string>();

        foreach (var nestedType in typeof(Permissions).GetNestedTypes(BindingFlags.Public))
        {
            foreach (var field in nestedType.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (field.FieldType == typeof(string) && field.IsLiteral)
                    keys.Add((string)field.GetRawConstantValue()!);
            }
        }

        return keys.AsReadOnly();
    }
}
