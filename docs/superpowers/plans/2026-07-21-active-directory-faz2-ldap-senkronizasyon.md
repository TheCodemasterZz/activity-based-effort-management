# Active Directory Entegrasyonu — Faz 2: LDAP Senkronizasyon Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** AD dizinlerinden kullanıcıları (ve seçili attribute'larını) LDAP üzerinden çekip veritabanına senkronize etmek — manuel tetikleme ve zamanlanmış arka plan görevi ile.

**Architecture:** LDAP I/O Infrastructure'da (`LdapService`), senkronizasyon iş mantığı Application'da (`SyncDirectoryCommandHandler`) yer alır; aradaki sözleşme `ILdapService`. Bu ayrım sayesinde senkronizasyon mantığı gerçek bir AD olmadan, `ILdapService` mock'lanarak birim testi edilebilir. Zamanlanmış çalıştırma Infrastructure'daki `DirectorySyncBackgroundService` içinde MediatR üzerinden aynı komutu tetikler.

**Tech Stack:** .NET 8, System.DirectoryServices.Protocols 8.0.2, EF Core 8, MediatR, FluentValidation, Mapster, xUnit, FluentAssertions, NSubstitute.

## Global Constraints

- Faz 1'in tüm kuralları geçerlidir: domain entity'leri `sealed` + private ctor + static factory + `Entity` base; command/query `sealed record`; handler primary constructor'lı `sealed class`; validator `AbstractValidator<T>`; controller `[ApiController]`/`[ApiVersion("1.0")]`/`ISender`.
- **Application katmanında loglama yapılmaz** — projede hiçbir handler `ILogger` almaz. Senkronizasyon sonucu DTO olarak döner; loglama `DirectorySyncBackgroundService` (Infrastructure) içinde yapılır.
- **Şifre, token, bind credential asla loglanmaz.** Log satırları yalnızca sayısal özet ve dizin adı içerir.
- `Directory` tipi `System.IO.Directory` ile çakışır — `Directory` adını kullanan her dosyaya `using Directory = EforTakip.Domain.Directories.Directory;` alias'ı eklenir.
- Bind şifresi Faz 1'de düz metin saklanıyor; Faz 2 bunu olduğu gibi kullanır. Şifreleme Faz 3'te eklenecek (`SettingsEncryptor`).
- Tüm kullanıcıya dönük metinler Türkçe.
- LDAP çağrıları senkron API'dir; `Task.Run` ile sarılıp `CancellationToken` ile iptal edilebilir hale getirilir.

---

## Dosya Yapısı

**Domain (`backend/src/EforTakip.Domain/Directories/`):**
- `DirectoryUserAttribute.cs` — yeni: kullanıcının attribute değeri (DirectoryUser aggregate'inin çocuğu)
- `DirectoryUser.cs` — değişiklik: attribute koleksiyonu + `SetAttribute`/`ClearAttributes`
- `Directory.cs` — değişiklik: `LastSyncedUtc`, `MarkSynced()`, `IsSyncDue()`

**Application (`backend/src/EforTakip.Application/Directories/`):**
- `Ldap/ILdapService.cs` — LDAP sözleşmesi
- `Ldap/LdapUser.cs` — LDAP'ten dönen kullanıcı modeli
- `Ldap/LdapConnectionTestResult.cs` — bağlantı testi sonucu
- `Commands/TestDirectoryConnection/` — Command, Handler
- `Commands/SyncDirectory/` — Command, Handler
- `Dtos/DirectorySyncResultDto.cs`, `Dtos/DirectoryUserDto.cs`, `Dtos/DirectoryUserDetailDto.cs`
- `Queries/GetDirectoryUsers/` — Query, Handler
- `Queries/GetDirectoryUserById/` — Query, Handler

**Infrastructure (`backend/src/EforTakip.Infrastructure/`):**
- `EforTakip.Infrastructure.csproj` — değişiklik: 3 paket
- `Ldap/LdapService.cs` — LDAP bağlantı/arama implementasyonu
- `Sync/DirectorySyncBackgroundService.cs` — zamanlanmış senkronizasyon
- `DependencyInjection.cs` — değişiklik: servis kayıtları

**Persistence (`backend/src/EforTakip.Persistence/`):**
- `Configurations/DirectoryUserAttributeConfiguration.cs`
- `Configurations/DirectoryUserConfiguration.cs` — değişiklik: attribute koleksiyonu navigasyonu
- `EforTakipDbContext.cs`, `IApplicationDbContext.cs` — değişiklik: yeni DbSet
- `Migrations/` — yeni migration

**API (`backend/src/EforTakip.Api/Controllers/v1/`):**
- `DirectoriesController.cs` — değişiklik: `sync` ve `test-connection` endpoint'leri
- `DirectoryUsersController.cs` — yeni

**Tests:**
- `backend/tests/EforTakip.Domain.Tests/Directories/DirectoryUserTests.cs` — ek testler
- `backend/tests/EforTakip.Domain.Tests/Directories/DirectoryTests.cs` — ek testler (`IsSyncDue`)
- `backend/tests/EforTakip.Application.Tests/Directories/Commands/SyncDirectoryCommandHandlerTests.cs`
- `backend/tests/EforTakip.Application.Tests/Directories/Commands/TestDirectoryConnectionCommandHandlerTests.cs`

---

## Task 1: Domain — attribute koleksiyonu ve senkron zamanlaması

**Files:**
- Create: `backend/src/EforTakip.Domain/Directories/DirectoryUserAttribute.cs`
- Modify: `backend/src/EforTakip.Domain/Directories/DirectoryUser.cs`
- Modify: `backend/src/EforTakip.Domain/Directories/Directory.cs`
- Test: `backend/tests/EforTakip.Domain.Tests/Directories/DirectoryUserTests.cs` (mevcut dosyaya ek)
- Test: `backend/tests/EforTakip.Domain.Tests/Directories/DirectoryTests.cs` (mevcut dosyaya ek)

**Interfaces:**
- Consumes: `Entity`, `IAggregateRoot`, `BusinessRuleValidationException`, `SyncScheduleKind` (Faz 1).
- Produces:
  - `DirectoryUserAttribute` — properties: `DirectoryUserId`, `AttributeMappingId`, `Value`; `static Create(Guid directoryUserId, Guid attributeMappingId, string? value)`; `void SetValue(string? value)`
  - `DirectoryUser.Attributes` → `IReadOnlyCollection<DirectoryUserAttribute>`
  - `DirectoryUser.SetAttribute(Guid attributeMappingId, string? value)` — varsa günceller, yoksa ekler
  - `DirectoryUser.ClearAttributes()`
  - `DirectoryUser.UpdateFromSync(string? firstName, string? lastName, string? displayName, string? email, bool isEnabled, DateTime syncedUtc)` — **imza değişikliği:** `isEnabled` eklendi; aktiflik artık dizinden gelir, koşulsuz `true` yapılmaz
  - `Directory.LastSyncedUtc` → `DateTime?`
  - `Directory.MarkSynced(DateTime syncedUtc)`
  - `Directory.IsSyncDue(DateTime nowUtc)` → `bool`

- [ ] **Step 1: Testleri yaz**

`backend/tests/EforTakip.Domain.Tests/Directories/DirectoryUserTests.cs` — dosyanın sonuna, son `}` işaretinden önce ekle:
```csharp
    [Fact]
    public void SetAttribute_WithNewMapping_AddsAttribute()
    {
        var user = DirectoryUser.CreateFromActiveDirectory(
            Guid.NewGuid(), "serkan.gultepe", "Serkan", "Gültepe", "Serkan Gültepe", null, "guid");
        var mappingId = Guid.NewGuid();

        user.SetAttribute(mappingId, "Kızılay");

        user.Attributes.Should().ContainSingle();
        user.Attributes.Single().AttributeMappingId.Should().Be(mappingId);
        user.Attributes.Single().Value.Should().Be("Kızılay");
    }

    [Fact]
    public void SetAttribute_WithExistingMapping_UpdatesInPlace()
    {
        var user = DirectoryUser.CreateFromActiveDirectory(
            Guid.NewGuid(), "serkan.gultepe", "Serkan", "Gültepe", "Serkan Gültepe", null, "guid");
        var mappingId = Guid.NewGuid();
        user.SetAttribute(mappingId, "Eski Kurum");

        user.SetAttribute(mappingId, "Yeni Kurum");

        user.Attributes.Should().ContainSingle();
        user.Attributes.Single().Value.Should().Be("Yeni Kurum");
    }

    [Fact]
    public void ClearAttributes_RemovesAll()
    {
        var user = DirectoryUser.CreateFromActiveDirectory(
            Guid.NewGuid(), "serkan.gultepe", "Serkan", "Gültepe", "Serkan Gültepe", null, "guid");
        user.SetAttribute(Guid.NewGuid(), "a");
        user.SetAttribute(Guid.NewGuid(), "b");

        user.ClearAttributes();

        user.Attributes.Should().BeEmpty();
    }

    [Fact]
    public void UpdateFromSync_WhenDisabledInDirectory_DeactivatesUser()
    {
        var user = DirectoryUser.CreateFromActiveDirectory(
            Guid.NewGuid(), "serkan.gultepe", "Serkan", "Gültepe", "Serkan Gültepe", null, "guid");

        user.UpdateFromSync("Serkan", "Gültepe", "Serkan Gültepe", null, isEnabled: false, DateTime.UtcNow);

        user.IsActive.Should().BeFalse();
    }

    [Fact]
    public void UpdateFromSync_WhenReEnabledInDirectory_ReactivatesUser()
    {
        var user = DirectoryUser.CreateFromActiveDirectory(
            Guid.NewGuid(), "serkan.gultepe", "Serkan", "Gültepe", "Serkan Gültepe", null, "guid");
        user.Deactivate();

        user.UpdateFromSync("Serkan", "Gültepe", "Serkan Gültepe", null, isEnabled: true, DateTime.UtcNow);

        user.IsActive.Should().BeTrue();
    }
```

**Mevcut testte imza değişikliği:** `DirectoryUserTests.UpdateFromSync_UpdatesFieldsAndLastSynced` testindeki çağrıyı güncelle:
```csharp
        user.UpdateFromSync("Serkan", "Yeni", "Serkan Yeni", "yeni@x.com", isEnabled: true, syncTime);
```

`backend/tests/EforTakip.Domain.Tests/Directories/DirectoryTests.cs` — dosyanın sonuna, son `}` işaretinden önce ekle:
```csharp
    [Fact]
    public void IsSyncDue_WithScheduleOff_ReturnsFalse()
    {
        var directory = Directory.CreateActiveDirectory(
            "Ad", "Microsoft Active Directory", "kizilay.local", 389, false, "u", "ENC(x)",
            "DC=kizilay,DC=local", null, null, DirectoryPermission.ReadOnly, "user", "(x)",
            "sAMAccountName", "cn", "givenName", "sn", "displayName", "mail", "objectGUID",
            SyncScheduleKind.Off, 0);

        directory.IsSyncDue(DateTime.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void IsSyncDue_NeverSynced_ReturnsTrue()
    {
        var directory = CreateValidAd();

        directory.IsSyncDue(DateTime.UtcNow).Should().BeTrue();
    }

    [Fact]
    public void IsSyncDue_DailyAndSyncedRecently_ReturnsFalse()
    {
        var directory = CreateValidAd();
        var now = DateTime.UtcNow;
        directory.MarkSynced(now.AddHours(-2));

        directory.IsSyncDue(now).Should().BeFalse();
    }

    [Fact]
    public void IsSyncDue_DailyAndSyncedLongAgo_ReturnsTrue()
    {
        var directory = CreateValidAd();
        var now = DateTime.UtcNow;
        directory.MarkSynced(now.AddDays(-2));

        directory.IsSyncDue(now).Should().BeTrue();
    }

    [Fact]
    public void IsSyncDue_InactiveDirectory_ReturnsFalse()
    {
        var directory = CreateValidAd();
        directory.Deactivate();

        directory.IsSyncDue(DateTime.UtcNow).Should().BeFalse();
    }
```

- [ ] **Step 2: Testleri çalıştır, başarısız olduğunu doğrula**

Run: `dotnet test backend/tests/EforTakip.Domain.Tests/EforTakip.Domain.Tests.csproj --filter "DirectoryTests|DirectoryUserTests"`
Expected: FAIL — `SetAttribute`, `Attributes`, `MarkSynced`, `IsSyncDue` tanımlı değil.

- [ ] **Step 3: DirectoryUserAttribute entity'sini yaz**

`backend/src/EforTakip.Domain/Directories/DirectoryUserAttribute.cs`:
```csharp
using EforTakip.Domain.Common;
using EforTakip.Domain.Exceptions;

namespace EforTakip.Domain.Directories;

public sealed class DirectoryUserAttribute : Entity
{
    public Guid DirectoryUserId { get; private set; }
    public Guid AttributeMappingId { get; private set; }
    public string? Value { get; private set; }

    private DirectoryUserAttribute()
    {
        // EF Core
    }

    public static DirectoryUserAttribute Create(Guid directoryUserId, Guid attributeMappingId, string? value)
    {
        if (attributeMappingId == Guid.Empty)
            throw new BusinessRuleValidationException("Attribute eşlemesi belirtilmelidir.");

        return new DirectoryUserAttribute
        {
            DirectoryUserId = directoryUserId,
            AttributeMappingId = attributeMappingId,
            Value = value
        };
    }

    public void SetValue(string? value) => Value = value;
}
```

- [ ] **Step 4: DirectoryUser'a attribute koleksiyonunu ekle**

`backend/src/EforTakip.Domain/Directories/DirectoryUser.cs` — `LastSyncedUtc` property'sinden sonra ekle:
```csharp
    private readonly List<DirectoryUserAttribute> _attributes = [];
    public IReadOnlyCollection<DirectoryUserAttribute> Attributes => _attributes.AsReadOnly();
```

Mevcut `UpdateFromSync` metodunu değiştir — aktiflik artık dizinden gelir:
```csharp
    public void UpdateFromSync(
        string? firstName, string? lastName, string? displayName, string? email,
        bool isEnabled, DateTime syncedUtc)
    {
        FirstName = firstName;
        LastName = lastName;
        DisplayName = displayName;
        Email = email;
        LastSyncedUtc = syncedUtc;
        // Dizinde devre dışı bırakılan hesap sistemde de pasife alınır.
        IsActive = isEnabled;
    }
```

Ve `Activate()` metodundan sonra, `private static void ValidateDirectoryId` satırından önce ekle:
```csharp
    public void SetAttribute(Guid attributeMappingId, string? value)
    {
        var existing = _attributes.FirstOrDefault(a => a.AttributeMappingId == attributeMappingId);
        if (existing is not null)
        {
            existing.SetValue(value);
            return;
        }

        _attributes.Add(DirectoryUserAttribute.Create(Id, attributeMappingId, value));
    }

    public void ClearAttributes() => _attributes.Clear();
```

- [ ] **Step 5: Directory'ye senkron zamanlamasını ekle**

`backend/src/EforTakip.Domain/Directories/Directory.cs` — `SortOrder` property'sinden sonra ekle:
```csharp
    public DateTime? LastSyncedUtc { get; private set; }
```

Ve `Deactivate()` metodundan sonra ekle:
```csharp
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
```

- [ ] **Step 6: Testleri çalıştır, geçtiğini doğrula**

Run: `dotnet test backend/tests/EforTakip.Domain.Tests/EforTakip.Domain.Tests.csproj`
Expected: PASS (Faz 1'in 59 testi + yeni 8 test = 67).

- [ ] **Step 7: Commit**

```bash
git add backend/src/EforTakip.Domain/Directories/ backend/tests/EforTakip.Domain.Tests/Directories/
git commit -m "feat: add directory user attributes and sync scheduling to domain"
```

---

## Task 2: Persistence — DirectoryUserAttribute ve migration

**Files:**
- Create: `backend/src/EforTakip.Persistence/Configurations/DirectoryUserAttributeConfiguration.cs`
- Modify: `backend/src/EforTakip.Persistence/Configurations/DirectoryUserConfiguration.cs`
- Modify: `backend/src/EforTakip.Persistence/EforTakipDbContext.cs`
- Modify: `backend/src/EforTakip.Application/Common/Interfaces/IApplicationDbContext.cs`

**Interfaces:**
- Consumes: `DirectoryUserAttribute`, `DirectoryUser.Attributes` (Task 1).
- Produces: `IApplicationDbContext.DirectoryUserAttributes` → `DbSet<DirectoryUserAttribute>`; `DirectoryUsers` sorgularında `.Include(u => u.Attributes)` kullanılabilir hale gelir.

- [ ] **Step 1: DirectoryUserAttributeConfiguration yaz**

`backend/src/EforTakip.Persistence/Configurations/DirectoryUserAttributeConfiguration.cs`:
```csharp
using EforTakip.Domain.Directories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EforTakip.Persistence.Configurations;

public sealed class DirectoryUserAttributeConfiguration : IEntityTypeConfiguration<DirectoryUserAttribute>
{
    public void Configure(EntityTypeBuilder<DirectoryUserAttribute> builder)
    {
        builder.ToTable("DirectoryUserAttributes");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Value).HasMaxLength(2000);

        builder.HasIndex(a => new { a.DirectoryUserId, a.AttributeMappingId }).IsUnique();

        builder.HasOne<DirectoryAttributeMapping>()
            .WithMany()
            .HasForeignKey(a => a.AttributeMappingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

- [ ] **Step 2: DirectoryUserConfiguration'a koleksiyon navigasyonunu ekle**

`backend/src/EforTakip.Persistence/Configurations/DirectoryUserConfiguration.cs` — `builder.HasOne<Directory>()` bloğundan önce ekle:
```csharp
        builder.HasMany(u => u.Attributes)
            .WithOne()
            .HasForeignKey(a => a.DirectoryUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(DirectoryUser.Attributes))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
```

- [ ] **Step 3: DbSet'leri ekle**

`backend/src/EforTakip.Application/Common/Interfaces/IApplicationDbContext.cs` — `DirectoryAttributeMappings` satırından sonra ekle:
```csharp
    DbSet<DirectoryUserAttribute> DirectoryUserAttributes { get; }
```

`backend/src/EforTakip.Persistence/EforTakipDbContext.cs` — `DirectoryAttributeMappings` satırından sonra ekle:
```csharp
    public DbSet<DirectoryUserAttribute> DirectoryUserAttributes => Set<DirectoryUserAttribute>();
```

- [ ] **Step 4: Derle**

Run: `dotnet build backend/EforTakip.sln`
Expected: Build succeeded. (Çalışan bir `EforTakip.Api` süreci varsa DLL kilidi hatası alırsın — önce süreci kapat.)

- [ ] **Step 5: Migration oluştur**

Run:
```bash
export PATH="$PATH:$HOME/.dotnet/tools"
ASPNETCORE_ENVIRONMENT=Production \
ConnectionStrings__DefaultConnection="Host=localhost;Database=efortakip;Username=postgres;Password=postgres" \
dotnet ef migrations add AddDirectoryUserAttributes \
  --project backend/src/EforTakip.Persistence \
  --startup-project backend/src/EforTakip.Api
```
Expected: `Done.` — `Migrations/` altında `*_AddDirectoryUserAttributes.cs` oluşur.

Not: Development ortamı InMemory kullandığı için migration üretemez; sahte connection string yalnızca şema üretimi içindir, veritabanına bağlanılmaz.

- [ ] **Step 6: Migration'ın doğru tabloyu ürettiğini doğrula**

Run: `grep -c "DirectoryUserAttributes" backend/src/EforTakip.Persistence/Migrations/*AddDirectoryUserAttributes.cs`
Expected: 1'den büyük bir sayı (CreateTable + index + DropTable).

- [ ] **Step 7: Testleri çalıştır**

Run: `dotnet test backend/tests/EforTakip.Domain.Tests/EforTakip.Domain.Tests.csproj`
Expected: PASS.

- [ ] **Step 8: Commit**

```bash
git add backend/src/EforTakip.Persistence/ backend/src/EforTakip.Application/Common/Interfaces/IApplicationDbContext.cs
git commit -m "feat: persist directory user attributes"
```

---

## Task 3: ILdapService sözleşmesi ve modelleri

**Files:**
- Create: `backend/src/EforTakip.Application/Directories/Ldap/LdapUser.cs`
- Create: `backend/src/EforTakip.Application/Directories/Ldap/LdapConnectionTestResult.cs`
- Create: `backend/src/EforTakip.Application/Directories/Ldap/ILdapService.cs`

**Interfaces:**
- Consumes: `Directory` (Domain).
- Produces:
  - `LdapUser` — `sealed record LdapUser(string Username, string? FirstName, string? LastName, string? DisplayName, string? Email, string ObjectGuid, IReadOnlyDictionary<string, string?> Attributes)` — `Attributes` anahtarı AD attribute adıdır (ör. `company`).
  - `LdapConnectionTestResult` — `sealed record LdapConnectionTestResult(bool Success, string Message)`
  - `ILdapService` — `Task<LdapConnectionTestResult> TestConnectionAsync(Directory directory, CancellationToken cancellationToken)` ve `Task<IReadOnlyList<LdapUser>> SearchUsersAsync(Directory directory, IReadOnlyCollection<string> extraAttributeNames, CancellationToken cancellationToken)`

- [ ] **Step 1: Modelleri yaz**

`LdapUser.cs`:
```csharp
namespace EforTakip.Application.Directories.Ldap;

/// <summary>LDAP dizininden okunan tek bir kullanıcı. Attributes anahtarları AD attribute adlarıdır.</summary>
/// <param name="IsEnabled">
/// Hesabın dizinde etkin olup olmadığı. Microsoft AD'de devre dışı bırakılan hesaplar dizinde
/// kalmaya devam eder, yalnızca userAccountControl alanının ACCOUNTDISABLE biti işaretlenir.
/// </param>
public sealed record LdapUser(
    string Username,
    string? FirstName,
    string? LastName,
    string? DisplayName,
    string? Email,
    string ObjectGuid,
    bool IsEnabled,
    IReadOnlyDictionary<string, string?> Attributes);
```

`LdapConnectionTestResult.cs`:
```csharp
namespace EforTakip.Application.Directories.Ldap;

public sealed record LdapConnectionTestResult(bool Success, string Message);
```

- [ ] **Step 2: ILdapService yaz**

`ILdapService.cs`:
```csharp
using Directory = EforTakip.Domain.Directories.Directory;

namespace EforTakip.Application.Directories.Ldap;

public interface ILdapService
{
    /// <summary>Dizin ayarlarıyla bağlanmayı dener; başarısızlıkta kullanıcıya gösterilebilir bir mesaj döner.</summary>
    Task<LdapConnectionTestResult> TestConnectionAsync(Directory directory, CancellationToken cancellationToken);

    /// <summary>
    /// Dizindeki kullanıcıları arar. <paramref name="extraAttributeNames"/> senkronize edilecek
    /// ek AD attribute adlarıdır (alan eşlemelerinden gelir).
    /// </summary>
    Task<IReadOnlyList<LdapUser>> SearchUsersAsync(
        Directory directory,
        IReadOnlyCollection<string> extraAttributeNames,
        CancellationToken cancellationToken);
}
```

- [ ] **Step 3: Derle**

Run: `dotnet build backend/src/EforTakip.Application/EforTakip.Application.csproj`
Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add backend/src/EforTakip.Application/Directories/Ldap/
git commit -m "feat: add LDAP service contract and models"
```

---

## Task 4: LdapService implementasyonu (Infrastructure)

**Files:**
- Modify: `backend/src/EforTakip.Infrastructure/EforTakip.Infrastructure.csproj`
- Create: `backend/src/EforTakip.Infrastructure/Ldap/LdapService.cs`
- Modify: `backend/src/EforTakip.Infrastructure/DependencyInjection.cs`

**Interfaces:**
- Consumes: `ILdapService`, `LdapUser`, `LdapConnectionTestResult` (Task 3); `Directory` (Domain).
- Produces: `LdapService : ILdapService` — DI'da `ILdapService` olarak kayıtlı.

**Not:** Bu sınıf gerçek bir LDAP sunucusu gerektirdiği için birim testi yazılmaz; senkronizasyon mantığının testi Task 6'da `ILdapService` mock'lanarak yapılır. Bu sınıf Task 8'de canlı olarak doğrulanır.

- [ ] **Step 1: Paketleri ekle**

`backend/src/EforTakip.Infrastructure/EforTakip.Infrastructure.csproj` — `<ItemGroup>` bloklarının arasına yeni bir ItemGroup ekle:
```xml
  <ItemGroup>
    <PackageReference Include="System.DirectoryServices.Protocols" Version="8.0.2" />
    <PackageReference Include="Microsoft.Extensions.Hosting.Abstractions" Version="8.0.1" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.2" />
  </ItemGroup>
```

- [ ] **Step 2: Paketlerin çözüldüğünü doğrula**

Run: `dotnet restore backend/src/EforTakip.Infrastructure/EforTakip.Infrastructure.csproj`
Expected: Restore başarılı, hata yok.

- [ ] **Step 3: LdapService'i yaz**

`backend/src/EforTakip.Infrastructure/Ldap/LdapService.cs`:
```csharp
using System.DirectoryServices.Protocols;
using System.Net;
using System.Text;
using EforTakip.Application.Directories.Ldap;
using Directory = EforTakip.Domain.Directories.Directory;

namespace EforTakip.Infrastructure.Ldap;

/// <summary>
/// System.DirectoryServices.Protocols üzerinden LDAP erişimi. Kütüphanenin API'si senkron
/// olduğundan çağrılar Task.Run ile arka plana alınır.
/// </summary>
public sealed class LdapService : ILdapService
{
    private const int PageSize = 500;

    /// <summary>Microsoft AD userAccountControl bayrağı: hesap devre dışı.</summary>
    private const int AccountDisabledFlag = 0x2;

    private const string UserAccountControlAttribute = "userAccountControl";

    private static readonly TimeSpan ConnectionTimeout = TimeSpan.FromSeconds(30);

    public Task<LdapConnectionTestResult> TestConnectionAsync(
        Directory directory, CancellationToken cancellationToken)
        => Task.Run(() =>
        {
            try
            {
                using var connection = CreateConnection(directory);
                connection.Bind();
                return new LdapConnectionTestResult(true, "Bağlantı başarılı.");
            }
            catch (LdapException ex)
            {
                return new LdapConnectionTestResult(false, DescribeLdapError(ex));
            }
            catch (Exception)
            {
                return new LdapConnectionTestResult(false, "Sunucuya bağlanılamadı. Ayarları kontrol edin.");
            }
        }, cancellationToken);

    public Task<IReadOnlyList<LdapUser>> SearchUsersAsync(
        Directory directory, IReadOnlyCollection<string> extraAttributeNames, CancellationToken cancellationToken)
        => Task.Run<IReadOnlyList<LdapUser>>(() =>
        {
            using var connection = CreateConnection(directory);
            connection.Bind();

            var attributesToLoad = BuildAttributeList(directory, extraAttributeNames);
            var searchBase = BuildSearchBase(directory);
            var filter = string.IsNullOrWhiteSpace(directory.UserObjectFilter)
                ? "(objectClass=*)"
                : directory.UserObjectFilter;

            var request = new SearchRequest(searchBase, filter, SearchScope.Subtree, attributesToLoad);
            var pageControl = new PageResultRequestControl(PageSize);
            request.Controls.Add(pageControl);

            var users = new List<LdapUser>();

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var response = (SearchResponse)connection.SendRequest(request);

                foreach (SearchResultEntry entry in response.Entries)
                {
                    var user = MapUser(entry, directory, extraAttributeNames);
                    if (user is not null)
                        users.Add(user);
                }

                var pageResponse = response.Controls
                    .OfType<PageResultResponseControl>()
                    .FirstOrDefault();

                if (pageResponse is null || pageResponse.Cookie.Length == 0)
                    break;

                pageControl.Cookie = pageResponse.Cookie;
            }

            return users;
        }, cancellationToken);

    private static LdapConnection CreateConnection(Directory directory)
    {
        var identifier = new LdapDirectoryIdentifier(
            directory.Hostname, directory.Port, fullyQualifiedDnsHostName: false, connectionless: false);

        // NOT: BindPasswordEncrypted Faz 2'de düz metin tutulur; Faz 3'te çözülmüş değer geçilecek.
        var credential = new NetworkCredential(directory.BindUsername, directory.BindPasswordEncrypted);

        // AuthType.Basic = simple bind. Microsoft AD'ye simple bind ile bağlanırken şifre,
        // SSL kapalıysa ağ üzerinde düz metin gider — üretimde LDAPS (636) kullanılmalıdır.

        var connection = new LdapConnection(identifier, credential, AuthType.Basic)
        {
            Timeout = ConnectionTimeout
        };
        connection.SessionOptions.ProtocolVersion = 3;
        connection.SessionOptions.SecureSocketLayer = directory.UseSsl;
        return connection;
    }

    private static string BuildSearchBase(Directory directory)
        => string.IsNullOrWhiteSpace(directory.AdditionalUserDn)
            ? directory.BaseDn ?? string.Empty
            : $"{directory.AdditionalUserDn},{directory.BaseDn}";

    private static string[] BuildAttributeList(Directory directory, IReadOnlyCollection<string> extraAttributeNames)
    {
        var names = new List<string?>
        {
            directory.UsernameAttribute,
            directory.FirstNameAttribute,
            directory.LastNameAttribute,
            directory.DisplayNameAttribute,
            directory.EmailAttribute,
            directory.UniqueIdAttribute,
            UserAccountControlAttribute
        };
        names.AddRange(extraAttributeNames);

        return names
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static LdapUser? MapUser(
        SearchResultEntry entry, Directory directory, IReadOnlyCollection<string> extraAttributeNames)
    {
        var username = ReadString(entry, directory.UsernameAttribute);
        var objectGuid = ReadGuid(entry, directory.UniqueIdAttribute);

        // Kullanıcı adı veya benzersiz kimliği olmayan kayıtlar (ör. konteyner nesneleri) atlanır.
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(objectGuid))
            return null;

        var attributes = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in extraAttributeNames)
            attributes[name] = ReadString(entry, name);

        return new LdapUser(
            username,
            ReadString(entry, directory.FirstNameAttribute),
            ReadString(entry, directory.LastNameAttribute),
            ReadString(entry, directory.DisplayNameAttribute),
            ReadString(entry, directory.EmailAttribute),
            objectGuid,
            ReadIsEnabled(entry),
            attributes);
    }

    /// <summary>
    /// Microsoft AD'de devre dışı bırakılan hesap dizinden silinmez; userAccountControl alanının
    /// ACCOUNTDISABLE biti işaretlenir. Alan okunamazsa hesap etkin kabul edilir.
    /// </summary>
    private static bool ReadIsEnabled(SearchResultEntry entry)
    {
        var raw = ReadString(entry, UserAccountControlAttribute);
        if (!int.TryParse(raw, out var flags))
            return true;

        return (flags & AccountDisabledFlag) == 0;
    }

    private static string? ReadString(SearchResultEntry entry, string? attributeName)
    {
        var raw = ReadRaw(entry, attributeName);
        return raw switch
        {
            null => null,
            string s => s,
            byte[] bytes => Encoding.UTF8.GetString(bytes),
            _ => raw.ToString()
        };
    }

    private static string? ReadGuid(SearchResultEntry entry, string? attributeName)
    {
        var raw = ReadRaw(entry, attributeName);
        return raw switch
        {
            null => null,
            byte[] { Length: 16 } bytes => new Guid(bytes).ToString(),
            byte[] bytes => Convert.ToHexString(bytes),
            string s => s,
            _ => raw.ToString()
        };
    }

    private static object? ReadRaw(SearchResultEntry entry, string? attributeName)
    {
        if (string.IsNullOrWhiteSpace(attributeName) || !entry.Attributes.Contains(attributeName))
            return null;

        var attribute = entry.Attributes[attributeName];
        return attribute is null || attribute.Count == 0 ? null : attribute[0];
    }

    /// <summary>İç sistem detayı sızdırmadan, yöneticinin ayarı düzeltmesine yarayan mesaj üretir.</summary>
    private static string DescribeLdapError(LdapException ex) => ex.ErrorCode switch
    {
        49 => "Kullanıcı adı veya şifre hatalı.",
        81 => "Sunucuya ulaşılamıyor. Adres ve port bilgisini kontrol edin.",
        _ => "Bağlantı kurulamadı. Sunucu ayarlarını kontrol edin."
    };
}
```

- [ ] **Step 4: DI kaydını ekle**

`backend/src/EforTakip.Infrastructure/DependencyInjection.cs` içeriğini değiştir:
```csharp
using EforTakip.Application.Directories.Ldap;
using EforTakip.Infrastructure.Ldap;
using Microsoft.Extensions.DependencyInjection;

namespace EforTakip.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<ILdapService, LdapService>();

        return services;
    }
}
```

- [ ] **Step 5: Derle**

Run: `dotnet build backend/EforTakip.sln`
Expected: Build succeeded.

- [ ] **Step 6: Commit**

```bash
git add backend/src/EforTakip.Infrastructure/
git commit -m "feat: implement LDAP service with paged search"
```

---

## Task 5: TestDirectoryConnection command

**Files:**
- Create: `backend/src/EforTakip.Application/Directories/Commands/TestDirectoryConnection/TestDirectoryConnectionCommand.cs`
- Create: `backend/src/EforTakip.Application/Directories/Commands/TestDirectoryConnection/TestDirectoryConnectionCommandHandler.cs`
- Test: `backend/tests/EforTakip.Application.Tests/Directories/Commands/TestDirectoryConnectionCommandHandlerTests.cs`

**Interfaces:**
- Consumes: `IRepository<Directory>`, `ILdapService`, `NotFoundException`, `BusinessRuleValidationException`.
- Produces: `TestDirectoryConnectionCommand(Guid DirectoryId) : IRequest<LdapConnectionTestResult>`.

- [ ] **Step 1: Command'ı yaz**

`TestDirectoryConnectionCommand.cs`:
```csharp
using EforTakip.Application.Directories.Ldap;
using MediatR;

namespace EforTakip.Application.Directories.Commands.TestDirectoryConnection;

public sealed record TestDirectoryConnectionCommand(Guid DirectoryId) : IRequest<LdapConnectionTestResult>;
```

- [ ] **Step 2: Testi yaz**

`TestDirectoryConnectionCommandHandlerTests.cs`:
```csharp
using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Directories.Commands.TestDirectoryConnection;
using EforTakip.Application.Directories.Ldap;
using EforTakip.Domain.Directories;
using EforTakip.Domain.Exceptions;
using FluentAssertions;
using NSubstitute;
using Directory = EforTakip.Domain.Directories.Directory;

namespace EforTakip.Application.Tests.Directories.Commands;

public class TestDirectoryConnectionCommandHandlerTests
{
    private readonly IRepository<Directory> _repository = Substitute.For<IRepository<Directory>>();
    private readonly ILdapService _ldapService = Substitute.For<ILdapService>();

    private static Directory ValidAd() =>
        Directory.CreateActiveDirectory(
            "Ad", "Microsoft Active Directory", "kizilay.local", 389, false, "u", "p",
            "DC=kizilay,DC=local", null, null, DirectoryPermission.ReadOnly, "user", "(x)",
            "sAMAccountName", "cn", "givenName", "sn", "displayName", "mail", "objectGUID",
            SyncScheduleKind.Off, 0);

    [Fact]
    public async Task Handle_SuccessfulConnection_ReturnsSuccess()
    {
        var directory = ValidAd();
        _repository.GetByIdAsync(directory.Id, Arg.Any<CancellationToken>()).Returns(directory);
        _ldapService.TestConnectionAsync(directory, Arg.Any<CancellationToken>())
            .Returns(new LdapConnectionTestResult(true, "Bağlantı başarılı."));
        var handler = new TestDirectoryConnectionCommandHandler(_repository, _ldapService);

        var result = await handler.Handle(new TestDirectoryConnectionCommand(directory.Id), CancellationToken.None);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_FailedConnection_ReturnsFailureMessage()
    {
        var directory = ValidAd();
        _repository.GetByIdAsync(directory.Id, Arg.Any<CancellationToken>()).Returns(directory);
        _ldapService.TestConnectionAsync(directory, Arg.Any<CancellationToken>())
            .Returns(new LdapConnectionTestResult(false, "Kullanıcı adı veya şifre hatalı."));
        var handler = new TestDirectoryConnectionCommandHandler(_repository, _ldapService);

        var result = await handler.Handle(new TestDirectoryConnectionCommand(directory.Id), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Kullanıcı adı veya şifre hatalı.");
    }

    [Fact]
    public async Task Handle_NonExistingDirectory_ThrowsNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Directory?)null);
        var handler = new TestDirectoryConnectionCommandHandler(_repository, _ldapService);

        var act = async () => await handler.Handle(
            new TestDirectoryConnectionCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_InternalDirectory_ThrowsBusinessRule()
    {
        var directory = Directory.CreateInternal("Internal Users", 0);
        _repository.GetByIdAsync(directory.Id, Arg.Any<CancellationToken>()).Returns(directory);
        var handler = new TestDirectoryConnectionCommandHandler(_repository, _ldapService);

        var act = async () => await handler.Handle(
            new TestDirectoryConnectionCommand(directory.Id), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleValidationException>();
    }
}
```

- [ ] **Step 3: Testi çalıştır, başarısız olduğunu doğrula**

Run: `dotnet test backend/tests/EforTakip.Application.Tests/EforTakip.Application.Tests.csproj --filter TestDirectoryConnection`
Expected: FAIL — `TestDirectoryConnectionCommandHandler` bulunamıyor.

- [ ] **Step 4: Handler'ı yaz**

`TestDirectoryConnectionCommandHandler.cs`:
```csharp
using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Directories.Ldap;
using EforTakip.Domain.Directories;
using EforTakip.Domain.Exceptions;
using MediatR;
using Directory = EforTakip.Domain.Directories.Directory;

namespace EforTakip.Application.Directories.Commands.TestDirectoryConnection;

public sealed class TestDirectoryConnectionCommandHandler(
    IRepository<Directory> repository, ILdapService ldapService)
    : IRequestHandler<TestDirectoryConnectionCommand, LdapConnectionTestResult>
{
    public async Task<LdapConnectionTestResult> Handle(
        TestDirectoryConnectionCommand request, CancellationToken cancellationToken)
    {
        var directory = await repository.GetByIdAsync(request.DirectoryId, cancellationToken)
            ?? throw new NotFoundException(nameof(Directory), request.DirectoryId);

        if (directory.Source != DirectorySource.ActiveDirectory)
            throw new BusinessRuleValidationException("Yalnızca Active Directory dizinleri için bağlantı testi yapılabilir.");

        return await ldapService.TestConnectionAsync(directory, cancellationToken);
    }
}
```

- [ ] **Step 5: Testi çalıştır, geçtiğini doğrula**

Run: `dotnet test backend/tests/EforTakip.Application.Tests/EforTakip.Application.Tests.csproj --filter TestDirectoryConnection`
Expected: PASS (4 test).

- [ ] **Step 6: Commit**

```bash
git add backend/src/EforTakip.Application/Directories/Commands/TestDirectoryConnection/ backend/tests/EforTakip.Application.Tests/Directories/Commands/TestDirectoryConnectionCommandHandlerTests.cs
git commit -m "feat: add directory connection test command"
```

---

## Task 6: SyncDirectory command (senkronizasyon çekirdeği)

**Files:**
- Create: `backend/src/EforTakip.Application/Directories/Dtos/DirectorySyncResultDto.cs`
- Create: `backend/src/EforTakip.Application/Directories/Commands/SyncDirectory/SyncDirectoryCommand.cs`
- Create: `backend/src/EforTakip.Application/Directories/Commands/SyncDirectory/SyncDirectoryCommandHandler.cs`
- Test: `backend/tests/EforTakip.Application.Tests/Directories/Commands/SyncDirectoryCommandHandlerTests.cs`

**Interfaces:**
- Consumes: `IApplicationDbContext`, `IRepository<Directory>`, `IUnitOfWork`, `ILdapService`, `LdapUser`, `DirectoryUser.CreateFromActiveDirectory/UpdateFromSync/SetAttribute/Deactivate`, `Directory.MarkSynced`.
- Produces:
  - `DirectorySyncResultDto { DirectoryId, DirectoryName, Added, Updated, Deactivated, TotalFromDirectory, SyncedAtUtc }`
  - `SyncDirectoryCommand(Guid DirectoryId) : IRequest<DirectorySyncResultDto>`

**Senkronizasyon kuralları:**
1. Dizin `ActiveDirectory` kaynaklı ve aktif olmalı; değilse `BusinessRuleValidationException`.
2. `IsSynced == true` olan alan eşlemeleri LDAP'ten çekilecek ek attribute listesini belirler.
3. Eşleştirme `ObjectGuid` üzerinden yapılır (kullanıcı adı değişse bile kimlik korunur).
4. LDAP'te olup DB'de olmayan → eklenir. İkisinde de olan → güncellenir. DB'de olup LDAP'te olmayan → pasife alınır (silinmez).
5. **Dizinde devre dışı bırakılan hesap (Microsoft AD `userAccountControl` ACCOUNTDISABLE biti) sistemde de pasife alınır.** Hesap dizinde durmaya devam ettiği için 4. kural bunu yakalamaz; aktiflik her senkronda dizinden okunur.
6. Her kullanıcı için yalnızca senkronize edilecek attribute'lar yazılır.
7. Dizin `MarkSynced` ile damgalanır.

- [ ] **Step 1: DTO'yu yaz**

`DirectorySyncResultDto.cs`:
```csharp
namespace EforTakip.Application.Directories.Dtos;

public sealed class DirectorySyncResultDto
{
    public Guid DirectoryId { get; init; }
    public string DirectoryName { get; init; } = default!;
    public int Added { get; init; }
    public int Updated { get; init; }
    public int Deactivated { get; init; }
    public int TotalFromDirectory { get; init; }
    public DateTime SyncedAtUtc { get; init; }
}
```

- [ ] **Step 2: Command'ı yaz**

`SyncDirectoryCommand.cs`:
```csharp
using EforTakip.Application.Directories.Dtos;
using MediatR;

namespace EforTakip.Application.Directories.Commands.SyncDirectory;

public sealed record SyncDirectoryCommand(Guid DirectoryId) : IRequest<DirectorySyncResultDto>;
```

- [ ] **Step 3: Testi yaz**

`SyncDirectoryCommandHandlerTests.cs`:
```csharp
using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Directories.Commands.SyncDirectory;
using EforTakip.Application.Directories.Ldap;
using EforTakip.Domain.Directories;
using EforTakip.Domain.Exceptions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Directory = EforTakip.Domain.Directories.Directory;

namespace EforTakip.Application.Tests.Directories.Commands;

public class SyncDirectoryCommandHandlerTests : IAsyncDisposable
{
    private readonly IRepository<Directory> _directoryRepository = Substitute.For<IRepository<Directory>>();
    private readonly ILdapService _ldapService = Substitute.For<ILdapService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly TestDbContext _db;

    public SyncDirectoryCommandHandlerTests()
    {
        _db = CreateDb();

        // IUnitOfWork gerçek context'e delege edilir; aksi halde handler'ın eklediği
        // varlıklar kaydedilmez ve DbSet sorgularında görünmez.
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(callInfo => _db.SaveChangesAsync(callInfo.Arg<CancellationToken>()));
    }

    public async ValueTask DisposeAsync() => await _db.DisposeAsync();

    private SyncDirectoryCommandHandler CreateHandler()
        => new(_db, _directoryRepository, _ldapService, _unitOfWork);

    private static Directory ValidAd() =>
        Directory.CreateActiveDirectory(
            "Kızılay AD", "Microsoft Active Directory", "kizilay.local", 389, false, "u", "p",
            "DC=kizilay,DC=local", null, null, DirectoryPermission.ReadOnly, "user", "(x)",
            "sAMAccountName", "cn", "givenName", "sn", "displayName", "mail", "objectGUID",
            SyncScheduleKind.Daily, 0);

    private static LdapUser LdapUserOf(
        string username, string guid, string? company = null, bool isEnabled = true) =>
        new(username, "Serkan", "Gültepe", "Serkan Gültepe", $"{username}@kizilay.org.tr", guid, isEnabled,
            company is null
                ? new Dictionary<string, string?>()
                : new Dictionary<string, string?> { ["company"] = company });

    /// <summary>InMemory context — mock'lanan DbSet async LINQ desteklemediği için gerçek EF context kullanılır.</summary>
    private static TestDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase($"sync-tests-{Guid.NewGuid()}")
            .Options;
        return new TestDbContext(options);
    }

    [Fact]
    public async Task Handle_NewUsersFromLdap_AddsThem()
    {
        var directory = ValidAd();
        _directoryRepository.GetByIdAsync(directory.Id, Arg.Any<CancellationToken>()).Returns(directory);
        _ldapService.SearchUsersAsync(directory, Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<LdapUser> { LdapUserOf("serkan.gultepe", "guid-1") });

        var result = await CreateHandler().Handle(new SyncDirectoryCommand(directory.Id), CancellationToken.None);

        result.Added.Should().Be(1);
        result.Updated.Should().Be(0);
        result.Deactivated.Should().Be(0);
        result.TotalFromDirectory.Should().Be(1);
        _db.DirectoryUsers.Should().ContainSingle(u => u.Username == "serkan.gultepe");
    }

    [Fact]
    public async Task Handle_ExistingUser_UpdatesInsteadOfDuplicating()
    {
        var directory = ValidAd();
        var existing = DirectoryUser.CreateFromActiveDirectory(
            directory.Id, "serkan.gultepe", "Serkan", "Eski", "Eski Ad", "eski@x.com", "guid-1");
        _db.DirectoryUsers.Add(existing);
        await _db.SaveChangesAsync();

        _directoryRepository.GetByIdAsync(directory.Id, Arg.Any<CancellationToken>()).Returns(directory);
        _ldapService.SearchUsersAsync(directory, Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<LdapUser> { LdapUserOf("serkan.gultepe", "guid-1") });

        var result = await CreateHandler().Handle(new SyncDirectoryCommand(directory.Id), CancellationToken.None);

        result.Added.Should().Be(0);
        result.Updated.Should().Be(1);
        _db.DirectoryUsers.Should().ContainSingle();
        _db.DirectoryUsers.Single().LastName.Should().Be("Gültepe");
    }

    [Fact]
    public async Task Handle_UserMissingFromLdap_IsDeactivatedNotDeleted()
    {
        var directory = ValidAd();
        var stale = DirectoryUser.CreateFromActiveDirectory(
            directory.Id, "ayrilan.kullanici", "Ayrılan", "Kullanıcı", "Ayrılan", null, "guid-eski");
        _db.DirectoryUsers.Add(stale);
        await _db.SaveChangesAsync();

        _directoryRepository.GetByIdAsync(directory.Id, Arg.Any<CancellationToken>()).Returns(directory);
        _ldapService.SearchUsersAsync(directory, Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<LdapUser>());

        var result = await CreateHandler().Handle(new SyncDirectoryCommand(directory.Id), CancellationToken.None);

        result.Deactivated.Should().Be(1);
        _db.DirectoryUsers.Should().ContainSingle();
        _db.DirectoryUsers.Single().IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_UserDisabledInDirectory_IsStoredAsInactive()
    {
        var directory = ValidAd();
        _directoryRepository.GetByIdAsync(directory.Id, Arg.Any<CancellationToken>()).Returns(directory);
        _ldapService.SearchUsersAsync(directory, Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<LdapUser> { LdapUserOf("pasif.kullanici", "guid-2", isEnabled: false) });

        var result = await CreateHandler().Handle(new SyncDirectoryCommand(directory.Id), CancellationToken.None);

        result.Added.Should().Be(1);
        _db.DirectoryUsers.Single().IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ExistingUserDisabledInDirectory_BecomesInactive()
    {
        var directory = ValidAd();
        var existing = DirectoryUser.CreateFromActiveDirectory(
            directory.Id, "serkan.gultepe", "Serkan", "Gültepe", "Serkan Gültepe", null, "guid-1");
        _db.DirectoryUsers.Add(existing);
        await _db.SaveChangesAsync();

        _directoryRepository.GetByIdAsync(directory.Id, Arg.Any<CancellationToken>()).Returns(directory);
        _ldapService.SearchUsersAsync(directory, Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<LdapUser> { LdapUserOf("serkan.gultepe", "guid-1", isEnabled: false) });

        await CreateHandler().Handle(new SyncDirectoryCommand(directory.Id), CancellationToken.None);

        _db.DirectoryUsers.Single().IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithSyncedMapping_WritesAttributeValue()
    {
        var directory = ValidAd();
        var mapping = DirectoryAttributeMapping.Create("company", "Kurum", "text", isSynced: true, 0);
        var ignored = DirectoryAttributeMapping.Create("department", "Departman", "text", isSynced: false, 1);
        _db.DirectoryAttributeMappings.AddRange(mapping, ignored);
        await _db.SaveChangesAsync();

        _directoryRepository.GetByIdAsync(directory.Id, Arg.Any<CancellationToken>()).Returns(directory);
        _ldapService.SearchUsersAsync(directory, Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<LdapUser> { LdapUserOf("serkan.gultepe", "guid-1", company: "Kızılay") });

        await CreateHandler().Handle(new SyncDirectoryCommand(directory.Id), CancellationToken.None);

        var user = _db.DirectoryUsers.Include(u => u.Attributes).Single();
        user.Attributes.Should().ContainSingle();
        user.Attributes.Single().AttributeMappingId.Should().Be(mapping.Id);
        user.Attributes.Single().Value.Should().Be("Kızılay");
    }

    [Fact]
    public async Task Handle_OnlySyncedMappingsAreRequestedFromLdap()
    {
        var directory = ValidAd();
        _db.DirectoryAttributeMappings.AddRange(
            DirectoryAttributeMapping.Create("company", "Kurum", "text", isSynced: true, 0),
            DirectoryAttributeMapping.Create("department", "Departman", "text", isSynced: false, 1));
        await _db.SaveChangesAsync();

        _directoryRepository.GetByIdAsync(directory.Id, Arg.Any<CancellationToken>()).Returns(directory);
        _ldapService.SearchUsersAsync(directory, Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<LdapUser>());

        await CreateHandler().Handle(new SyncDirectoryCommand(directory.Id), CancellationToken.None);

        await _ldapService.Received(1).SearchUsersAsync(
            directory,
            Arg.Is<IReadOnlyCollection<string>>(names => names.Count == 1 && names.Contains("company")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MarksDirectoryAsSynced()
    {
        var directory = ValidAd();
        _directoryRepository.GetByIdAsync(directory.Id, Arg.Any<CancellationToken>()).Returns(directory);
        _ldapService.SearchUsersAsync(directory, Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<LdapUser>());

        await CreateHandler().Handle(new SyncDirectoryCommand(directory.Id), CancellationToken.None);

        directory.LastSyncedUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_InternalDirectory_ThrowsBusinessRule()
    {
        var directory = Directory.CreateInternal("Internal Users", 0);
        _directoryRepository.GetByIdAsync(directory.Id, Arg.Any<CancellationToken>()).Returns(directory);

        var act = async () => await CreateHandler().Handle(
            new SyncDirectoryCommand(directory.Id), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleValidationException>();
    }

    [Fact]
    public async Task Handle_NonExistingDirectory_ThrowsNotFound()
    {
        _directoryRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Directory?)null);

        var act = async () => await CreateHandler().Handle(
            new SyncDirectoryCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
```

- [ ] **Step 4: Test için IApplicationDbContext'i uygulayan InMemory context'i yaz**

`backend/tests/EforTakip.Application.Tests/Directories/Commands/TestDbContext.cs`:
```csharp
using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Activities;
using EforTakip.Domain.Customers;
using EforTakip.Domain.Directories;
using EforTakip.Domain.EmployeeLeaves;
using EforTakip.Domain.Employees;
using EforTakip.Domain.Holidays;
using EforTakip.Domain.Notifications;
using EforTakip.Domain.Projects;
using EforTakip.Domain.ValueStreams;
using EforTakip.Domain.WorkCalendars;
using EforTakip.Domain.WorkLogApprovals;
using EforTakip.Domain.WorkLogs;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.Tests.Directories.Commands;

/// <summary>
/// Senkronizasyon testleri için gerçek EF Core InMemory context'i. NSubstitute ile mock'lanan
/// DbSet'ler async LINQ (AnyAsync/ToListAsync) desteklemediğinden gerçek context kullanılır.
/// </summary>
public sealed class TestDbContext(DbContextOptions<TestDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectCustomerAssignment> ProjectCustomerAssignments => Set<ProjectCustomerAssignment>();
    public DbSet<ProjectEmployeeAssignment> ProjectEmployeeAssignments => Set<ProjectEmployeeAssignment>();
    public DbSet<ValueStream> ValueStreams => Set<ValueStream>();
    public DbSet<ValueStreamStage> ValueStreamStages => Set<ValueStreamStage>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<StageActivityAssignment> StageActivityAssignments => Set<StageActivityAssignment>();
    public DbSet<EmployeeWorkLog> EmployeeWorkLogs => Set<EmployeeWorkLog>();
    public DbSet<Holiday> Holidays => Set<Holiday>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<WorkCalendar> WorkCalendars => Set<WorkCalendar>();
    public DbSet<WorkCalendarDay> WorkCalendarDays => Set<WorkCalendarDay>();
    public DbSet<WorkLogApproval> WorkLogApprovals => Set<WorkLogApproval>();
    public DbSet<EmployeeLeave> EmployeeLeaves => Set<EmployeeLeave>();
    public DbSet<Domain.Directories.Directory> Directories => Set<Domain.Directories.Directory>();
    public DbSet<DirectoryUser> DirectoryUsers => Set<DirectoryUser>();
    public DbSet<DirectoryAttributeMapping> DirectoryAttributeMappings => Set<DirectoryAttributeMapping>();
    public DbSet<DirectoryUserAttribute> DirectoryUserAttributes => Set<DirectoryUserAttribute>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DirectoryUser>()
            .HasMany(u => u.Attributes)
            .WithOne()
            .HasForeignKey(a => a.DirectoryUserId);

        modelBuilder.Entity<DirectoryUser>()
            .Metadata
            .FindNavigation(nameof(DirectoryUser.Attributes))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        base.OnModelCreating(modelBuilder);
    }
}
```

Test projesinde EF Core InMemory paketi yoksa ekle:
```bash
dotnet add backend/tests/EforTakip.Application.Tests/EforTakip.Application.Tests.csproj \
  package Microsoft.EntityFrameworkCore.InMemory --version 8.0.11
```

- [ ] **Step 5: Testleri çalıştır, başarısız olduğunu doğrula**

Run: `dotnet test backend/tests/EforTakip.Application.Tests/EforTakip.Application.Tests.csproj --filter SyncDirectory`
Expected: FAIL — `SyncDirectoryCommandHandler` bulunamıyor.

- [ ] **Step 6: Handler'ı yaz**

`SyncDirectoryCommandHandler.cs`:
```csharp
using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Directories.Dtos;
using EforTakip.Application.Directories.Ldap;
using EforTakip.Domain.Directories;
using EforTakip.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Directory = EforTakip.Domain.Directories.Directory;

namespace EforTakip.Application.Directories.Commands.SyncDirectory;

public sealed class SyncDirectoryCommandHandler(
    IApplicationDbContext db,
    IRepository<Directory> directoryRepository,
    ILdapService ldapService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<SyncDirectoryCommand, DirectorySyncResultDto>
{
    public async Task<DirectorySyncResultDto> Handle(
        SyncDirectoryCommand request, CancellationToken cancellationToken)
    {
        var directory = await directoryRepository.GetByIdAsync(request.DirectoryId, cancellationToken)
            ?? throw new NotFoundException(nameof(Directory), request.DirectoryId);

        if (directory.Source != DirectorySource.ActiveDirectory)
            throw new BusinessRuleValidationException("Yalnızca Active Directory dizinleri senkronize edilebilir.");

        var syncedMappings = await db.DirectoryAttributeMappings
            .Where(m => m.IsSynced)
            .ToListAsync(cancellationToken);

        var extraAttributeNames = syncedMappings
            .Select(m => m.AdAttributeName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var ldapUsers = await ldapService.SearchUsersAsync(directory, extraAttributeNames, cancellationToken);

        var existingUsers = await db.DirectoryUsers
            .Include(u => u.Attributes)
            .Where(u => u.DirectoryId == directory.Id)
            .ToListAsync(cancellationToken);

        var byObjectGuid = existingUsers
            .Where(u => u.ObjectGuid is not null)
            .ToDictionary(u => u.ObjectGuid!, StringComparer.OrdinalIgnoreCase);

        var syncedAtUtc = DateTime.UtcNow;
        var seenObjectGuids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var added = 0;
        var updated = 0;

        foreach (var ldapUser in ldapUsers)
        {
            seenObjectGuids.Add(ldapUser.ObjectGuid);

            if (byObjectGuid.TryGetValue(ldapUser.ObjectGuid, out var existing))
            {
                existing.UpdateFromSync(
                    ldapUser.FirstName, ldapUser.LastName, ldapUser.DisplayName, ldapUser.Email,
                    ldapUser.IsEnabled, syncedAtUtc);
                ApplyAttributes(existing, ldapUser, syncedMappings);
                updated++;
            }
            else
            {
                var created = DirectoryUser.CreateFromActiveDirectory(
                    directory.Id, ldapUser.Username, ldapUser.FirstName, ldapUser.LastName,
                    ldapUser.DisplayName, ldapUser.Email, ldapUser.ObjectGuid);
                if (!ldapUser.IsEnabled)
                    created.Deactivate();
                ApplyAttributes(created, ldapUser, syncedMappings);
                db.DirectoryUsers.Add(created);
                added++;
            }
        }

        // Dizinde artık bulunmayan kullanıcılar silinmez, yalnızca pasife alınır.
        var deactivated = 0;
        foreach (var user in existingUsers)
        {
            if (user.ObjectGuid is not null && seenObjectGuids.Contains(user.ObjectGuid))
                continue;
            if (!user.IsActive)
                continue;

            user.Deactivate();
            deactivated++;
        }

        directory.MarkSynced(syncedAtUtc);
        directoryRepository.Update(directory);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new DirectorySyncResultDto
        {
            DirectoryId = directory.Id,
            DirectoryName = directory.Name,
            Added = added,
            Updated = updated,
            Deactivated = deactivated,
            TotalFromDirectory = ldapUsers.Count,
            SyncedAtUtc = syncedAtUtc
        };
    }

    private static void ApplyAttributes(
        DirectoryUser user, LdapUser ldapUser, IReadOnlyCollection<DirectoryAttributeMapping> mappings)
    {
        foreach (var mapping in mappings)
        {
            ldapUser.Attributes.TryGetValue(mapping.AdAttributeName, out var value);
            user.SetAttribute(mapping.Id, value);
        }
    }
}
```

- [ ] **Step 7: Testleri çalıştır, geçtiğini doğrula**

Run: `dotnet test backend/tests/EforTakip.Application.Tests/EforTakip.Application.Tests.csproj --filter SyncDirectory`
Expected: PASS (8 test).

**Not:** `IUnitOfWork` mock'u test sınıfının kurucusunda gerçek `TestDbContext.SaveChangesAsync`'e delege edilir. Bu olmadan handler'ın `db.DirectoryUsers.Add(...)` ile eklediği varlıklar kaydedilmez ve sorgularda görünmezdi — testler yanlışlıkla başarısız olurdu.

- [ ] **Step 8: Commit**

```bash
git add backend/src/EforTakip.Application/Directories/ backend/tests/EforTakip.Application.Tests/Directories/
git commit -m "feat: add directory synchronization command"
```

---

## Task 7: DirectoryUser sorguları

**Files:**
- Create: `backend/src/EforTakip.Application/Directories/Dtos/DirectoryUserDto.cs`
- Create: `backend/src/EforTakip.Application/Directories/Dtos/DirectoryUserDetailDto.cs`
- Create: `backend/src/EforTakip.Application/Directories/Queries/GetDirectoryUsers/GetDirectoryUsersQuery.cs`
- Create: `backend/src/EforTakip.Application/Directories/Queries/GetDirectoryUsers/GetDirectoryUsersQueryHandler.cs`
- Create: `backend/src/EforTakip.Application/Directories/Queries/GetDirectoryUserById/GetDirectoryUserByIdQuery.cs`
- Create: `backend/src/EforTakip.Application/Directories/Queries/GetDirectoryUserById/GetDirectoryUserByIdQueryHandler.cs`

**Interfaces:**
- Consumes: `IApplicationDbContext`, `PaginationParams`, `PagedResult<T>`, `NotFoundException`.
- Produces:
  - `DirectoryUserDto { Id, DirectoryId, DirectoryName, Source, Username, FirstName, LastName, DisplayName, Email, IsActive, LastSyncedUtc }`
  - `DirectoryUserAttributeValueDto { SystemFieldName, AdAttributeName, FieldType, Value }`
  - `DirectoryUserDetailDto` — `DirectoryUserDto` alanları + `IReadOnlyCollection<DirectoryUserAttributeValueDto> Attributes`
  - `GetDirectoryUsersQuery : PaginationParams, IRequest<PagedResult<DirectoryUserDto>>` — `Guid? DirectoryId`, `string? SearchTerm`, `bool? OnlyActive`
  - `GetDirectoryUserByIdQuery(Guid DirectoryUserId) : IRequest<DirectoryUserDetailDto>`

**Not:** `PasswordHash` hiçbir DTO'da yer almaz.

- [ ] **Step 1: DTO'ları yaz**

`DirectoryUserDto.cs`:
```csharp
using EforTakip.Domain.Directories;

namespace EforTakip.Application.Directories.Dtos;

/// <summary>Şifre hash'i bilinçli olarak yer almaz.</summary>
public sealed class DirectoryUserDto
{
    public Guid Id { get; init; }
    public Guid DirectoryId { get; init; }
    public string DirectoryName { get; init; } = default!;
    public DirectorySource Source { get; init; }
    public string Username { get; init; } = default!;
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? DisplayName { get; init; }
    public string? Email { get; init; }
    public bool IsActive { get; init; }
    public DateTime? LastSyncedUtc { get; init; }
}
```

`DirectoryUserDetailDto.cs`:
```csharp
using EforTakip.Domain.Directories;

namespace EforTakip.Application.Directories.Dtos;

public sealed class DirectoryUserAttributeValueDto
{
    public string SystemFieldName { get; init; } = default!;
    public string AdAttributeName { get; init; } = default!;
    public string FieldType { get; init; } = default!;
    public string? Value { get; init; }
}

/// <summary>Kullanıcı kartı için tüm senkronize attribute'larla birlikte kullanıcı bilgisi.</summary>
public sealed class DirectoryUserDetailDto
{
    public Guid Id { get; init; }
    public Guid DirectoryId { get; init; }
    public string DirectoryName { get; init; } = default!;
    public DirectorySource Source { get; init; }
    public string Username { get; init; } = default!;
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? DisplayName { get; init; }
    public string? Email { get; init; }
    public bool IsActive { get; init; }
    public DateTime? LastSyncedUtc { get; init; }
    public IReadOnlyCollection<DirectoryUserAttributeValueDto> Attributes { get; init; } = [];
}
```

- [ ] **Step 2: GetDirectoryUsers query + handler yaz**

`GetDirectoryUsersQuery.cs`:
```csharp
using EforTakip.Application.Common.Models;
using EforTakip.Application.Directories.Dtos;
using MediatR;

namespace EforTakip.Application.Directories.Queries.GetDirectoryUsers;

public sealed class GetDirectoryUsersQuery : PaginationParams, IRequest<PagedResult<DirectoryUserDto>>
{
    public Guid? DirectoryId { get; set; }
    public string? SearchTerm { get; set; }
    public bool? OnlyActive { get; set; }
}
```

`GetDirectoryUsersQueryHandler.cs`:
```csharp
using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Common.Models;
using EforTakip.Application.Directories.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.Directories.Queries.GetDirectoryUsers;

public sealed class GetDirectoryUsersQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetDirectoryUsersQuery, PagedResult<DirectoryUserDto>>
{
    public async Task<PagedResult<DirectoryUserDto>> Handle(
        GetDirectoryUsersQuery request, CancellationToken cancellationToken)
    {
        var query = db.DirectoryUsers.AsNoTracking();

        if (request.DirectoryId is { } directoryId)
            query = query.Where(u => u.DirectoryId == directoryId);

        if (request.OnlyActive == true)
            query = query.Where(u => u.IsActive);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower();
            query = query.Where(u =>
                u.Username.ToLower().Contains(term) ||
                (u.DisplayName != null && u.DisplayName.ToLower().Contains(term)) ||
                (u.Email != null && u.Email.ToLower().Contains(term)));
        }

        query = query.OrderBy(u => u.Username);

        var totalCount = await query.CountAsync(cancellationToken);

        // Dizin adı için join — N+1 sorgusundan kaçınmak amacıyla tek sorguda projekte edilir.
        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Join(db.Directories, u => u.DirectoryId, d => d.Id, (u, d) => new DirectoryUserDto
            {
                Id = u.Id,
                DirectoryId = u.DirectoryId,
                DirectoryName = d.Name,
                Source = u.Source,
                Username = u.Username,
                FirstName = u.FirstName,
                LastName = u.LastName,
                DisplayName = u.DisplayName,
                Email = u.Email,
                IsActive = u.IsActive,
                LastSyncedUtc = u.LastSyncedUtc
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<DirectoryUserDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
```

- [ ] **Step 3: GetDirectoryUserById query + handler yaz**

`GetDirectoryUserByIdQuery.cs`:
```csharp
using EforTakip.Application.Directories.Dtos;
using MediatR;

namespace EforTakip.Application.Directories.Queries.GetDirectoryUserById;

public sealed record GetDirectoryUserByIdQuery(Guid DirectoryUserId) : IRequest<DirectoryUserDetailDto>;
```

`GetDirectoryUserByIdQueryHandler.cs`:
```csharp
using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Directories.Dtos;
using EforTakip.Domain.Directories;
using EforTakip.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.Directories.Queries.GetDirectoryUserById;

public sealed class GetDirectoryUserByIdQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetDirectoryUserByIdQuery, DirectoryUserDetailDto>
{
    public async Task<DirectoryUserDetailDto> Handle(
        GetDirectoryUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await db.DirectoryUsers
            .AsNoTracking()
            .Include(u => u.Attributes)
            .FirstOrDefaultAsync(u => u.Id == request.DirectoryUserId, cancellationToken)
            ?? throw new NotFoundException(nameof(DirectoryUser), request.DirectoryUserId);

        var directoryName = await db.Directories
            .AsNoTracking()
            .Where(d => d.Id == user.DirectoryId)
            .Select(d => d.Name)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        var mappings = await db.DirectoryAttributeMappings
            .AsNoTracking()
            .OrderBy(m => m.SortOrder)
            .ToListAsync(cancellationToken);

        var valuesByMappingId = user.Attributes.ToDictionary(a => a.AttributeMappingId, a => a.Value);

        var attributes = mappings
            .Where(m => valuesByMappingId.ContainsKey(m.Id))
            .Select(m => new DirectoryUserAttributeValueDto
            {
                SystemFieldName = m.SystemFieldName,
                AdAttributeName = m.AdAttributeName,
                FieldType = m.FieldType,
                Value = valuesByMappingId[m.Id]
            })
            .ToList();

        return new DirectoryUserDetailDto
        {
            Id = user.Id,
            DirectoryId = user.DirectoryId,
            DirectoryName = directoryName,
            Source = user.Source,
            Username = user.Username,
            FirstName = user.FirstName,
            LastName = user.LastName,
            DisplayName = user.DisplayName,
            Email = user.Email,
            IsActive = user.IsActive,
            LastSyncedUtc = user.LastSyncedUtc,
            Attributes = attributes
        };
    }
}
```

- [ ] **Step 4: Derle ve testleri çalıştır**

Run: `dotnet build backend/EforTakip.sln && dotnet test backend/tests/EforTakip.Application.Tests/EforTakip.Application.Tests.csproj --filter Directory`
Expected: Build succeeded; Directory testleri PASS.

- [ ] **Step 5: Commit**

```bash
git add backend/src/EforTakip.Application/Directories/
git commit -m "feat: add directory user queries with attribute details"
```

---

## Task 8: API endpoint'leri

**Files:**
- Modify: `backend/src/EforTakip.Api/Controllers/v1/DirectoriesController.cs`
- Create: `backend/src/EforTakip.Api/Controllers/v1/DirectoryUsersController.cs`

**Interfaces:**
- Consumes: `SyncDirectoryCommand`, `TestDirectoryConnectionCommand`, `GetDirectoryUsersQuery`, `GetDirectoryUserByIdQuery` ve ilgili DTO'lar.
- Produces: `POST /api/v1/directories/{id}/sync`, `POST /api/v1/directories/{id}/test-connection`, `GET /api/v1/directoryusers`, `GET /api/v1/directoryusers/{id}`.

- [ ] **Step 1: DirectoriesController'a sync ve test endpoint'lerini ekle**

`backend/src/EforTakip.Api/Controllers/v1/DirectoriesController.cs` — using bloğuna ekle:
```csharp
using EforTakip.Application.Directories.Commands.SyncDirectory;
using EforTakip.Application.Directories.Commands.TestDirectoryConnection;
using EforTakip.Application.Directories.Ldap;
```

Ve `Delete` metodundan sonra, sınıfın kapanış `}` işaretinden önce ekle:
```csharp
    [HttpPost("{id:guid}/sync")]
    [ProducesResponseType(typeof(DirectorySyncResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DirectorySyncResultDto>> Sync(Guid id, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new SyncDirectoryCommand(id), cancellationToken));

    [HttpPost("{id:guid}/test-connection")]
    [ProducesResponseType(typeof(LdapConnectionTestResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<LdapConnectionTestResult>> TestConnection(
        Guid id, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new TestDirectoryConnectionCommand(id), cancellationToken));
```

- [ ] **Step 2: DirectoryUsersController'ı yaz**

`backend/src/EforTakip.Api/Controllers/v1/DirectoryUsersController.cs`:
```csharp
using Asp.Versioning;
using EforTakip.Application.Common.Models;
using EforTakip.Application.Directories.Dtos;
using EforTakip.Application.Directories.Queries.GetDirectoryUserById;
using EforTakip.Application.Directories.Queries.GetDirectoryUsers;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EforTakip.Api.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class DirectoryUsersController(ISender mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<DirectoryUserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<DirectoryUserDto>>> GetAll(
        [FromQuery] GetDirectoryUsersQuery query, CancellationToken cancellationToken)
        => Ok(await mediator.Send(query, cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DirectoryUserDetailDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DirectoryUserDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new GetDirectoryUserByIdQuery(id), cancellationToken));
}
```

- [ ] **Step 3: Derle**

Run: `dotnet build backend/EforTakip.sln`
Expected: Build succeeded.

- [ ] **Step 4: Swagger'da endpoint'leri doğrula**

API'yi başlat (`dotnet run --project backend/src/EforTakip.Api --urls http://localhost:5298`), sonra:
```bash
curl -s http://localhost:5298/swagger/v1/swagger.json | grep -o '"/api/v1/[Dd]irector[^"]*"' | sort -u
```
Expected: Aşağıdaki 6 route görünür:
```
"/api/v1/Directories"
"/api/v1/Directories/{id}"
"/api/v1/Directories/{id}/sync"
"/api/v1/Directories/{id}/test-connection"
"/api/v1/DirectoryUsers"
"/api/v1/DirectoryUsers/{id}"
```

- [ ] **Step 5: Bağlantı testinin hata yolunu canlı doğrula**

Bir AD dizini oluştur (Faz 1'deki POST gövdesiyle, `hostname` erişilemez bir adres olsun), sonra:
```bash
curl -s -X POST "http://localhost:5298/api/v1/directories/<ID>/test-connection"
```
Expected: `{"success":false,"message":"Sunucuya ulaşılamıyor. Adres ve port bilgisini kontrol edin."}` benzeri bir yanıt (200 OK, success=false). İç sistem detayı sızmamalı.

API'yi durdur.

- [ ] **Step 6: Commit**

```bash
git add backend/src/EforTakip.Api/Controllers/v1/
git commit -m "feat: add directory sync, connection test and user endpoints"
```

---

## Task 9: Zamanlanmış senkronizasyon (background service)

**Files:**
- Create: `backend/src/EforTakip.Infrastructure/Sync/DirectorySyncBackgroundService.cs`
- Modify: `backend/src/EforTakip.Infrastructure/DependencyInjection.cs`

**Interfaces:**
- Consumes: `IApplicationDbContext`, `ISender` (MediatR), `SyncDirectoryCommand`, `Directory.IsSyncDue`, `ILogger<T>`, `BackgroundService`.
- Produces: `DirectorySyncBackgroundService` — DI'da hosted service olarak kayıtlı.

**Davranış:** 5 dakikada bir uyanır, `IsSyncDue(nowUtc)` dönen tüm AD dizinleri için `SyncDirectoryCommand` gönderir. Bir dizinin senkronizasyonu hata verirse loglanır ve diğer dizinlere devam edilir (tek hatalı dizin tüm zamanlayıcıyı düşürmez).

- [ ] **Step 1: Background service'i yaz**

`backend/src/EforTakip.Infrastructure/Sync/DirectorySyncBackgroundService.cs`:
```csharp
using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Directories.Commands.SyncDirectory;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EforTakip.Infrastructure.Sync;

/// <summary>
/// Zamanlaması gelen Active Directory dizinlerini periyodik olarak senkronize eder.
/// Hassas veri (şifre, bind bilgisi) loglanmaz; yalnızca dizin adı ve sayısal özet yazılır.
/// </summary>
public sealed class DirectorySyncBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<DirectorySyncBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunDueSyncsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Zamanlanmış dizin senkronizasyonu turu başarısız oldu.");
            }

            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task RunDueSyncsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<ISender>();

        var nowUtc = DateTime.UtcNow;

        var candidates = await db.Directories
            .AsNoTracking()
            .Where(d => d.IsActive)
            .ToListAsync(cancellationToken);

        var dueDirectoryIds = candidates
            .Where(d => d.IsSyncDue(nowUtc))
            .Select(d => d.Id)
            .ToList();

        foreach (var directoryId in dueDirectoryIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var result = await mediator.Send(new SyncDirectoryCommand(directoryId), cancellationToken);
                logger.LogInformation(
                    "Dizin senkronizasyonu tamamlandı: {DirectoryName} — {Added} eklendi, {Updated} güncellendi, {Deactivated} pasife alındı.",
                    result.DirectoryName, result.Added, result.Updated, result.Deactivated);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Tek bir dizinin hatası diğerlerini ve zamanlayıcıyı durdurmamalı.
                logger.LogError(ex, "Dizin senkronizasyonu başarısız oldu: {DirectoryId}", directoryId);
            }
        }
    }
}
```

- [ ] **Step 2: DI kaydını ekle**

`backend/src/EforTakip.Infrastructure/DependencyInjection.cs` içeriğini değiştir:
```csharp
using EforTakip.Application.Directories.Ldap;
using EforTakip.Infrastructure.Ldap;
using EforTakip.Infrastructure.Sync;
using Microsoft.Extensions.DependencyInjection;

namespace EforTakip.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<ILdapService, LdapService>();
        services.AddHostedService<DirectorySyncBackgroundService>();

        return services;
    }
}
```

- [ ] **Step 3: Derle**

Run: `dotnet build backend/EforTakip.sln`
Expected: Build succeeded.

- [ ] **Step 4: Uygulamanın background service ile sorunsuz açıldığını doğrula**

API'yi başlat, logları kontrol et:
```bash
dotnet run --project backend/src/EforTakip.Api --urls http://localhost:5298
```
Expected: Uygulama açılır, `Application started` görünür, background service kaynaklı hata/exception yok. (Zamanlaması gelen dizin varsa senkronizasyon denenir ve LDAP'e ulaşılamadığı için hata loglanır — bu beklenen davranıştır, uygulama çalışmaya devam etmelidir.)

API'yi durdur.

- [ ] **Step 5: Tüm test suite'ini çalıştır**

Run:
```bash
dotnet test backend/tests/EforTakip.Domain.Tests/EforTakip.Domain.Tests.csproj
dotnet test backend/tests/EforTakip.Application.Tests/EforTakip.Application.Tests.csproj
```
Expected: Domain PASS; Application'da yalnızca önceden bilinen 2 `LogWorkCommandHandlerTests` hatası kalır (ayrı iş olarak takip ediliyor), Directory testlerinin tamamı geçer.

- [ ] **Step 6: Commit**

```bash
git add backend/src/EforTakip.Infrastructure/
git commit -m "feat: add scheduled directory sync background service"
```

---

## Faz 2 Tamamlanma Kriteri

- [ ] Domain testleri geçiyor (Faz 1'in 59 testi + Faz 2'nin 10 testi).
- [ ] `SyncDirectoryCommandHandlerTests` (10 test) ve `TestDirectoryConnectionCommandHandlerTests` (4 test) geçiyor.
- [ ] Dizinde devre dışı bırakılan hesap sistemde pasif görünüyor.
- [ ] `dotnet build backend/EforTakip.sln` başarılı.
- [ ] Migration oluşturuldu (`AddDirectoryUserAttributes`).
- [ ] Swagger'da 6 dizin route'u görünüyor (2 yeni komut + 2 yeni kullanıcı endpoint'i dahil).
- [ ] Bağlantı testi hata durumunda iç sistem detayı sızdırmıyor.
- [ ] Uygulama background service ile sorunsuz açılıyor; tek bir dizinin hatası servisi düşürmüyor.
- [ ] Hiçbir DTO'da `PasswordHash` veya bind şifresi yer almıyor.

## Bilinen Sınırlar (Faz 3'e devrediliyor)

- Bind şifresi hâlâ düz metin saklanıyor — `SettingsEncryptor` Faz 3'te eklenecek ve `LdapService.CreateConnection` çözülmüş şifreyi alacak.
- `LdapService` birim testi yok (gerçek LDAP sunucusu gerektirir); doğrulama canlı/manuel yapılır.
- Linux/Docker dağıtımında `System.DirectoryServices.Protocols` OpenLDAP kütüphanesine (`libldap`) ihtiyaç duyar — Docker imajına eklenmelidir (bkz. Docker dağıtım planı).

## Sonraki Fazlar

- **Faz 3 — JWT Auth + Login:** `IPasswordHasher`/`PasswordHasher` (BCrypt), `ITokenService`/`TokenService` (JWT), `SettingsEncryptor`, `LoginCommand` (internal + AD doğrulama), `AuthController`, JWT middleware.
- **Faz 4 — Frontend:** Kullanıcı Klasörü UI, global Alan Eşlemeleri bölümü, Kullanıcı Kartı, `LoginPage`, API client + react-query hook'ları.
