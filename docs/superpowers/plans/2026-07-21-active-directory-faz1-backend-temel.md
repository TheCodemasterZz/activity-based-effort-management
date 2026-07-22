# Active Directory Entegrasyonu — Faz 1: Backend Temel (Dizin Yönetimi) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** AD/dizin tanımlarının ve global attribute eşlemelerinin domain modelini, kalıcılığını ve CRUD API'sini kurmak.

**Architecture:** Mevcut Clean Architecture (Domain → Application → Persistence → API) katmanlarına `Directories` modülü eklenir. Domain'de `Directory`, `DirectoryUser`, `DirectoryAttributeMapping` entity'leri; Application'da MediatR command/query'ler; Persistence'da EF Core configuration + migration; API'da controller'lar. Bu faz LDAP bağlantısı, senkronizasyon ve auth içermez — sadece konfigürasyon verisinin yönetimi.

**Tech Stack:** .NET 8, EF Core (Npgsql + InMemory), MediatR, FluentValidation, Mapster, xUnit, FluentAssertions, NSubstitute.

## Global Constraints

- Domain entity'leri `sealed`, private parametresiz ctor + static `Create` factory, `Entity` base'inden türer (`EforTakip.Domain.Common.Entity`).
- Aggregate root'lar `IAggregateRoot` implement eder.
- İş kuralı ihlalinde `BusinessRuleValidationException`, bulunamayanda `NotFoundException` fırlatılır.
- Command/Query = `sealed record` veya `PaginationParams`'tan türeyen `sealed class`; Handler = primary constructor'lı `sealed class`.
- Validator = `AbstractValidator<T>`, Türkçe hata mesajları.
- EF Configuration = `IEntityTypeConfiguration<T>`, `ToTable(...)`, `HasKey(...)`.
- Controller = `[ApiController]`, `[ApiVersion("1.0")]`, `[Route("api/v{version:apiVersion}/[controller]")]`, `ISender mediator` primary ctor.
- Bind şifresi asla düz metin dönmez/loglanmaz. DTO'larda şifre alanı yer almaz.
- Tüm kullanıcıya dönük metinler Türkçe.

---

## Dosya Yapısı

**Domain (`backend/src/EforTakip.Domain/Directories/`):**
- `DirectorySource.cs` — enum (Internal, ActiveDirectory)
- `DirectoryPermission.cs` — enum (ReadOnly, ReadOnlyLocalGroups, ReadWrite)
- `SyncScheduleKind.cs` — enum (Off, Hourly, Daily, Weekly)
- `Directory.cs` — aggregate root
- `DirectoryUser.cs` — aggregate root
- `DirectoryAttributeMapping.cs` — aggregate root

**Application (`backend/src/EforTakip.Application/Directories/`):**
- `Commands/CreateDirectory/` — Command, Handler, Validator
- `Commands/UpdateDirectory/` — Command, Handler, Validator
- `Commands/DeleteDirectory/` — Command, Handler
- `Commands/CreateAttributeMapping/` — Command, Handler, Validator
- `Commands/UpdateAttributeMapping/` — Command, Handler, Validator
- `Commands/DeleteAttributeMapping/` — Command, Handler
- `Queries/GetDirectories/` — Query, Handler
- `Queries/GetDirectoryById/` — Query, Handler
- `Queries/GetAttributeMappings/` — Query, Handler
- `Dtos/DirectoryDto.cs`, `Dtos/DirectoryAttributeMappingDto.cs`

**Persistence (`backend/src/EforTakip.Persistence/`):**
- `Configurations/DirectoryConfiguration.cs`
- `Configurations/DirectoryUserConfiguration.cs`
- `Configurations/DirectoryAttributeMappingConfiguration.cs`
- `EforTakipDbContext.cs` (modify — DbSet ekle)
- `DependencyInjection.cs` (modify — repository kayıtları)
- `EforTakip.Application/Common/Interfaces/IApplicationDbContext.cs` (modify — DbSet ekle)

**API (`backend/src/EforTakip.Api/Controllers/v1/`):**
- `DirectoriesController.cs`
- `DirectoryAttributeMappingsController.cs`

**Tests:**
- `backend/tests/EforTakip.Domain.Tests/Directories/DirectoryTests.cs`
- `backend/tests/EforTakip.Domain.Tests/Directories/DirectoryUserTests.cs`
- `backend/tests/EforTakip.Domain.Tests/Directories/DirectoryAttributeMappingTests.cs`
- `backend/tests/EforTakip.Application.Tests/Directories/Commands/CreateDirectoryCommandHandlerTests.cs`
- `backend/tests/EforTakip.Application.Tests/Directories/Commands/CreateDirectoryCommandValidatorTests.cs`
- `backend/tests/EforTakip.Application.Tests/Directories/Commands/CreateAttributeMappingCommandHandlerTests.cs`

---

## Task 1: Directory enum'ları

**Files:**
- Create: `backend/src/EforTakip.Domain/Directories/DirectorySource.cs`
- Create: `backend/src/EforTakip.Domain/Directories/DirectoryPermission.cs`
- Create: `backend/src/EforTakip.Domain/Directories/SyncScheduleKind.cs`

**Interfaces:**
- Produces: `DirectorySource { Internal, ActiveDirectory }`, `DirectoryPermission { ReadOnly, ReadOnlyLocalGroups, ReadWrite }`, `SyncScheduleKind { Off, Hourly, Daily, Weekly }` — namespace `EforTakip.Domain.Directories`.

- [ ] **Step 1: Enum dosyalarını oluştur**

`DirectorySource.cs`:
```csharp
namespace EforTakip.Domain.Directories;

public enum DirectorySource
{
    Internal = 0,
    ActiveDirectory = 1
}
```

`DirectoryPermission.cs`:
```csharp
namespace EforTakip.Domain.Directories;

public enum DirectoryPermission
{
    ReadOnly = 0,
    ReadOnlyLocalGroups = 1,
    ReadWrite = 2
}
```

`SyncScheduleKind.cs`:
```csharp
namespace EforTakip.Domain.Directories;

public enum SyncScheduleKind
{
    Off = 0,
    Hourly = 1,
    Daily = 2,
    Weekly = 3
}
```

- [ ] **Step 2: Derle**

Run: `dotnet build backend/src/EforTakip.Domain/EforTakip.Domain.csproj`
Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add backend/src/EforTakip.Domain/Directories/
git commit -m "feat: add directory domain enums"
```

---

## Task 2: Directory aggregate

**Files:**
- Create: `backend/src/EforTakip.Domain/Directories/Directory.cs`
- Test: `backend/tests/EforTakip.Domain.Tests/Directories/DirectoryTests.cs`

**Interfaces:**
- Consumes: `DirectorySource`, `DirectoryPermission`, `SyncScheduleKind` (Task 1); `Entity`, `IAggregateRoot`, `BusinessRuleValidationException`.
- Produces:
  - `Directory.CreateActiveDirectory(string name, string directoryType, string hostname, int port, bool useSsl, string bindUsername, string bindPasswordEncrypted, string baseDn, string? additionalUserDn, string? additionalGroupDn, DirectoryPermission permission, string userObjectClass, string userObjectFilter, string usernameAttribute, string usernameRdnAttribute, string firstNameAttribute, string lastNameAttribute, string displayNameAttribute, string emailAttribute, string uniqueIdAttribute, SyncScheduleKind syncSchedule, int sortOrder)` → `Directory`
  - `Directory.CreateInternal(string name, int sortOrder)` → `Directory`
  - Properties (private set): `Name, Source, DirectoryType, Hostname, Port, UseSsl, BindUsername, BindPasswordEncrypted, BaseDn, AdditionalUserDn, AdditionalGroupDn, Permission, UserObjectClass, UserObjectFilter, UsernameAttribute, UsernameRdnAttribute, FirstNameAttribute, LastNameAttribute, DisplayNameAttribute, EmailAttribute, UniqueIdAttribute, SyncSchedule, IsActive, SortOrder`
  - `void UpdateActiveDirectorySettings(...)` (aynı AD parametreleri, `bindPasswordEncrypted` opsiyonel: null ise mevcut korunur)
  - `void Rename(string name)`, `void Activate()`, `void Deactivate()`

- [ ] **Step 1: Testi yaz**

`backend/tests/EforTakip.Domain.Tests/Directories/DirectoryTests.cs`:
```csharp
using EforTakip.Domain.Directories;
using EforTakip.Domain.Exceptions;
using FluentAssertions;

namespace EforTakip.Domain.Tests.Directories;

public class DirectoryTests
{
    private static Directory CreateValidAd() =>
        Directory.CreateActiveDirectory(
            name: "Active Directory server",
            directoryType: "Microsoft Active Directory",
            hostname: "kizilay.local",
            port: 389,
            useSsl: false,
            bindUsername: "jira_user@kizilay.org.tr",
            bindPasswordEncrypted: "ENC(secret)",
            baseDn: "DC=kizilay,DC=local",
            additionalUserDn: null,
            additionalGroupDn: null,
            permission: DirectoryPermission.ReadOnlyLocalGroups,
            userObjectClass: "user",
            userObjectFilter: "(&(objectCategory=Person)(sAMAccountName=*))",
            usernameAttribute: "sAMAccountName",
            usernameRdnAttribute: "cn",
            firstNameAttribute: "givenName",
            lastNameAttribute: "sn",
            displayNameAttribute: "displayName",
            emailAttribute: "mail",
            uniqueIdAttribute: "objectGUID",
            syncSchedule: SyncScheduleKind.Daily,
            sortOrder: 0);

    [Fact]
    public void CreateActiveDirectory_WithValidData_CreatesDirectory()
    {
        var directory = CreateValidAd();

        directory.Name.Should().Be("Active Directory server");
        directory.Source.Should().Be(DirectorySource.ActiveDirectory);
        directory.Hostname.Should().Be("kizilay.local");
        directory.Port.Should().Be(389);
        directory.Permission.Should().Be(DirectoryPermission.ReadOnlyLocalGroups);
        directory.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CreateActiveDirectory_WithEmptyName_Throws(string? name)
    {
        var act = () => Directory.CreateActiveDirectory(
            name!, "Microsoft Active Directory", "kizilay.local", 389, false,
            "u", "ENC(x)", "DC=kizilay,DC=local", null, null,
            DirectoryPermission.ReadOnly, "user", "(x)", "sAMAccountName", "cn",
            "givenName", "sn", "displayName", "mail", "objectGUID", SyncScheduleKind.Off, 0);

        act.Should().Throw<BusinessRuleValidationException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(70000)]
    public void CreateActiveDirectory_WithInvalidPort_Throws(int port)
    {
        var act = () => Directory.CreateActiveDirectory(
            "Ad", "Microsoft Active Directory", "kizilay.local", port, false,
            "u", "ENC(x)", "DC=kizilay,DC=local", null, null,
            DirectoryPermission.ReadOnly, "user", "(x)", "sAMAccountName", "cn",
            "givenName", "sn", "displayName", "mail", "objectGUID", SyncScheduleKind.Off, 0);

        act.Should().Throw<BusinessRuleValidationException>();
    }

    [Fact]
    public void CreateInternal_CreatesInternalDirectory()
    {
        var directory = Directory.CreateInternal("Internal Users", 0);

        directory.Source.Should().Be(DirectorySource.Internal);
        directory.Name.Should().Be("Internal Users");
        directory.IsActive.Should().BeTrue();
    }

    [Fact]
    public void UpdateActiveDirectorySettings_WithNullPassword_KeepsExistingPassword()
    {
        var directory = CreateValidAd();

        directory.UpdateActiveDirectorySettings(
            "Yeni Ad", "Microsoft Active Directory", "yeni.local", 636, true,
            "u2", null, "DC=yeni,DC=local", null, null,
            DirectoryPermission.ReadWrite, "user", "(x)", "sAMAccountName", "cn",
            "givenName", "sn", "displayName", "mail", "objectGUID", SyncScheduleKind.Hourly);

        directory.Name.Should().Be("Yeni Ad");
        directory.Port.Should().Be(636);
        directory.BindPasswordEncrypted.Should().Be("ENC(secret)");
    }

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        var directory = CreateValidAd();

        directory.Deactivate();

        directory.IsActive.Should().BeFalse();
    }
}
```

- [ ] **Step 2: Testi çalıştır, başarısız olduğunu doğrula**

Run: `dotnet test backend/tests/EforTakip.Domain.Tests/EforTakip.Domain.Tests.csproj --filter DirectoryTests`
Expected: FAIL — `Directory` tipi yok / derlenmiyor.

- [ ] **Step 3: Directory entity'sini yaz**

`backend/src/EforTakip.Domain/Directories/Directory.cs`:
```csharp
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
```

- [ ] **Step 4: Testi çalıştır, geçtiğini doğrula**

Run: `dotnet test backend/tests/EforTakip.Domain.Tests/EforTakip.Domain.Tests.csproj --filter DirectoryTests`
Expected: PASS (tüm testler).

- [ ] **Step 5: Commit**

```bash
git add backend/src/EforTakip.Domain/Directories/Directory.cs backend/tests/EforTakip.Domain.Tests/Directories/DirectoryTests.cs
git commit -m "feat: add Directory aggregate root"
```

---

## Task 3: DirectoryUser aggregate

**Files:**
- Create: `backend/src/EforTakip.Domain/Directories/DirectoryUser.cs`
- Test: `backend/tests/EforTakip.Domain.Tests/Directories/DirectoryUserTests.cs`

**Interfaces:**
- Consumes: `DirectorySource` (Task 1); `Entity`, `IAggregateRoot`, `BusinessRuleValidationException`.
- Produces:
  - `DirectoryUser.CreateFromActiveDirectory(Guid directoryId, string username, string? firstName, string? lastName, string? displayName, string? email, string objectGuid)` → `DirectoryUser`
  - `DirectoryUser.CreateInternal(Guid directoryId, string username, string? firstName, string? lastName, string? displayName, string? email, string passwordHash)` → `DirectoryUser`
  - Properties (private set): `DirectoryId, Source, Username, FirstName, LastName, DisplayName, Email, ObjectGuid, PasswordHash, IsActive, LastSyncedUtc`
  - `void UpdateFromSync(string? firstName, string? lastName, string? displayName, string? email, DateTime syncedUtc)`
  - `void Deactivate()`, `void Activate()`

- [ ] **Step 1: Testi yaz**

`backend/tests/EforTakip.Domain.Tests/Directories/DirectoryUserTests.cs`:
```csharp
using EforTakip.Domain.Directories;
using EforTakip.Domain.Exceptions;
using FluentAssertions;

namespace EforTakip.Domain.Tests.Directories;

public class DirectoryUserTests
{
    [Fact]
    public void CreateFromActiveDirectory_WithValidData_CreatesUser()
    {
        var directoryId = Guid.NewGuid();

        var user = DirectoryUser.CreateFromActiveDirectory(
            directoryId, "serkan.gultepe", "Serkan", "Gültepe",
            "Serkan Gültepe", "serkan@kizilay.org.tr", "guid-123");

        user.DirectoryId.Should().Be(directoryId);
        user.Source.Should().Be(DirectorySource.ActiveDirectory);
        user.Username.Should().Be("serkan.gultepe");
        user.ObjectGuid.Should().Be("guid-123");
        user.PasswordHash.Should().BeNull();
        user.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CreateFromActiveDirectory_WithEmptyUsername_Throws(string? username)
    {
        var act = () => DirectoryUser.CreateFromActiveDirectory(
            Guid.NewGuid(), username!, "a", "b", "c", "d", "guid");

        act.Should().Throw<BusinessRuleValidationException>();
    }

    [Fact]
    public void CreateInternal_WithValidData_CreatesUserWithPasswordHash()
    {
        var user = DirectoryUser.CreateInternal(
            Guid.NewGuid(), "sanal.kullanici", "Sanal", "Kullanıcı",
            "Sanal Kullanıcı", null, "HASHED");

        user.Source.Should().Be(DirectorySource.Internal);
        user.PasswordHash.Should().Be("HASHED");
        user.ObjectGuid.Should().BeNull();
    }

    [Fact]
    public void UpdateFromSync_UpdatesFieldsAndLastSynced()
    {
        var user = DirectoryUser.CreateFromActiveDirectory(
            Guid.NewGuid(), "serkan.gultepe", "Serkan", "Eski", "x", "eski@x.com", "guid");
        var syncTime = DateTime.UtcNow;

        user.UpdateFromSync("Serkan", "Yeni", "Serkan Yeni", "yeni@x.com", syncTime);

        user.LastName.Should().Be("Yeni");
        user.Email.Should().Be("yeni@x.com");
        user.LastSyncedUtc.Should().Be(syncTime);
    }
}
```

- [ ] **Step 2: Testi çalıştır, başarısız olduğunu doğrula**

Run: `dotnet test backend/tests/EforTakip.Domain.Tests/EforTakip.Domain.Tests.csproj --filter DirectoryUserTests`
Expected: FAIL — `DirectoryUser` tipi yok.

- [ ] **Step 3: DirectoryUser entity'sini yaz**

`backend/src/EforTakip.Domain/Directories/DirectoryUser.cs`:
```csharp
using EforTakip.Domain.Common;
using EforTakip.Domain.Exceptions;

namespace EforTakip.Domain.Directories;

public sealed class DirectoryUser : Entity, IAggregateRoot
{
    public Guid DirectoryId { get; private set; }
    public DirectorySource Source { get; private set; }
    public string Username { get; private set; } = default!;
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public string? DisplayName { get; private set; }
    public string? Email { get; private set; }
    public string? ObjectGuid { get; private set; }
    public string? PasswordHash { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime? LastSyncedUtc { get; private set; }

    private DirectoryUser()
    {
        // EF Core
    }

    public static DirectoryUser CreateFromActiveDirectory(
        Guid directoryId, string username, string? firstName, string? lastName,
        string? displayName, string? email, string objectGuid)
    {
        ValidateDirectoryId(directoryId);
        ValidateUsername(username);
        if (string.IsNullOrWhiteSpace(objectGuid))
            throw new BusinessRuleValidationException("AD kullanıcısının benzersiz kimliği (ObjectGuid) zorunludur.");

        return new DirectoryUser
        {
            DirectoryId = directoryId,
            Source = DirectorySource.ActiveDirectory,
            Username = username.Trim(),
            FirstName = firstName,
            LastName = lastName,
            DisplayName = displayName,
            Email = email,
            ObjectGuid = objectGuid,
            IsActive = true,
            LastSyncedUtc = DateTime.UtcNow
        };
    }

    public static DirectoryUser CreateInternal(
        Guid directoryId, string username, string? firstName, string? lastName,
        string? displayName, string? email, string passwordHash)
    {
        ValidateDirectoryId(directoryId);
        ValidateUsername(username);
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new BusinessRuleValidationException("Internal kullanıcı için şifre zorunludur.");

        return new DirectoryUser
        {
            DirectoryId = directoryId,
            Source = DirectorySource.Internal,
            Username = username.Trim(),
            FirstName = firstName,
            LastName = lastName,
            DisplayName = displayName,
            Email = email,
            PasswordHash = passwordHash,
            IsActive = true
        };
    }

    public void UpdateFromSync(
        string? firstName, string? lastName, string? displayName, string? email, DateTime syncedUtc)
    {
        FirstName = firstName;
        LastName = lastName;
        DisplayName = displayName;
        Email = email;
        LastSyncedUtc = syncedUtc;
        IsActive = true;
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    private static void ValidateDirectoryId(Guid directoryId)
    {
        if (directoryId == Guid.Empty)
            throw new BusinessRuleValidationException("Kullanıcı bir dizine bağlı olmalıdır.");
    }

    private static void ValidateUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new BusinessRuleValidationException("Kullanıcı adı boş olamaz.");
    }
}
```

- [ ] **Step 4: Testi çalıştır, geçtiğini doğrula**

Run: `dotnet test backend/tests/EforTakip.Domain.Tests/EforTakip.Domain.Tests.csproj --filter DirectoryUserTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add backend/src/EforTakip.Domain/Directories/DirectoryUser.cs backend/tests/EforTakip.Domain.Tests/Directories/DirectoryUserTests.cs
git commit -m "feat: add DirectoryUser aggregate root"
```

---

## Task 4: DirectoryAttributeMapping aggregate

**Files:**
- Create: `backend/src/EforTakip.Domain/Directories/DirectoryAttributeMapping.cs`
- Test: `backend/tests/EforTakip.Domain.Tests/Directories/DirectoryAttributeMappingTests.cs`

**Interfaces:**
- Consumes: `Entity`, `IAggregateRoot`, `BusinessRuleValidationException`.
- Produces:
  - `DirectoryAttributeMapping.Create(string adAttributeName, string systemFieldName, string fieldType, bool isSynced, int sortOrder)` → `DirectoryAttributeMapping`
  - Properties (private set): `AdAttributeName, SystemFieldName, FieldType, IsSynced, SortOrder`
  - `void Update(string adAttributeName, string systemFieldName, string fieldType, bool isSynced, int sortOrder)`

- [ ] **Step 1: Testi yaz**

`backend/tests/EforTakip.Domain.Tests/Directories/DirectoryAttributeMappingTests.cs`:
```csharp
using EforTakip.Domain.Directories;
using EforTakip.Domain.Exceptions;
using FluentAssertions;

namespace EforTakip.Domain.Tests.Directories;

public class DirectoryAttributeMappingTests
{
    [Fact]
    public void Create_WithValidData_CreatesMapping()
    {
        var mapping = DirectoryAttributeMapping.Create("company", "Kurum", "text", true, 0);

        mapping.AdAttributeName.Should().Be("company");
        mapping.SystemFieldName.Should().Be("Kurum");
        mapping.FieldType.Should().Be("text");
        mapping.IsSynced.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "Kurum")]
    [InlineData("company", "")]
    [InlineData("   ", "Kurum")]
    public void Create_WithEmptyNames_Throws(string adName, string systemName)
    {
        var act = () => DirectoryAttributeMapping.Create(adName, systemName, "text", true, 0);

        act.Should().Throw<BusinessRuleValidationException>();
    }

    [Fact]
    public void Update_ChangesValues()
    {
        var mapping = DirectoryAttributeMapping.Create("company", "Kurum", "text", true, 0);

        mapping.Update("department", "Departman", "text", false, 1);

        mapping.AdAttributeName.Should().Be("department");
        mapping.SystemFieldName.Should().Be("Departman");
        mapping.IsSynced.Should().BeFalse();
        mapping.SortOrder.Should().Be(1);
    }
}
```

- [ ] **Step 2: Testi çalıştır, başarısız olduğunu doğrula**

Run: `dotnet test backend/tests/EforTakip.Domain.Tests/EforTakip.Domain.Tests.csproj --filter DirectoryAttributeMappingTests`
Expected: FAIL — tip yok.

- [ ] **Step 3: Entity'yi yaz**

`backend/src/EforTakip.Domain/Directories/DirectoryAttributeMapping.cs`:
```csharp
using EforTakip.Domain.Common;
using EforTakip.Domain.Exceptions;

namespace EforTakip.Domain.Directories;

public sealed class DirectoryAttributeMapping : Entity, IAggregateRoot
{
    public string AdAttributeName { get; private set; } = default!;
    public string SystemFieldName { get; private set; } = default!;
    public string FieldType { get; private set; } = default!;
    public bool IsSynced { get; private set; }
    public int SortOrder { get; private set; }

    private DirectoryAttributeMapping()
    {
        // EF Core
    }

    public static DirectoryAttributeMapping Create(
        string adAttributeName, string systemFieldName, string fieldType, bool isSynced, int sortOrder)
    {
        Validate(adAttributeName, systemFieldName, fieldType);

        return new DirectoryAttributeMapping
        {
            AdAttributeName = adAttributeName.Trim(),
            SystemFieldName = systemFieldName.Trim(),
            FieldType = fieldType.Trim(),
            IsSynced = isSynced,
            SortOrder = sortOrder
        };
    }

    public void Update(
        string adAttributeName, string systemFieldName, string fieldType, bool isSynced, int sortOrder)
    {
        Validate(adAttributeName, systemFieldName, fieldType);

        AdAttributeName = adAttributeName.Trim();
        SystemFieldName = systemFieldName.Trim();
        FieldType = fieldType.Trim();
        IsSynced = isSynced;
        SortOrder = sortOrder;
    }

    private static void Validate(string adAttributeName, string systemFieldName, string fieldType)
    {
        if (string.IsNullOrWhiteSpace(adAttributeName))
            throw new BusinessRuleValidationException("AD alan adı boş olamaz.");
        if (string.IsNullOrWhiteSpace(systemFieldName))
            throw new BusinessRuleValidationException("Sistem alan adı boş olamaz.");
        if (string.IsNullOrWhiteSpace(fieldType))
            throw new BusinessRuleValidationException("Alan tipi boş olamaz.");
    }
}
```

- [ ] **Step 4: Testi çalıştır, geçtiğini doğrula**

Run: `dotnet test backend/tests/EforTakip.Domain.Tests/EforTakip.Domain.Tests.csproj --filter DirectoryAttributeMappingTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add backend/src/EforTakip.Domain/Directories/DirectoryAttributeMapping.cs backend/tests/EforTakip.Domain.Tests/Directories/DirectoryAttributeMappingTests.cs
git commit -m "feat: add DirectoryAttributeMapping aggregate root"
```

---

## Task 5: Persistence — EF configurations, DbContext, migration

**Files:**
- Create: `backend/src/EforTakip.Persistence/Configurations/DirectoryConfiguration.cs`
- Create: `backend/src/EforTakip.Persistence/Configurations/DirectoryUserConfiguration.cs`
- Create: `backend/src/EforTakip.Persistence/Configurations/DirectoryAttributeMappingConfiguration.cs`
- Modify: `backend/src/EforTakip.Persistence/EforTakipDbContext.cs`
- Modify: `backend/src/EforTakip.Application/Common/Interfaces/IApplicationDbContext.cs`
- Modify: `backend/src/EforTakip.Persistence/DependencyInjection.cs`

**Interfaces:**
- Consumes: `Directory`, `DirectoryUser`, `DirectoryAttributeMapping` (Tasks 2-4).
- Produces: `IApplicationDbContext.Directories`, `.DirectoryUsers`, `.DirectoryAttributeMappings` (tümü `DbSet<T>`); `IRepository<Directory>`, `IRepository<DirectoryUser>`, `IRepository<DirectoryAttributeMapping>` DI kayıtları.

- [ ] **Step 1: DirectoryConfiguration yaz**

`backend/src/EforTakip.Persistence/Configurations/DirectoryConfiguration.cs`:
```csharp
using EforTakip.Domain.Directories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EforTakip.Persistence.Configurations;

public sealed class DirectoryConfiguration : IEntityTypeConfiguration<Directory>
{
    public void Configure(EntityTypeBuilder<Directory> builder)
    {
        builder.ToTable("Directories");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Name).IsRequired().HasMaxLength(200);
        builder.Property(d => d.Source).IsRequired().HasConversion<string>().HasMaxLength(30);
        builder.Property(d => d.Permission).IsRequired().HasConversion<string>().HasMaxLength(30);
        builder.Property(d => d.SyncSchedule).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(d => d.DirectoryType).HasMaxLength(100);
        builder.Property(d => d.Hostname).HasMaxLength(255);
        builder.Property(d => d.BindUsername).HasMaxLength(255);
        builder.Property(d => d.BindPasswordEncrypted).HasMaxLength(1024);
        builder.Property(d => d.BaseDn).HasMaxLength(512);
        builder.Property(d => d.AdditionalUserDn).HasMaxLength(512);
        builder.Property(d => d.AdditionalGroupDn).HasMaxLength(512);
        builder.Property(d => d.UserObjectClass).HasMaxLength(100);
        builder.Property(d => d.UserObjectFilter).HasMaxLength(1024);
        builder.Property(d => d.UsernameAttribute).HasMaxLength(100);
        builder.Property(d => d.UsernameRdnAttribute).HasMaxLength(100);
        builder.Property(d => d.FirstNameAttribute).HasMaxLength(100);
        builder.Property(d => d.LastNameAttribute).HasMaxLength(100);
        builder.Property(d => d.DisplayNameAttribute).HasMaxLength(100);
        builder.Property(d => d.EmailAttribute).HasMaxLength(100);
        builder.Property(d => d.UniqueIdAttribute).HasMaxLength(100);
    }
}
```

- [ ] **Step 2: DirectoryUserConfiguration yaz**

`backend/src/EforTakip.Persistence/Configurations/DirectoryUserConfiguration.cs`:
```csharp
using EforTakip.Domain.Directories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EforTakip.Persistence.Configurations;

public sealed class DirectoryUserConfiguration : IEntityTypeConfiguration<DirectoryUser>
{
    public void Configure(EntityTypeBuilder<DirectoryUser> builder)
    {
        builder.ToTable("DirectoryUsers");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Username).IsRequired().HasMaxLength(150);
        builder.Property(u => u.Source).IsRequired().HasConversion<string>().HasMaxLength(30);
        builder.Property(u => u.FirstName).HasMaxLength(150);
        builder.Property(u => u.LastName).HasMaxLength(150);
        builder.Property(u => u.DisplayName).HasMaxLength(300);
        builder.Property(u => u.Email).HasMaxLength(255);
        builder.Property(u => u.ObjectGuid).HasMaxLength(100);
        builder.Property(u => u.PasswordHash).HasMaxLength(500);

        builder.HasIndex(u => u.Username).IsUnique();

        builder.HasOne<Directory>()
            .WithMany()
            .HasForeignKey(u => u.DirectoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

- [ ] **Step 3: DirectoryAttributeMappingConfiguration yaz**

`backend/src/EforTakip.Persistence/Configurations/DirectoryAttributeMappingConfiguration.cs`:
```csharp
using EforTakip.Domain.Directories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EforTakip.Persistence.Configurations;

public sealed class DirectoryAttributeMappingConfiguration : IEntityTypeConfiguration<DirectoryAttributeMapping>
{
    public void Configure(EntityTypeBuilder<DirectoryAttributeMapping> builder)
    {
        builder.ToTable("DirectoryAttributeMappings");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.AdAttributeName).IsRequired().HasMaxLength(150);
        builder.Property(m => m.SystemFieldName).IsRequired().HasMaxLength(150);
        builder.Property(m => m.FieldType).IsRequired().HasMaxLength(50);
    }
}
```

- [ ] **Step 4: IApplicationDbContext'e DbSet'leri ekle**

`backend/src/EforTakip.Application/Common/Interfaces/IApplicationDbContext.cs` — `using EforTakip.Domain.Directories;` ekle ve interface içine ekle:
```csharp
    DbSet<Directory> Directories { get; }

    DbSet<DirectoryUser> DirectoryUsers { get; }

    DbSet<DirectoryAttributeMapping> DirectoryAttributeMappings { get; }
```

- [ ] **Step 5: EforTakipDbContext'e DbSet'leri ekle**

`backend/src/EforTakip.Persistence/EforTakipDbContext.cs` — `using EforTakip.Domain.Directories;` ekle ve diğer DbSet'lerin yanına ekle:
```csharp
    public DbSet<Directory> Directories => Set<Directory>();

    public DbSet<DirectoryUser> DirectoryUsers => Set<DirectoryUser>();

    public DbSet<DirectoryAttributeMapping> DirectoryAttributeMappings => Set<DirectoryAttributeMapping>();
```

- [ ] **Step 6: DependencyInjection'a repository kayıtları ekle**

`backend/src/EforTakip.Persistence/DependencyInjection.cs` — `using EforTakip.Domain.Directories;` ekle ve diğer `AddScoped` kayıtlarının yanına ekle:
```csharp
        services.AddScoped<IRepository<Directory>, RepositoryBase<Directory>>();
        services.AddScoped<IRepository<DirectoryUser>, RepositoryBase<DirectoryUser>>();
        services.AddScoped<IRepository<DirectoryAttributeMapping>, RepositoryBase<DirectoryAttributeMapping>>();
```

- [ ] **Step 7: Derle**

Run: `dotnet build backend/EforTakip.sln`
Expected: Build succeeded.

- [ ] **Step 8: Migration oluştur**

Run:
```bash
dotnet ef migrations add AddDirectories \
  --project backend/src/EforTakip.Persistence \
  --startup-project backend/src/EforTakip.Api
```
Expected: Migration dosyaları `backend/src/EforTakip.Persistence/Migrations/` altında oluşur. (Not: `dotnet ef` yoksa `dotnet tool install --global dotnet-ef` çalıştır.)

- [ ] **Step 9: Testleri çalıştır (regresyon kontrolü)**

Run: `dotnet test backend/EforTakip.sln`
Expected: PASS (mevcut + yeni domain testleri).

- [ ] **Step 10: Commit**

```bash
git add backend/src/EforTakip.Persistence/ backend/src/EforTakip.Application/Common/Interfaces/IApplicationDbContext.cs
git commit -m "feat: persist directory entities with EF Core configurations and migration"
```

---

## Task 6: CreateDirectory command

**Files:**
- Create: `backend/src/EforTakip.Application/Directories/Commands/CreateDirectory/CreateDirectoryCommand.cs`
- Create: `backend/src/EforTakip.Application/Directories/Commands/CreateDirectory/CreateDirectoryCommandHandler.cs`
- Create: `backend/src/EforTakip.Application/Directories/Commands/CreateDirectory/CreateDirectoryCommandValidator.cs`
- Test: `backend/tests/EforTakip.Application.Tests/Directories/Commands/CreateDirectoryCommandHandlerTests.cs`
- Test: `backend/tests/EforTakip.Application.Tests/Directories/Commands/CreateDirectoryCommandValidatorTests.cs`

**Interfaces:**
- Consumes: `IRepository<Directory>`, `IUnitOfWork`, `Directory.CreateActiveDirectory(...)`, `Directory.CreateInternal(...)`, `DirectoryPermission`, `SyncScheduleKind`, `DirectorySource`.
- Produces: `CreateDirectoryCommand` (record, `IRequest<Guid>`) with fields: `Name, Source, DirectoryType, Hostname, Port, UseSsl, BindUsername, BindPassword, BaseDn, AdditionalUserDn, AdditionalGroupDn, Permission, UserObjectClass, UserObjectFilter, UsernameAttribute, UsernameRdnAttribute, FirstNameAttribute, LastNameAttribute, DisplayNameAttribute, EmailAttribute, UniqueIdAttribute, SyncSchedule, SortOrder`. **Not:** `BindPassword` düz metin gelir; handler bu fazda geçici olarak olduğu gibi saklar (Faz 3'te `SettingsEncryptor` ile şifrelenecek — bkz. handler yorumu).

- [ ] **Step 1: Command'ı yaz**

`CreateDirectoryCommand.cs`:
```csharp
using EforTakip.Domain.Directories;
using MediatR;

namespace EforTakip.Application.Directories.Commands.CreateDirectory;

public sealed record CreateDirectoryCommand(
    string Name,
    DirectorySource Source,
    string? DirectoryType,
    string? Hostname,
    int Port,
    bool UseSsl,
    string? BindUsername,
    string? BindPassword,
    string? BaseDn,
    string? AdditionalUserDn,
    string? AdditionalGroupDn,
    DirectoryPermission Permission,
    string? UserObjectClass,
    string? UserObjectFilter,
    string? UsernameAttribute,
    string? UsernameRdnAttribute,
    string? FirstNameAttribute,
    string? LastNameAttribute,
    string? DisplayNameAttribute,
    string? EmailAttribute,
    string? UniqueIdAttribute,
    SyncScheduleKind SyncSchedule,
    int SortOrder) : IRequest<Guid>;
```

- [ ] **Step 2: Validator testini yaz**

`CreateDirectoryCommandValidatorTests.cs`:
```csharp
using EforTakip.Application.Directories.Commands.CreateDirectory;
using EforTakip.Domain.Directories;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace EforTakip.Application.Tests.Directories.Commands;

public class CreateDirectoryCommandValidatorTests
{
    private readonly CreateDirectoryCommandValidator _validator = new();

    private static CreateDirectoryCommand AdCommand(string name = "Ad", string? hostname = "kizilay.local", int port = 389) =>
        new(name, DirectorySource.ActiveDirectory, "Microsoft Active Directory", hostname, port, false,
            "u", "p", "DC=kizilay,DC=local", null, null, DirectoryPermission.ReadOnly,
            "user", "(x)", "sAMAccountName", "cn", "givenName", "sn", "displayName", "mail",
            "objectGUID", SyncScheduleKind.Off, 0);

    [Fact]
    public void ValidAdCommand_Passes()
    {
        _validator.TestValidate(AdCommand()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyName_Fails()
    {
        _validator.TestValidate(AdCommand(name: "")).ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void ActiveDirectoryWithoutHostname_Fails()
    {
        _validator.TestValidate(AdCommand(hostname: null)).ShouldHaveValidationErrorFor(x => x.Hostname);
    }

    [Fact]
    public void InvalidPort_Fails()
    {
        _validator.TestValidate(AdCommand(port: 0)).ShouldHaveValidationErrorFor(x => x.Port);
    }
}
```

- [ ] **Step 3: Validator'ı yaz**

`CreateDirectoryCommandValidator.cs`:
```csharp
using EforTakip.Domain.Directories;
using FluentValidation;

namespace EforTakip.Application.Directories.Commands.CreateDirectory;

public sealed class CreateDirectoryCommandValidator : AbstractValidator<CreateDirectoryCommand>
{
    public CreateDirectoryCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Dizin adı zorunludur.")
            .MaximumLength(200).WithMessage("Dizin adı en fazla 200 karakter olabilir.");

        When(x => x.Source == DirectorySource.ActiveDirectory, () =>
        {
            RuleFor(x => x.Hostname)
                .NotEmpty().WithMessage("Sunucu adresi (hostname) zorunludur.");
            RuleFor(x => x.BaseDn)
                .NotEmpty().WithMessage("Base DN zorunludur.");
            RuleFor(x => x.Port)
                .InclusiveBetween(1, 65535).WithMessage("Port 1-65535 aralığında olmalıdır.");
            RuleFor(x => x.UsernameAttribute)
                .NotEmpty().WithMessage("Kullanıcı adı attribute'u zorunludur.");
        });
    }
}
```

- [ ] **Step 4: Handler testini yaz**

`CreateDirectoryCommandHandlerTests.cs`:
```csharp
using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Directories.Commands.CreateDirectory;
using EforTakip.Domain.Directories;
using FluentAssertions;
using NSubstitute;

namespace EforTakip.Application.Tests.Directories.Commands;

public class CreateDirectoryCommandHandlerTests
{
    private readonly IRepository<Directory> _repository = Substitute.For<IRepository<Directory>>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task Handle_ActiveDirectory_CreatesAndPersists()
    {
        var handler = new CreateDirectoryCommandHandler(_repository, _unitOfWork);
        var command = new CreateDirectoryCommand(
            "Active Directory server", DirectorySource.ActiveDirectory, "Microsoft Active Directory",
            "kizilay.local", 389, false, "u", "p", "DC=kizilay,DC=local", null, null,
            DirectoryPermission.ReadOnlyLocalGroups, "user", "(x)", "sAMAccountName", "cn",
            "givenName", "sn", "displayName", "mail", "objectGUID", SyncScheduleKind.Daily, 0);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeEmpty();
        await _repository.Received(1).AddAsync(
            Arg.Is<Directory>(d => d.Name == "Active Directory server" && d.Source == DirectorySource.ActiveDirectory),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Internal_CreatesInternalDirectory()
    {
        var handler = new CreateDirectoryCommandHandler(_repository, _unitOfWork);
        var command = new CreateDirectoryCommand(
            "Internal Users", DirectorySource.Internal, null, null, 0, false, null, null, null, null, null,
            DirectoryPermission.ReadWrite, null, null, null, null, null, null, null, null, null,
            SyncScheduleKind.Off, 0);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeEmpty();
        await _repository.Received(1).AddAsync(
            Arg.Is<Directory>(d => d.Source == DirectorySource.Internal), Arg.Any<CancellationToken>());
    }
}
```

- [ ] **Step 5: Testleri çalıştır, başarısız olduğunu doğrula**

Run: `dotnet test backend/tests/EforTakip.Application.Tests/EforTakip.Application.Tests.csproj --filter CreateDirectory`
Expected: FAIL — handler yok.

- [ ] **Step 6: Handler'ı yaz**

`CreateDirectoryCommandHandler.cs`:
```csharp
using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Directories;
using MediatR;

namespace EforTakip.Application.Directories.Commands.CreateDirectory;

public sealed class CreateDirectoryCommandHandler(IRepository<Directory> repository, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateDirectoryCommand, Guid>
{
    public async Task<Guid> Handle(CreateDirectoryCommand request, CancellationToken cancellationToken)
    {
        // NOT: BindPassword bu fazda düz metin saklanır. Faz 3'te SettingsEncryptor ile
        // şifrelenmiş değer geçirilecek şekilde güncellenecek.
        var directory = request.Source == DirectorySource.Internal
            ? Directory.CreateInternal(request.Name, request.SortOrder)
            : Directory.CreateActiveDirectory(
                request.Name, request.DirectoryType!, request.Hostname!, request.Port, request.UseSsl,
                request.BindUsername!, request.BindPassword ?? string.Empty, request.BaseDn!,
                request.AdditionalUserDn, request.AdditionalGroupDn, request.Permission,
                request.UserObjectClass!, request.UserObjectFilter!, request.UsernameAttribute!,
                request.UsernameRdnAttribute!, request.FirstNameAttribute!, request.LastNameAttribute!,
                request.DisplayNameAttribute!, request.EmailAttribute!, request.UniqueIdAttribute!,
                request.SyncSchedule, request.SortOrder);

        await repository.AddAsync(directory, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return directory.Id;
    }
}
```

- [ ] **Step 7: Testleri çalıştır, geçtiğini doğrula**

Run: `dotnet test backend/tests/EforTakip.Application.Tests/EforTakip.Application.Tests.csproj --filter CreateDirectory`
Expected: PASS.

- [ ] **Step 8: Commit**

```bash
git add backend/src/EforTakip.Application/Directories/Commands/CreateDirectory/ backend/tests/EforTakip.Application.Tests/Directories/Commands/
git commit -m "feat: add CreateDirectory command"
```

---

## Task 7: UpdateDirectory ve DeleteDirectory commands

**Files:**
- Create: `backend/src/EforTakip.Application/Directories/Commands/UpdateDirectory/UpdateDirectoryCommand.cs`
- Create: `backend/src/EforTakip.Application/Directories/Commands/UpdateDirectory/UpdateDirectoryCommandHandler.cs`
- Create: `backend/src/EforTakip.Application/Directories/Commands/UpdateDirectory/UpdateDirectoryCommandValidator.cs`
- Create: `backend/src/EforTakip.Application/Directories/Commands/DeleteDirectory/DeleteDirectoryCommand.cs`
- Create: `backend/src/EforTakip.Application/Directories/Commands/DeleteDirectory/DeleteDirectoryCommandHandler.cs`
- Test: `backend/tests/EforTakip.Application.Tests/Directories/Commands/UpdateDirectoryCommandHandlerTests.cs`

**Interfaces:**
- Consumes: `IRepository<Directory>`, `IUnitOfWork`, `Directory.UpdateActiveDirectorySettings(...)`, `Directory.Rename(...)`, `NotFoundException`.
- Produces:
  - `UpdateDirectoryCommand` (record, `IRequest`) — `CreateDirectoryCommand` ile aynı alanlar + başta `Guid Id`. `BindPassword` null olabilir (mevcut korunur).
  - `DeleteDirectoryCommand(Guid Id)` (record, `IRequest`).

- [ ] **Step 1: UpdateDirectoryCommand yaz**

`UpdateDirectoryCommand.cs`:
```csharp
using EforTakip.Domain.Directories;
using MediatR;

namespace EforTakip.Application.Directories.Commands.UpdateDirectory;

public sealed record UpdateDirectoryCommand(
    Guid Id,
    string Name,
    DirectorySource Source,
    string? DirectoryType,
    string? Hostname,
    int Port,
    bool UseSsl,
    string? BindUsername,
    string? BindPassword,
    string? BaseDn,
    string? AdditionalUserDn,
    string? AdditionalGroupDn,
    DirectoryPermission Permission,
    string? UserObjectClass,
    string? UserObjectFilter,
    string? UsernameAttribute,
    string? UsernameRdnAttribute,
    string? FirstNameAttribute,
    string? LastNameAttribute,
    string? DisplayNameAttribute,
    string? EmailAttribute,
    string? UniqueIdAttribute,
    SyncScheduleKind SyncSchedule) : IRequest;
```

- [ ] **Step 2: UpdateDirectoryCommandValidator yaz**

`UpdateDirectoryCommandValidator.cs`:
```csharp
using EforTakip.Domain.Directories;
using FluentValidation;

namespace EforTakip.Application.Directories.Commands.UpdateDirectory;

public sealed class UpdateDirectoryCommandValidator : AbstractValidator<UpdateDirectoryCommand>
{
    public UpdateDirectoryCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Dizin adı zorunludur.")
            .MaximumLength(200).WithMessage("Dizin adı en fazla 200 karakter olabilir.");

        When(x => x.Source == DirectorySource.ActiveDirectory, () =>
        {
            RuleFor(x => x.Hostname).NotEmpty().WithMessage("Sunucu adresi (hostname) zorunludur.");
            RuleFor(x => x.BaseDn).NotEmpty().WithMessage("Base DN zorunludur.");
            RuleFor(x => x.Port).InclusiveBetween(1, 65535).WithMessage("Port 1-65535 aralığında olmalıdır.");
        });
    }
}
```

- [ ] **Step 3: Handler testini yaz**

`UpdateDirectoryCommandHandlerTests.cs`:
```csharp
using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Directories.Commands.UpdateDirectory;
using EforTakip.Domain.Directories;
using EforTakip.Domain.Exceptions;
using FluentAssertions;
using NSubstitute;

namespace EforTakip.Application.Tests.Directories.Commands;

public class UpdateDirectoryCommandHandlerTests
{
    private readonly IRepository<Directory> _repository = Substitute.For<IRepository<Directory>>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private static Directory ExistingAd() =>
        Directory.CreateActiveDirectory(
            "Eski", "Microsoft Active Directory", "eski.local", 389, false, "u", "ENC(x)",
            "DC=eski,DC=local", null, null, DirectoryPermission.ReadOnly, "user", "(x)",
            "sAMAccountName", "cn", "givenName", "sn", "displayName", "mail", "objectGUID",
            SyncScheduleKind.Off, 0);

    private static UpdateDirectoryCommand Command(Guid id) =>
        new(id, "Yeni Ad", DirectorySource.ActiveDirectory, "Microsoft Active Directory",
            "yeni.local", 636, true, "u2", null, "DC=yeni,DC=local", null, null,
            DirectoryPermission.ReadWrite, "user", "(x)", "sAMAccountName", "cn", "givenName",
            "sn", "displayName", "mail", "objectGUID", SyncScheduleKind.Hourly);

    [Fact]
    public async Task Handle_ExistingDirectory_Updates()
    {
        var directory = ExistingAd();
        _repository.GetByIdAsync(directory.Id, Arg.Any<CancellationToken>()).Returns(directory);
        var handler = new UpdateDirectoryCommandHandler(_repository, _unitOfWork);

        await handler.Handle(Command(directory.Id), CancellationToken.None);

        directory.Name.Should().Be("Yeni Ad");
        directory.Port.Should().Be(636);
        _repository.Received(1).Update(directory);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NonExisting_ThrowsNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Directory?)null);
        var handler = new UpdateDirectoryCommandHandler(_repository, _unitOfWork);

        var act = async () => await handler.Handle(Command(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
```

- [ ] **Step 4: Testi çalıştır, başarısız olduğunu doğrula**

Run: `dotnet test backend/tests/EforTakip.Application.Tests/EforTakip.Application.Tests.csproj --filter UpdateDirectory`
Expected: FAIL — handler yok.

- [ ] **Step 5: UpdateDirectory handler'ı yaz**

`UpdateDirectoryCommandHandler.cs`:
```csharp
using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Directories;
using EforTakip.Domain.Exceptions;
using MediatR;

namespace EforTakip.Application.Directories.Commands.UpdateDirectory;

public sealed class UpdateDirectoryCommandHandler(IRepository<Directory> repository, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateDirectoryCommand>
{
    public async Task Handle(UpdateDirectoryCommand request, CancellationToken cancellationToken)
    {
        var directory = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Directory), request.Id);

        if (request.Source == DirectorySource.Internal)
        {
            directory.Rename(request.Name);
        }
        else
        {
            directory.UpdateActiveDirectorySettings(
                request.Name, request.DirectoryType!, request.Hostname!, request.Port, request.UseSsl,
                request.BindUsername!, request.BindPassword, request.BaseDn!, request.AdditionalUserDn,
                request.AdditionalGroupDn, request.Permission, request.UserObjectClass!,
                request.UserObjectFilter!, request.UsernameAttribute!, request.UsernameRdnAttribute!,
                request.FirstNameAttribute!, request.LastNameAttribute!, request.DisplayNameAttribute!,
                request.EmailAttribute!, request.UniqueIdAttribute!, request.SyncSchedule);
        }

        repository.Update(directory);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
```

- [ ] **Step 6: DeleteDirectory command + handler yaz**

`DeleteDirectoryCommand.cs`:
```csharp
using MediatR;

namespace EforTakip.Application.Directories.Commands.DeleteDirectory;

public sealed record DeleteDirectoryCommand(Guid Id) : IRequest;
```

`DeleteDirectoryCommandHandler.cs`:
```csharp
using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Directories;
using EforTakip.Domain.Exceptions;
using MediatR;

namespace EforTakip.Application.Directories.Commands.DeleteDirectory;

public sealed class DeleteDirectoryCommandHandler(IRepository<Directory> repository, IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteDirectoryCommand>
{
    public async Task Handle(DeleteDirectoryCommand request, CancellationToken cancellationToken)
    {
        var directory = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Directory), request.Id);

        repository.Remove(directory);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
```

- [ ] **Step 7: Testleri çalıştır, geçtiğini doğrula**

Run: `dotnet test backend/tests/EforTakip.Application.Tests/EforTakip.Application.Tests.csproj --filter Directory`
Expected: PASS.

- [ ] **Step 8: Commit**

```bash
git add backend/src/EforTakip.Application/Directories/Commands/UpdateDirectory/ backend/src/EforTakip.Application/Directories/Commands/DeleteDirectory/ backend/tests/EforTakip.Application.Tests/Directories/Commands/UpdateDirectoryCommandHandlerTests.cs
git commit -m "feat: add UpdateDirectory and DeleteDirectory commands"
```

---

## Task 8: DirectoryDto + GetDirectories/GetDirectoryById queries

**Files:**
- Create: `backend/src/EforTakip.Application/Directories/Dtos/DirectoryDto.cs`
- Create: `backend/src/EforTakip.Application/Directories/Queries/GetDirectories/GetDirectoriesQuery.cs`
- Create: `backend/src/EforTakip.Application/Directories/Queries/GetDirectories/GetDirectoriesQueryHandler.cs`
- Create: `backend/src/EforTakip.Application/Directories/Queries/GetDirectoryById/GetDirectoryByIdQuery.cs`
- Create: `backend/src/EforTakip.Application/Directories/Queries/GetDirectoryById/GetDirectoryByIdQueryHandler.cs`

**Interfaces:**
- Consumes: `IApplicationDbContext.Directories`, `PagedResult<T>`, `PaginationParams`, `NotFoundException`, Mapster `ProjectToType`/`Adapt`.
- Produces:
  - `DirectoryDto` — `Directory`'nin şifre HARİÇ tüm alanları + `Id`.
  - `GetDirectoriesQuery : PaginationParams, IRequest<PagedResult<DirectoryDto>>` with `string? NameFilter`.
  - `GetDirectoryByIdQuery(Guid DirectoryId) : IRequest<DirectoryDto>`.

**Not:** `DirectoryDto` şifre alanı içermez (güvenlik). Mapster otomatik eşlemede `BindPasswordEncrypted` DTO'da olmadığı için atlanır.

- [ ] **Step 1: DirectoryDto yaz**

`DirectoryDto.cs`:
```csharp
using EforTakip.Domain.Directories;

namespace EforTakip.Application.Directories.Dtos;

public sealed class DirectoryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public DirectorySource Source { get; init; }
    public string? DirectoryType { get; init; }
    public string? Hostname { get; init; }
    public int Port { get; init; }
    public bool UseSsl { get; init; }
    public string? BindUsername { get; init; }
    public string? BaseDn { get; init; }
    public string? AdditionalUserDn { get; init; }
    public string? AdditionalGroupDn { get; init; }
    public DirectoryPermission Permission { get; init; }
    public string? UserObjectClass { get; init; }
    public string? UserObjectFilter { get; init; }
    public string? UsernameAttribute { get; init; }
    public string? UsernameRdnAttribute { get; init; }
    public string? FirstNameAttribute { get; init; }
    public string? LastNameAttribute { get; init; }
    public string? DisplayNameAttribute { get; init; }
    public string? EmailAttribute { get; init; }
    public string? UniqueIdAttribute { get; init; }
    public SyncScheduleKind SyncSchedule { get; init; }
    public bool IsActive { get; init; }
    public int SortOrder { get; init; }
}
```

- [ ] **Step 2: GetDirectoriesQuery + Handler yaz**

`GetDirectoriesQuery.cs`:
```csharp
using EforTakip.Application.Common.Models;
using EforTakip.Application.Directories.Dtos;
using MediatR;

namespace EforTakip.Application.Directories.Queries.GetDirectories;

public sealed class GetDirectoriesQuery : PaginationParams, IRequest<PagedResult<DirectoryDto>>
{
    public string? NameFilter { get; set; }
}
```

`GetDirectoriesQueryHandler.cs`:
```csharp
using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Common.Models;
using EforTakip.Application.Directories.Dtos;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.Directories.Queries.GetDirectories;

public sealed class GetDirectoriesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetDirectoriesQuery, PagedResult<DirectoryDto>>
{
    public async Task<PagedResult<DirectoryDto>> Handle(GetDirectoriesQuery request, CancellationToken cancellationToken)
    {
        var query = db.Directories.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.NameFilter))
        {
            var nameFilter = request.NameFilter.ToLower();
            query = query.Where(d => d.Name.ToLower().Contains(nameFilter));
        }

        query = query.OrderBy(d => d.SortOrder).ThenBy(d => d.Name);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ProjectToType<DirectoryDto>()
            .ToListAsync(cancellationToken);

        return new PagedResult<DirectoryDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
```

- [ ] **Step 3: GetDirectoryByIdQuery + Handler yaz**

`GetDirectoryByIdQuery.cs`:
```csharp
using EforTakip.Application.Directories.Dtos;
using MediatR;

namespace EforTakip.Application.Directories.Queries.GetDirectoryById;

public sealed record GetDirectoryByIdQuery(Guid DirectoryId) : IRequest<DirectoryDto>;
```

`GetDirectoryByIdQueryHandler.cs`:
```csharp
using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Directories.Dtos;
using EforTakip.Domain.Directories;
using EforTakip.Domain.Exceptions;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.Directories.Queries.GetDirectoryById;

public sealed class GetDirectoryByIdQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetDirectoryByIdQuery, DirectoryDto>
{
    public async Task<DirectoryDto> Handle(GetDirectoryByIdQuery request, CancellationToken cancellationToken)
    {
        var directory = await db.Directories
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == request.DirectoryId, cancellationToken)
            ?? throw new NotFoundException(nameof(Directory), request.DirectoryId);

        return directory.Adapt<DirectoryDto>();
    }
}
```

- [ ] **Step 4: Derle ve testleri çalıştır (regresyon)**

Run: `dotnet test backend/EforTakip.sln`
Expected: PASS. (Build başarılı, mevcut testler geçiyor.)

- [ ] **Step 5: Commit**

```bash
git add backend/src/EforTakip.Application/Directories/Dtos/ backend/src/EforTakip.Application/Directories/Queries/
git commit -m "feat: add directory queries and DTO"
```

---

## Task 9: AttributeMapping commands + query + DTO

**Files:**
- Create: `backend/src/EforTakip.Application/Directories/Dtos/DirectoryAttributeMappingDto.cs`
- Create: `backend/src/EforTakip.Application/Directories/Commands/CreateAttributeMapping/` (Command, Handler, Validator)
- Create: `backend/src/EforTakip.Application/Directories/Commands/UpdateAttributeMapping/` (Command, Handler, Validator)
- Create: `backend/src/EforTakip.Application/Directories/Commands/DeleteAttributeMapping/` (Command, Handler)
- Create: `backend/src/EforTakip.Application/Directories/Queries/GetAttributeMappings/` (Query, Handler)
- Test: `backend/tests/EforTakip.Application.Tests/Directories/Commands/CreateAttributeMappingCommandHandlerTests.cs`

**Interfaces:**
- Consumes: `IRepository<DirectoryAttributeMapping>`, `IUnitOfWork`, `IApplicationDbContext.DirectoryAttributeMappings`, `DirectoryAttributeMapping.Create/.Update`, `NotFoundException`.
- Produces:
  - `DirectoryAttributeMappingDto { Id, AdAttributeName, SystemFieldName, FieldType, IsSynced, SortOrder }`.
  - `CreateAttributeMappingCommand(string AdAttributeName, string SystemFieldName, string FieldType, bool IsSynced, int SortOrder) : IRequest<Guid>`.
  - `UpdateAttributeMappingCommand(Guid Id, string AdAttributeName, string SystemFieldName, string FieldType, bool IsSynced, int SortOrder) : IRequest`.
  - `DeleteAttributeMappingCommand(Guid Id) : IRequest`.
  - `GetAttributeMappingsQuery() : IRequest<IReadOnlyCollection<DirectoryAttributeMappingDto>>` (global liste, pagination'sız — az sayıda kayıt).

- [ ] **Step 1: DTO yaz**

`DirectoryAttributeMappingDto.cs`:
```csharp
namespace EforTakip.Application.Directories.Dtos;

public sealed class DirectoryAttributeMappingDto
{
    public Guid Id { get; init; }
    public string AdAttributeName { get; init; } = default!;
    public string SystemFieldName { get; init; } = default!;
    public string FieldType { get; init; } = default!;
    public bool IsSynced { get; init; }
    public int SortOrder { get; init; }
}
```

- [ ] **Step 2: Create command + validator yaz**

`CreateAttributeMapping/CreateAttributeMappingCommand.cs`:
```csharp
using MediatR;

namespace EforTakip.Application.Directories.Commands.CreateAttributeMapping;

public sealed record CreateAttributeMappingCommand(
    string AdAttributeName,
    string SystemFieldName,
    string FieldType,
    bool IsSynced,
    int SortOrder) : IRequest<Guid>;
```

`CreateAttributeMapping/CreateAttributeMappingCommandValidator.cs`:
```csharp
using FluentValidation;

namespace EforTakip.Application.Directories.Commands.CreateAttributeMapping;

public sealed class CreateAttributeMappingCommandValidator : AbstractValidator<CreateAttributeMappingCommand>
{
    public CreateAttributeMappingCommandValidator()
    {
        RuleFor(x => x.AdAttributeName)
            .NotEmpty().WithMessage("AD alan adı zorunludur.")
            .MaximumLength(150);
        RuleFor(x => x.SystemFieldName)
            .NotEmpty().WithMessage("Sistem alan adı zorunludur.")
            .MaximumLength(150);
        RuleFor(x => x.FieldType)
            .NotEmpty().WithMessage("Alan tipi zorunludur.")
            .MaximumLength(50);
    }
}
```

`CreateAttributeMapping/CreateAttributeMappingCommandHandler.cs`:
```csharp
using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Directories;
using MediatR;

namespace EforTakip.Application.Directories.Commands.CreateAttributeMapping;

public sealed class CreateAttributeMappingCommandHandler(
    IRepository<DirectoryAttributeMapping> repository, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateAttributeMappingCommand, Guid>
{
    public async Task<Guid> Handle(CreateAttributeMappingCommand request, CancellationToken cancellationToken)
    {
        var mapping = DirectoryAttributeMapping.Create(
            request.AdAttributeName, request.SystemFieldName, request.FieldType,
            request.IsSynced, request.SortOrder);

        await repository.AddAsync(mapping, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return mapping.Id;
    }
}
```

- [ ] **Step 3: Handler testini yaz**

`CreateAttributeMappingCommandHandlerTests.cs`:
```csharp
using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Directories.Commands.CreateAttributeMapping;
using EforTakip.Domain.Directories;
using FluentAssertions;
using NSubstitute;

namespace EforTakip.Application.Tests.Directories.Commands;

public class CreateAttributeMappingCommandHandlerTests
{
    private readonly IRepository<DirectoryAttributeMapping> _repository =
        Substitute.For<IRepository<DirectoryAttributeMapping>>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task Handle_CreatesMapping()
    {
        var handler = new CreateAttributeMappingCommandHandler(_repository, _unitOfWork);
        var command = new CreateAttributeMappingCommand("company", "Kurum", "text", true, 0);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeEmpty();
        await _repository.Received(1).AddAsync(
            Arg.Is<DirectoryAttributeMapping>(m => m.AdAttributeName == "company" && m.SystemFieldName == "Kurum"),
            Arg.Any<CancellationToken>());
    }
}
```

- [ ] **Step 4: Testi çalıştır, başarısız→geçer döngüsü**

Run: `dotnet test backend/tests/EforTakip.Application.Tests/EforTakip.Application.Tests.csproj --filter CreateAttributeMapping`
Expected: İlk çalıştırmada (handler henüz yoksa) FAIL, Step 2 tamamlandıysa PASS. Handler yazıldıktan sonra PASS.

- [ ] **Step 5: Update command + validator + handler yaz**

`UpdateAttributeMapping/UpdateAttributeMappingCommand.cs`:
```csharp
using MediatR;

namespace EforTakip.Application.Directories.Commands.UpdateAttributeMapping;

public sealed record UpdateAttributeMappingCommand(
    Guid Id,
    string AdAttributeName,
    string SystemFieldName,
    string FieldType,
    bool IsSynced,
    int SortOrder) : IRequest;
```

`UpdateAttributeMapping/UpdateAttributeMappingCommandValidator.cs`:
```csharp
using FluentValidation;

namespace EforTakip.Application.Directories.Commands.UpdateAttributeMapping;

public sealed class UpdateAttributeMappingCommandValidator : AbstractValidator<UpdateAttributeMappingCommand>
{
    public UpdateAttributeMappingCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.AdAttributeName).NotEmpty().WithMessage("AD alan adı zorunludur.").MaximumLength(150);
        RuleFor(x => x.SystemFieldName).NotEmpty().WithMessage("Sistem alan adı zorunludur.").MaximumLength(150);
        RuleFor(x => x.FieldType).NotEmpty().WithMessage("Alan tipi zorunludur.").MaximumLength(50);
    }
}
```

`UpdateAttributeMapping/UpdateAttributeMappingCommandHandler.cs`:
```csharp
using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Directories;
using EforTakip.Domain.Exceptions;
using MediatR;

namespace EforTakip.Application.Directories.Commands.UpdateAttributeMapping;

public sealed class UpdateAttributeMappingCommandHandler(
    IRepository<DirectoryAttributeMapping> repository, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateAttributeMappingCommand>
{
    public async Task Handle(UpdateAttributeMappingCommand request, CancellationToken cancellationToken)
    {
        var mapping = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(DirectoryAttributeMapping), request.Id);

        mapping.Update(request.AdAttributeName, request.SystemFieldName, request.FieldType,
            request.IsSynced, request.SortOrder);

        repository.Update(mapping);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
```

- [ ] **Step 6: Delete command + handler yaz**

`DeleteAttributeMapping/DeleteAttributeMappingCommand.cs`:
```csharp
using MediatR;

namespace EforTakip.Application.Directories.Commands.DeleteAttributeMapping;

public sealed record DeleteAttributeMappingCommand(Guid Id) : IRequest;
```

`DeleteAttributeMapping/DeleteAttributeMappingCommandHandler.cs`:
```csharp
using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Directories;
using EforTakip.Domain.Exceptions;
using MediatR;

namespace EforTakip.Application.Directories.Commands.DeleteAttributeMapping;

public sealed class DeleteAttributeMappingCommandHandler(
    IRepository<DirectoryAttributeMapping> repository, IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteAttributeMappingCommand>
{
    public async Task Handle(DeleteAttributeMappingCommand request, CancellationToken cancellationToken)
    {
        var mapping = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(DirectoryAttributeMapping), request.Id);

        repository.Remove(mapping);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
```

- [ ] **Step 7: GetAttributeMappings query + handler yaz**

`GetAttributeMappings/GetAttributeMappingsQuery.cs`:
```csharp
using EforTakip.Application.Directories.Dtos;
using MediatR;

namespace EforTakip.Application.Directories.Queries.GetAttributeMappings;

public sealed record GetAttributeMappingsQuery : IRequest<IReadOnlyCollection<DirectoryAttributeMappingDto>>;
```

`GetAttributeMappings/GetAttributeMappingsQueryHandler.cs`:
```csharp
using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Directories.Dtos;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.Directories.Queries.GetAttributeMappings;

public sealed class GetAttributeMappingsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAttributeMappingsQuery, IReadOnlyCollection<DirectoryAttributeMappingDto>>
{
    public async Task<IReadOnlyCollection<DirectoryAttributeMappingDto>> Handle(
        GetAttributeMappingsQuery request, CancellationToken cancellationToken)
    {
        return await db.DirectoryAttributeMappings
            .AsNoTracking()
            .OrderBy(m => m.SortOrder)
            .ProjectToType<DirectoryAttributeMappingDto>()
            .ToListAsync(cancellationToken);
    }
}
```

- [ ] **Step 8: Testleri çalıştır, geçtiğini doğrula**

Run: `dotnet test backend/EforTakip.sln`
Expected: PASS.

- [ ] **Step 9: Commit**

```bash
git add backend/src/EforTakip.Application/Directories/ backend/tests/EforTakip.Application.Tests/Directories/Commands/CreateAttributeMappingCommandHandlerTests.cs
git commit -m "feat: add attribute mapping commands and query"
```

---

## Task 10: API Controllers

**Files:**
- Create: `backend/src/EforTakip.Api/Controllers/v1/DirectoriesController.cs`
- Create: `backend/src/EforTakip.Api/Controllers/v1/DirectoryAttributeMappingsController.cs`

**Interfaces:**
- Consumes: `ISender mediator`; tüm Task 6-9 command/query'leri; `DirectoryDto`, `DirectoryAttributeMappingDto`, `PagedResult<T>`.
- Produces: REST endpoints.

- [ ] **Step 1: DirectoriesController yaz**

`DirectoriesController.cs`:
```csharp
using Asp.Versioning;
using EforTakip.Application.Common.Models;
using EforTakip.Application.Directories.Commands.CreateDirectory;
using EforTakip.Application.Directories.Commands.DeleteDirectory;
using EforTakip.Application.Directories.Commands.UpdateDirectory;
using EforTakip.Application.Directories.Dtos;
using EforTakip.Application.Directories.Queries.GetDirectories;
using EforTakip.Application.Directories.Queries.GetDirectoryById;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EforTakip.Api.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class DirectoriesController(ISender mediator) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateDirectoryCommand command, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id, version = "1.0" }, null);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DirectoryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DirectoryDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new GetDirectoryByIdQuery(id), cancellationToken));

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<DirectoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<DirectoryDto>>> GetAll(
        [FromQuery] GetDirectoriesQuery query, CancellationToken cancellationToken)
        => Ok(await mediator.Send(query, cancellationToken));

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(Guid id, UpdateDirectoryCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id)
            return BadRequest("Route ve gövde kimlikleri eşleşmiyor.");
        await mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteDirectoryCommand(id), cancellationToken);
        return NoContent();
    }
}
```

- [ ] **Step 2: DirectoryAttributeMappingsController yaz**

`DirectoryAttributeMappingsController.cs`:
```csharp
using Asp.Versioning;
using EforTakip.Application.Directories.Commands.CreateAttributeMapping;
using EforTakip.Application.Directories.Commands.DeleteAttributeMapping;
using EforTakip.Application.Directories.Commands.UpdateAttributeMapping;
using EforTakip.Application.Directories.Dtos;
using EforTakip.Application.Directories.Queries.GetAttributeMappings;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EforTakip.Api.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class DirectoryAttributeMappingsController(ISender mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<DirectoryAttributeMappingDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<DirectoryAttributeMappingDto>>> GetAll(
        CancellationToken cancellationToken)
        => Ok(await mediator.Send(new GetAttributeMappingsQuery(), cancellationToken));

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        CreateAttributeMappingCommand command, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { version = "1.0" }, new { id });
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(
        Guid id, UpdateAttributeMappingCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id)
            return BadRequest("Route ve gövde kimlikleri eşleşmiyor.");
        await mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteAttributeMappingCommand(id), cancellationToken);
        return NoContent();
    }
}
```

- [ ] **Step 3: Derle**

Run: `dotnet build backend/EforTakip.sln`
Expected: Build succeeded.

- [ ] **Step 4: Uygulamayı test modunda başlat, Swagger'da endpoint'leri doğrula**

Run: `dotnet run --project backend/src/EforTakip.Api` (arka planda) — sonra `http://localhost:5298/swagger` altında `Directories` ve `DirectoryAttributeMappings` endpoint'lerinin göründüğünü doğrula. Manuel bir POST ile dizin oluşturup GET ile listede döndüğünü kontrol et. Doğruladıktan sonra süreci durdur.
Expected: Endpoint'ler görünür; POST 201, GET dizini döndürür.

- [ ] **Step 5: Tüm test suite'ini son kez çalıştır**

Run: `dotnet test backend/EforTakip.sln`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add backend/src/EforTakip.Api/Controllers/v1/DirectoriesController.cs backend/src/EforTakip.Api/Controllers/v1/DirectoryAttributeMappingsController.cs
git commit -m "feat: add directory and attribute mapping API controllers"
```

---

## Faz 1 Tamamlanma Kriteri

- [ ] Tüm domain testleri geçiyor (Directory, DirectoryUser, DirectoryAttributeMapping).
- [ ] Tüm application testleri geçiyor (Create/Update directory + attribute mapping handler'ları, validator'lar).
- [ ] `dotnet build backend/EforTakip.sln` başarılı.
- [ ] Migration oluşturuldu (`AddDirectories`).
- [ ] Swagger'da 2 controller ve tüm CRUD endpoint'leri görünüyor, manuel POST/GET çalışıyor.
- [ ] Bind şifresi hiçbir DTO/response'ta dönmüyor.

## Sonraki Fazlar (ayrı planlar)

- **Faz 2 — LDAP Senkronizasyon:** `ILdapService` + `LdapService` (System.DirectoryServices.Protocols), `SyncDirectoryCommand`, `DirectoryUserAttribute` entity + persistence, `DirectorySyncBackgroundService`, `TestConnectionCommand`.
- **Faz 3 — JWT Auth + Login:** `IPasswordHasher`/`PasswordHasher` (BCrypt), `ITokenService`/`TokenService` (JWT), `SettingsEncryptor` (bind şifre şifreleme — CreateDirectory handler bu fazda güncellenir), `LoginCommand`, `AuthController`, JWT middleware (Program.cs).
- **Faz 4 — Frontend:** Kullanıcı Klasörü UI (`DirectoryList`, `DirectoryForm`, `DirectoryUserList`, `DirectoryUserCard`), global Alan Eşlemeleri bölümü, `LoginPage`, API client + react-query hook'ları, `AdminPage` entegrasyonu.
