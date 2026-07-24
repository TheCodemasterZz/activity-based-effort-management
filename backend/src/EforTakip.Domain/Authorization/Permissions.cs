using System.Reflection;

namespace EforTakip.Domain.Authorization;

/// <summary>
/// İzin kataloğu veritabanında değil burada, kodda yaşar. Yeni bir feature eklerken yapılması
/// gereken: buraya bir sabit + [PermissionLabel("Türkçe başlık")] eklemek ve ilgili controller
/// action'ına [RequirePermission(Permissions.Modül.İzin)] koymak — migration/seed gerekmez.
/// Ham anahtar (ör. "directory:manage") hiçbir yerde kullanıcıya çıplak gösterilmez; UI her zaman
/// PermissionDescriptor.Label'i kullanır. Bir role "modül:*" verilirse o moduldeki mevcut ve
/// gelecekteki tüm izinler otomatik kapsanır (bkz. Role.HasPermission).
/// </summary>
public static class Permissions
{
    [PermissionLabel("Roller ve İzinler")]
    public static class Role
    {
        [PermissionLabel("Rolleri Listele")]
        public const string Read = "role:read";

        [PermissionLabel("Rol Oluştur / Düzenle / Sil, İzin ve Kullanıcı Ata")]
        public const string Manage = "role:manage";
    }

    [PermissionLabel("Kullanıcı Hesapları")]
    public static class User
    {
        [PermissionLabel("Kullanıcı Hesaplarını Görüntüle")]
        public const string Read = "user:read";

        [PermissionLabel("Kullanıcı Hesabı Oluştur, Şifre Sıfırla")]
        public const string Manage = "user:manage";
    }

    [PermissionLabel("Active Directory")]
    public static class Directory
    {
        [PermissionLabel("Active Directory Bağlantılarını Görüntüle")]
        public const string Read = "directory:read";

        [PermissionLabel("Active Directory Bağlantısı Ekle / Senkronize Et / Alan Eşlemesi Yönet")]
        public const string Manage = "directory:manage";
    }

    [PermissionLabel("İzinler")]
    public static class Leave
    {
        [PermissionLabel("İzin Kayıtlarını Görüntüle")]
        public const string Read = "leave:read";

        [PermissionLabel("İzin Kaydı Ekle / Sil")]
        public const string Manage = "leave:manage";
    }

    [PermissionLabel("Müşteriler")]
    public static class Customer
    {
        [PermissionLabel("Müşterileri Görüntüle")]
        public const string Read = "customer:read";

        [PermissionLabel("Müşteri Ekle / Düzenle")]
        public const string Manage = "customer:manage";
    }

    [PermissionLabel("Projeler")]
    public static class Project
    {
        [PermissionLabel("Projeleri Görüntüle")]
        public const string Read = "project:read";

        [PermissionLabel("Proje Oluştur")]
        public const string Create = "project:create";

        [PermissionLabel("Proje Güncelle (sağlık durumu, atamalar, görev/risk/sorun dahil)")]
        public const string Update = "project:update";

        [PermissionLabel("Proje Sil")]
        public const string Delete = "project:delete";
    }

    [PermissionLabel("Efor Kayıtları")]
    public static class WorkLog
    {
        [PermissionLabel("Efor Kayıtlarını Görüntüle")]
        public const string Read = "worklog:read";

        [PermissionLabel("Efor Kaydı Gir / Düzenle")]
        public const string Create = "worklog:create";

        [PermissionLabel("Efor Kaydını Sil")]
        public const string Delete = "worklog:delete";

        [PermissionLabel("Efor Kayıtlarını Onayla (Haftalık Kilitleme)")]
        public const string Approve = "worklog:approve";
    }

    [PermissionLabel("Value Stream'ler")]
    public static class ValueStream
    {
        [PermissionLabel("Value Stream'leri Görüntüle")]
        public const string Read = "valuestream:read";

        [PermissionLabel("Value Stream Oluştur / Aşama ve Aktivite Ata")]
        public const string Manage = "valuestream:manage";
    }

    [PermissionLabel("Aktivite Kataloğu")]
    public static class Activity
    {
        [PermissionLabel("Aktivite Kataloğunu Görüntüle")]
        public const string Read = "activity:read";

        [PermissionLabel("Aktivite Ekle")]
        public const string Manage = "activity:manage";
    }

    [PermissionLabel("Takvimler")]
    public static class Calendar
    {
        [PermissionLabel("Takvimleri Görüntüle (Mesai Takvimi, Resmi Tatiller)")]
        public const string Read = "calendar:read";

        [PermissionLabel("Mesai Takvimi / Resmi Tatil Ekle")]
        public const string Manage = "calendar:manage";
    }

    [PermissionLabel("Sistem Ayarları")]
    public static class Settings
    {
        [PermissionLabel("Sistem Ayarlarını Yönet (Güven Skoru vb.)")]
        public const string Manage = "settings:manage";
    }

    public static IReadOnlyCollection<string> All { get; } = CollectDescriptors().Select(d => d.Key).ToList().AsReadOnly();

    public static IReadOnlyCollection<PermissionDescriptor> AllDescriptors { get; } = CollectDescriptors();

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

    private static IReadOnlyCollection<PermissionDescriptor> CollectDescriptors()
    {
        var descriptors = new List<PermissionDescriptor>();

        foreach (var nestedType in typeof(Permissions).GetNestedTypes(BindingFlags.Public))
        {
            var moduleLabel = nestedType.GetCustomAttribute<PermissionLabelAttribute>()?.Label ?? nestedType.Name;

            foreach (var field in nestedType.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (field.FieldType != typeof(string) || !field.IsLiteral)
                    continue;

                var key = (string)field.GetRawConstantValue()!;
                var label = field.GetCustomAttribute<PermissionLabelAttribute>()?.Label ?? key;
                descriptors.Add(new PermissionDescriptor(key, moduleLabel, label));
            }
        }

        return descriptors.AsReadOnly();
    }
}
