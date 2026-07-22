using EforTakip.Domain.Common;
using EforTakip.Domain.Exceptions;

namespace EforTakip.Domain.Directories;

public sealed class Directory : Entity, IAggregateRoot
{
    public string Name { get; private set; } = default!;
    public DirectorySource Source { get; private set; }
    public string? DirectoryType { get; private set; }
    public string? Hostname { get; private set; }
    public int Port { get; private set; }
    public bool UseSsl { get; private set; }
    public string? BindUsername { get; private set; }
    public string? BindPasswordEncrypted { get; private set; }
    public string? BaseDn { get; private set; }
    public string? AdditionalUserDn { get; private set; }
    public string? AdditionalGroupDn { get; private set; }
    public DirectoryPermission Permission { get; private set; }
    public string? UserObjectClass { get; private set; }
    public string? UserObjectFilter { get; private set; }
    public string? UsernameAttribute { get; private set; }
    public string? UsernameRdnAttribute { get; private set; }
    public string? FirstNameAttribute { get; private set; }
    public string? LastNameAttribute { get; private set; }
    public string? DisplayNameAttribute { get; private set; }
    public string? EmailAttribute { get; private set; }
    public string? UniqueIdAttribute { get; private set; }
    public SyncScheduleKind SyncSchedule { get; private set; }
    public bool IsActive { get; private set; }
    public int SortOrder { get; private set; }
    public DateTime? LastSyncedUtc { get; private set; }

    private Directory()
    {
        // EF Core
    }

    public static Directory CreateInternal(string name, int sortOrder)
    {
        ValidateName(name);

        return new Directory
        {
            Name = name.Trim(),
            Source = DirectorySource.Internal,
            Permission = DirectoryPermission.ReadWrite,
            SyncSchedule = SyncScheduleKind.Off,
            IsActive = true,
            SortOrder = sortOrder
        };
    }

    public static Directory CreateActiveDirectory(
        string name, string directoryType, string hostname, int port, bool useSsl,
        string bindUsername, string bindPasswordEncrypted, string baseDn,
        string? additionalUserDn, string? additionalGroupDn, DirectoryPermission permission,
        string userObjectClass, string userObjectFilter, string usernameAttribute,
        string usernameRdnAttribute, string firstNameAttribute, string lastNameAttribute,
        string displayNameAttribute, string emailAttribute, string uniqueIdAttribute,
        SyncScheduleKind syncSchedule, int sortOrder)
    {
        ValidateName(name);
        ValidatePort(port);
        if (string.IsNullOrWhiteSpace(hostname))
            throw new BusinessRuleValidationException("Sunucu adresi (hostname) zorunludur.");
        if (string.IsNullOrWhiteSpace(baseDn))
            throw new BusinessRuleValidationException("Base DN zorunludur.");

        var directory = new Directory
        {
            Name = name.Trim(),
            Source = DirectorySource.ActiveDirectory,
            IsActive = true,
            SortOrder = sortOrder
        };
        directory.ApplyActiveDirectorySettings(
            directoryType, hostname, port, useSsl, bindUsername, bindPasswordEncrypted,
            baseDn, additionalUserDn, additionalGroupDn, permission, userObjectClass,
            userObjectFilter, usernameAttribute, usernameRdnAttribute, firstNameAttribute,
            lastNameAttribute, displayNameAttribute, emailAttribute, uniqueIdAttribute, syncSchedule);
        return directory;
    }

    public void UpdateActiveDirectorySettings(
        string name, string directoryType, string hostname, int port, bool useSsl,
        string bindUsername, string? bindPasswordEncrypted, string baseDn,
        string? additionalUserDn, string? additionalGroupDn, DirectoryPermission permission,
        string userObjectClass, string userObjectFilter, string usernameAttribute,
        string usernameRdnAttribute, string firstNameAttribute, string lastNameAttribute,
        string displayNameAttribute, string emailAttribute, string uniqueIdAttribute,
        SyncScheduleKind syncSchedule)
    {
        if (Source != DirectorySource.ActiveDirectory)
            throw new BusinessRuleValidationException("Yalnızca Active Directory dizinlerinin bağlantı ayarları güncellenebilir.");

        ValidateName(name);
        ValidatePort(port);
        if (string.IsNullOrWhiteSpace(hostname))
            throw new BusinessRuleValidationException("Sunucu adresi (hostname) zorunludur.");
        if (string.IsNullOrWhiteSpace(baseDn))
            throw new BusinessRuleValidationException("Base DN zorunludur.");

        Name = name.Trim();
        // Boş şifre gelirse mevcut şifre korunur (formda şifre boş bırakılabilsin diye).
        var passwordToUse = string.IsNullOrWhiteSpace(bindPasswordEncrypted)
            ? BindPasswordEncrypted!
            : bindPasswordEncrypted;

        ApplyActiveDirectorySettings(
            directoryType, hostname, port, useSsl, bindUsername, passwordToUse,
            baseDn, additionalUserDn, additionalGroupDn, permission, userObjectClass,
            userObjectFilter, usernameAttribute, usernameRdnAttribute, firstNameAttribute,
            lastNameAttribute, displayNameAttribute, emailAttribute, uniqueIdAttribute, syncSchedule);
    }

    public void Rename(string name)
    {
        ValidateName(name);
        Name = name.Trim();
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public void MarkSynced(DateTime syncedUtc) => LastSyncedUtc = syncedUtc;

    /// <summary>Zamanlanmış senkronizasyonun bu dizin için çalışma zamanının gelip gelmediğini söyler.</summary>
    public bool IsSyncDue(DateTime nowUtc)
    {
        if (!IsActive || Source != DirectorySource.ActiveDirectory)
            return false;

        var interval = SyncSchedule switch
        {
            SyncScheduleKind.Hourly => TimeSpan.FromHours(1),
            SyncScheduleKind.Daily => TimeSpan.FromDays(1),
            SyncScheduleKind.Weekly => TimeSpan.FromDays(7),
            _ => TimeSpan.Zero
        };

        if (interval == TimeSpan.Zero)
            return false;

        return LastSyncedUtc is null || nowUtc - LastSyncedUtc.Value >= interval;
    }

    private void ApplyActiveDirectorySettings(
        string directoryType, string hostname, int port, bool useSsl, string bindUsername,
        string bindPasswordEncrypted, string baseDn, string? additionalUserDn,
        string? additionalGroupDn, DirectoryPermission permission, string userObjectClass,
        string userObjectFilter, string usernameAttribute, string usernameRdnAttribute,
        string firstNameAttribute, string lastNameAttribute, string displayNameAttribute,
        string emailAttribute, string uniqueIdAttribute, SyncScheduleKind syncSchedule)
    {
        DirectoryType = directoryType;
        Hostname = hostname.Trim();
        Port = port;
        UseSsl = useSsl;
        BindUsername = bindUsername;
        BindPasswordEncrypted = bindPasswordEncrypted;
        BaseDn = baseDn.Trim();
        AdditionalUserDn = additionalUserDn;
        AdditionalGroupDn = additionalGroupDn;
        Permission = permission;
        UserObjectClass = userObjectClass;
        UserObjectFilter = userObjectFilter;
        UsernameAttribute = usernameAttribute;
        UsernameRdnAttribute = usernameRdnAttribute;
        FirstNameAttribute = firstNameAttribute;
        LastNameAttribute = lastNameAttribute;
        DisplayNameAttribute = displayNameAttribute;
        EmailAttribute = emailAttribute;
        UniqueIdAttribute = uniqueIdAttribute;
        SyncSchedule = syncSchedule;
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessRuleValidationException("Dizin adı boş olamaz.");
    }

    private static void ValidatePort(int port)
    {
        if (port is <= 0 or > 65535)
            throw new BusinessRuleValidationException("Port 1-65535 aralığında olmalıdır.");
    }
}
