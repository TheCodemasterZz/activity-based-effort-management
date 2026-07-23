# Roller ve İzinler Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Operation bazlı (ekran değil) izin kataloğu, rol tanımlama, rollere izin atama ve
kullanıcılara (`DirectoryUser`) rol atama için backend + admin panel desteği kurmak; JWT'ye
izinleri gömüp policy-based authorization altyapısını (henüz mevcut controller'lara
uygulamadan) hazır hale getirmek.

**Architecture:** Mevcut Clean Architecture katmanlarına yeni bir modül eklenir. İzin kataloğu
DB'de değil kodda sabittir (`EforTakip.Domain.Authorization.Permissions` — modül bazlı nested
static class'lar, `"project:read"` gibi anahtarlar; `"project:*"` wildcard modül izni
destekler). `Role` (aggregate root) `RolePermission` (child) koleksiyonunu, `DirectoryUser`
(mevcut aggregate) `DirectoryUserRole` (child) koleksiyonunu owns eder — deseni birebir mevcut
`DirectoryUser.Attributes` desenini izler. Login'de kullanıcının tüm rollerinden toplanan
izinler ve `IsSystemAdmin` bayrağı JWT'ye claim olarak gömülür. API'da dinamik policy provider
(`Permission:<key>` policy adı → `PermissionRequirement`) ile `[RequirePermission(...)]`
attribute'u kurulur; bu attribute bu planda **hiçbir mevcut controller'a uygulanmaz** — yalnızca
yeni `RolesController`'ın kendi endpoint'lerini korur (rol yönetimi = hassas, gerisi ayrı bir
kademeli rollout kararı). Frontend'de `AdminPage.tsx`'te zaten duran "Roller ve İzinler"
placeholder sekmesi gerçek bir `RolesSection.tsx` ile doldurulur.

**Tech Stack:** .NET 8, EF Core 8 (Npgsql + InMemory), MediatR, FluentValidation, Mapster,
ASP.NET Core JWT Bearer + dinamik `IAuthorizationPolicyProvider`, xUnit, FluentAssertions,
NSubstitute, React 19, TypeScript, TanStack Query 5, Tailwind 4.

## Global Constraints

- Domain entity'leri `sealed`, private parametresiz ctor + static `Create` factory,
  `EforTakip.Domain.Common.Entity`'den türer. Aggregate root'lar `IAggregateRoot` implement eder;
  child entity'ler (`RolePermission`, `DirectoryUserRole`) etmez (bkz. `DirectoryUserAttribute`).
  Bu plan boyunca kullanılan tüm entity/DTO/handler tipleri Task 1-9'da tanımlanır; sonraki
  task'lar bunları olduğu gibi kullanır.
- İş kuralı ihlalinde `BusinessRuleValidationException`, bulunamayanda `NotFoundException`
  fırlatılır (`EforTakip.Domain.Exceptions`).
- Owned child koleksiyonlara (EF backing field, `PropertyAccessMode.Field`) yeni öğe eklerken
  **koleksiyon önceden `Include` ile yüklenmemişse** üye metod (`GrantPermission`,
  `AssignRole` vb.) mevcut öğeyi bulamaz ve yinelenen satır eklenmeye çalışılabilir — bu yüzden
  bu tür komutlarda entity `IRepository<T>.GetByIdAsync` (düz `Find`) yerine
  `IApplicationDbContext` üzerinden `.Include(...)` ile yüklenir; üye metod yeni oluşturulan
  child'ı geri döndürür ve çağıran taraf onu context'e açıkça ekler (bkz.
  `DirectoryUser.SetAttribute` / `SyncDirectoryCommandHandler` deseni).
- Command/Query = `sealed record` (parametresiz olanlar `sealed record ... : IRequest` şeklinde
  parametresiz de olabilir); Handler = primary constructor'lı `sealed class`. Validator =
  `AbstractValidator<T>`, Türkçe hata mesajları.
- EF Configuration = `IEntityTypeConfiguration<T>`, `ToTable(...)`, `HasKey(...)`.
- Controller = `[ApiController]`, `[ApiVersion("1.0")]`,
  `[Route("api/v{version:apiVersion}/[controller]")]`, `ISender mediator` primary ctor.
- Async LINQ (`Include`/`FirstOrDefaultAsync`/`AnyAsync`) kullanan handler testleri NSubstitute
  ile mock'lanan `IApplicationDbContext.DbSet` üzerinde **çalışmaz** (`NotSupportedException`) —
  bu testler mevcut `backend/tests/EforTakip.Application.Tests/Directories/Commands/TestDbContext.cs`
  (gerçek EF Core InMemory context) kullanır, `SyncDirectoryCommandHandlerTests` /
  `LoginCommandHandlerTests` desenini izler.
- Migration dosyaları `dotnet ef migrations add` ile üretilir ve `dotnet ef database update` ile
  yerel veritabanına uygulanır (bkz. `CONTRIBUTING.md`) — Neon'a asla bağlanılmaz.
- Tüm kullanıcıya dönük metinler Türkçe.
- Bu planda hiçbir mevcut controller'a (`ProjectsController`, `WorkLogsController` vb.)
  `[RequirePermission]` **eklenmez**. Sadece yeni `RolesController` korunur. Mevcut davranışta
  regresyon yoktur — herkes hâlâ giriş yaptıktan sonra eskisi gibi her endpoint'e erişir; tek
  fark, rol yönetimi artık `role:manage`/`role:read` iznini (veya `IsSystemAdmin`'i) gerektirir.

---

## Dosya Yapısı

**Domain (`backend/src/EforTakip.Domain/`):**
- `Authorization/Permissions.cs` — izin kataloğu (yeni)
- `Roles/Role.cs`, `Roles/RolePermission.cs` — yeni
- `Directories/DirectoryUserRole.cs` — yeni
- `Directories/DirectoryUser.cs` — değiştir (rol koleksiyonu + metotlar)

**Persistence (`backend/src/EforTakip.Persistence/`):**
- `Configurations/RoleConfiguration.cs`, `Configurations/RolePermissionConfiguration.cs`,
  `Configurations/DirectoryUserRoleConfiguration.cs` — yeni
- `Configurations/DirectoryUserConfiguration.cs` — değiştir (Roles navigation)
- `EforTakipDbContext.cs`, `DependencyInjection.cs` — değiştir
- `Seed/BootstrapAdminSeeder.cs` — değiştir (sistem yöneticisi rolü)
- `EforTakip.Application/Common/Interfaces/IApplicationDbContext.cs` — değiştir

**Application (`backend/src/EforTakip.Application/`):**
- `Roles/Dtos/RoleDto.cs`, `Roles/Dtos/RoleDetailDto.cs`, `Roles/Dtos/RoleAssignedUserDto.cs`
- `Roles/RoleMappingConfig.cs`
- `Roles/Commands/CreateRole/{Command,Handler,Validator}.cs`
- `Roles/Commands/UpdateRole/{Command,Handler,Validator}.cs`
- `Roles/Commands/DeleteRole/{Command,Handler}.cs`
- `Roles/Commands/GrantPermission/{Command,Handler,Validator}.cs`
- `Roles/Commands/RevokePermission/{Command,Handler,Validator}.cs`
- `Roles/Commands/AssignRoleToUser/{Command,Handler,Validator}.cs`
- `Roles/Commands/RemoveRoleFromUser/{Command,Handler}.cs`
- `Roles/Queries/GetRoles/{Query,Handler}.cs`
- `Roles/Queries/GetRoleById/{Query,Handler}.cs`
- `Roles/Queries/GetPermissionCatalog/{Query,Handler}.cs`
- `Auth/Commands/Login/LoginCommandHandler.cs` — değiştir
- `Common/Models/AuthenticatedUser.cs` — değiştir

**Infrastructure (`backend/src/EforTakip.Infrastructure/`):**
- `Security/JwtTokenService.cs` — değiştir

**API (`backend/src/EforTakip.Api/`):**
- `Authorization/PermissionRequirement.cs`, `Authorization/RequirePermissionAttribute.cs`,
  `Authorization/PermissionAuthorizationHandler.cs`, `Authorization/PermissionPolicyProvider.cs`
- `Contracts/Roles/UpdateRoleRequestBody.cs`
- `Controllers/v1/RolesController.cs`
- `Extensions/ApiServiceCollectionExtensions.cs` — değiştir

**Tests:**
- `backend/tests/EforTakip.Domain.Tests/Roles/RoleTests.cs`
- `backend/tests/EforTakip.Domain.Tests/Directories/DirectoryUserTests.cs` — değiştir (yeni testler eklenir)
- `backend/tests/EforTakip.Application.Tests/Roles/Commands/*Tests.cs`
- `backend/tests/EforTakip.Application.Tests/Auth/LoginCommandHandlerTests.cs` — değiştir
- `backend/tests/EforTakip.Application.Tests/Directories/Commands/TestDbContext.cs` — değiştir

**Frontend (`frontend/src/`):**
- `api/roles.ts`, `hooks/useRoles.ts`
- `api/types.ts` — değiştir (RoleDto, RoleDetailDto, RoleAssignedUserDto)
- `components/admin/roles/RolesSection.tsx`
- `pages/AdminPage.tsx` — değiştir

**Diğer:**
- `CLAUDE.md` — değiştir (yeni feature = 2 adım kuralı)

---

## Task 1: İzin kataloğu (`Permissions.cs`)

**Files:**
- Create: `backend/src/EforTakip.Domain/Authorization/Permissions.cs`
- Test: `backend/tests/EforTakip.Domain.Tests/Authorization/PermissionsTests.cs`

**Interfaces:**
- Produces: `Permissions` static class, nested modül sınıfları (`Permissions.Role.Read`,
  `Permissions.Role.Manage`, `Permissions.Project.*`, `Permissions.WorkLog.*`,
  `Permissions.Directory.Manage`, ...), `Permissions.All: IReadOnlyCollection<string>`,
  `Permissions.IsValidGrant(string key): bool` — Task 2 (`Role.HasPermission` wildcard mantığı),
  Task 7 (Grant/Revoke validator'ları) ve API authorization handler'ı (Task 11) bunu kullanır.

- [ ] **Step 1: Testi yaz**

`backend/tests/EforTakip.Domain.Tests/Authorization/PermissionsTests.cs`:

```csharp
using EforTakip.Domain.Authorization;
using FluentAssertions;

namespace EforTakip.Domain.Tests.Authorization;

public class PermissionsTests
{
    [Fact]
    public void All_ContainsKnownPermissions()
    {
        Permissions.All.Should().Contain(Permissions.Role.Manage);
        Permissions.All.Should().Contain(Permissions.Project.Delete);
        Permissions.All.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void IsValidGrant_ExactCatalogKey_ReturnsTrue()
    {
        Permissions.IsValidGrant(Permissions.Project.Read).Should().BeTrue();
    }

    [Fact]
    public void IsValidGrant_ValidModuleWildcard_ReturnsTrue()
    {
        Permissions.IsValidGrant("project:*").Should().BeTrue();
    }

    [Theory]
    [InlineData("project:uçmak")]
    [InlineData("olmayanmodul:*")]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValidGrant_UnknownOrEmptyKey_ReturnsFalse(string key)
    {
        Permissions.IsValidGrant(key).Should().BeFalse();
    }
}
```

- [ ] **Step 2: Testi çalıştır, başarısız olduğunu doğrula**

Run: `dotnet test backend/tests/EforTakip.Domain.Tests/EforTakip.Domain.Tests.csproj --filter PermissionsTests`
Expected: FAIL — `EforTakip.Domain.Authorization` namespace'i / `Permissions` tipi bulunamaz, derleme hatası.

- [ ] **Step 3: `Permissions.cs`'i oluştur**

```csharp
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
```

- [ ] **Step 4: Testi çalıştır, geçtiğini doğrula**

Run: `dotnet test backend/tests/EforTakip.Domain.Tests/EforTakip.Domain.Tests.csproj --filter PermissionsTests`
Expected: PASS (5 test).

- [ ] **Step 5: Commit**

```bash
git add backend/src/EforTakip.Domain/Authorization/ backend/tests/EforTakip.Domain.Tests/Authorization/
git commit -m "feat: add static permission catalog"
```

---

## Task 2: `Role` ve `RolePermission` domain entity'leri

**Files:**
- Create: `backend/src/EforTakip.Domain/Roles/RolePermission.cs`
- Create: `backend/src/EforTakip.Domain/Roles/Role.cs`
- Test: `backend/tests/EforTakip.Domain.Tests/Roles/RoleTests.cs`

**Interfaces:**
- Consumes: `Entity`, `IAggregateRoot`, `BusinessRuleValidationException` (mevcut),
  `Permissions` (Task 1, sadece testte referans için).
- Produces:
  - `Role.Create(string name, string? description, bool isSystemAdmin)` → `Role`
  - `Role.Id, Name, Description, IsSystemAdmin` (private set), `Role.Permissions: IReadOnlyCollection<RolePermission>`
  - `Role.Rename(string name)`, `Role.UpdateDescription(string? description)`
  - `Role.GrantPermission(string permissionKey): RolePermission?` (zaten varsa `null` döner — Task 7 handler'ı çağıracak)
  - `Role.RevokePermission(string permissionKey): void`
  - `Role.HasPermission(string permissionKey): bool` (tam eşleşme + `"modül:*"` wildcard + `IsSystemAdmin` bypass)
  - `RolePermission.Create(Guid roleId, string permissionKey)` → `RolePermission`, `RolePermission.RoleId`, `RolePermission.PermissionKey` (public get) — Task 4 (EF config) ve Task 7 bunları kullanır.

- [ ] **Step 1: Testi yaz**

`backend/tests/EforTakip.Domain.Tests/Roles/RoleTests.cs`:

```csharp
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.Roles;
using FluentAssertions;

namespace EforTakip.Domain.Tests.Roles;

public class RoleTests
{
    [Fact]
    public void Create_WithValidData_CreatesRole()
    {
        var role = Role.Create("Proje Yöneticisi", "Proje CRUD yetkisi", isSystemAdmin: false);

        role.Name.Should().Be("Proje Yöneticisi");
        role.Description.Should().Be("Proje CRUD yetkisi");
        role.IsSystemAdmin.Should().BeFalse();
        role.Permissions.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyName_Throws(string name)
    {
        var act = () => Role.Create(name, null, false);

        act.Should().Throw<BusinessRuleValidationException>();
    }

    [Fact]
    public void GrantPermission_NewKey_AddsAndReturnsPermission()
    {
        var role = Role.Create("Proje Yöneticisi", null, false);

        var created = role.GrantPermission("project:read");

        created.Should().NotBeNull();
        role.Permissions.Should().ContainSingle(p => p.PermissionKey == "project:read");
    }

    [Fact]
    public void GrantPermission_AlreadyGranted_ReturnsNullAndDoesNotDuplicate()
    {
        var role = Role.Create("Proje Yöneticisi", null, false);
        role.GrantPermission("project:read");

        var second = role.GrantPermission("project:read");

        second.Should().BeNull();
        role.Permissions.Should().HaveCount(1);
    }

    [Fact]
    public void RevokePermission_RemovesGrantedPermission()
    {
        var role = Role.Create("Proje Yöneticisi", null, false);
        role.GrantPermission("project:read");

        role.RevokePermission("project:read");

        role.Permissions.Should().BeEmpty();
    }

    [Fact]
    public void HasPermission_ExactMatch_ReturnsTrue()
    {
        var role = Role.Create("Proje Yöneticisi", null, false);
        role.GrantPermission("project:read");

        role.HasPermission("project:read").Should().BeTrue();
        role.HasPermission("project:delete").Should().BeFalse();
    }

    [Fact]
    public void HasPermission_ModuleWildcard_MatchesAnyKeyInModule()
    {
        var role = Role.Create("Proje Yöneticisi", null, false);
        role.GrantPermission("project:*");

        role.HasPermission("project:delete").Should().BeTrue();
        role.HasPermission("worklog:delete").Should().BeFalse();
    }

    [Fact]
    public void HasPermission_SystemAdmin_AlwaysReturnsTrue()
    {
        var role = Role.Create("Sistem Yöneticisi", null, isSystemAdmin: true);

        role.HasPermission("herhangi:birsey").Should().BeTrue();
    }

    [Fact]
    public void Rename_UpdatesName()
    {
        var role = Role.Create("Eski Ad", null, false);

        role.Rename("Yeni Ad");

        role.Name.Should().Be("Yeni Ad");
    }
}
```

- [ ] **Step 2: Testi çalıştır, başarısız olduğunu doğrula**

Run: `dotnet test backend/tests/EforTakip.Domain.Tests/EforTakip.Domain.Tests.csproj --filter RoleTests`
Expected: FAIL — `EforTakip.Domain.Roles` namespace'i bulunamaz, derleme hatası.

- [ ] **Step 3: `RolePermission.cs`'i oluştur**

```csharp
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
```

- [ ] **Step 4: `Role.cs`'i oluştur**

```csharp
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
```

- [ ] **Step 5: Testi çalıştır, geçtiğini doğrula**

Run: `dotnet test backend/tests/EforTakip.Domain.Tests/EforTakip.Domain.Tests.csproj --filter RoleTests`
Expected: PASS (9 test).

- [ ] **Step 6: Commit**

```bash
git add backend/src/EforTakip.Domain/Roles/ backend/tests/EforTakip.Domain.Tests/Roles/
git commit -m "feat: add Role and RolePermission domain entities"
```

---

## Task 3: `DirectoryUserRole` entity'si ve `DirectoryUser` rol metotları

**Files:**
- Create: `backend/src/EforTakip.Domain/Directories/DirectoryUserRole.cs`
- Modify: `backend/src/EforTakip.Domain/Directories/DirectoryUser.cs`
- Modify: `backend/tests/EforTakip.Domain.Tests/Directories/DirectoryUserTests.cs`

**Interfaces:**
- Consumes: `Entity`, `BusinessRuleValidationException` (mevcut).
- Produces:
  - `DirectoryUserRole.Create(Guid directoryUserId, Guid roleId)` → `DirectoryUserRole`,
    `DirectoryUserRole.DirectoryUserId`, `DirectoryUserRole.RoleId`, `DirectoryUserRole.AssignedAtUtc` (public get).
  - `DirectoryUser.Roles: IReadOnlyCollection<DirectoryUserRole>`
  - `DirectoryUser.AssignRole(Guid roleId): DirectoryUserRole?` (zaten atanmışsa `null`)
  - `DirectoryUser.RemoveRole(Guid roleId): void`
  — Task 4 (EF config), Task 8 (assign/remove komutları) ve Task 10 (login) bunları kullanır.

- [ ] **Step 1: `DirectoryUserTests.cs`'e yeni testler ekle**

`backend/tests/EforTakip.Domain.Tests/Directories/DirectoryUserTests.cs` dosyasının sonuna
(kapanış `}`'dan hemen önce) şunu ekle:

```csharp

    [Fact]
    public void AssignRole_NewRole_AddsAndReturnsAssignment()
    {
        var user = DirectoryUser.CreateInternal(
            Guid.NewGuid(), "kullanici", null, null, null, null, "HASH");
        var roleId = Guid.NewGuid();

        var created = user.AssignRole(roleId);

        created.Should().NotBeNull();
        user.Roles.Should().ContainSingle(r => r.RoleId == roleId);
    }

    [Fact]
    public void AssignRole_AlreadyAssigned_ReturnsNullAndDoesNotDuplicate()
    {
        var user = DirectoryUser.CreateInternal(
            Guid.NewGuid(), "kullanici", null, null, null, null, "HASH");
        var roleId = Guid.NewGuid();
        user.AssignRole(roleId);

        var second = user.AssignRole(roleId);

        second.Should().BeNull();
        user.Roles.Should().HaveCount(1);
    }

    [Fact]
    public void RemoveRole_RemovesAssignedRole()
    {
        var user = DirectoryUser.CreateInternal(
            Guid.NewGuid(), "kullanici", null, null, null, null, "HASH");
        var roleId = Guid.NewGuid();
        user.AssignRole(roleId);

        user.RemoveRole(roleId);

        user.Roles.Should().BeEmpty();
    }
```

- [ ] **Step 2: Testi çalıştır, başarısız olduğunu doğrula**

Run: `dotnet test backend/tests/EforTakip.Domain.Tests/EforTakip.Domain.Tests.csproj --filter DirectoryUserTests`
Expected: FAIL — `DirectoryUser.AssignRole` metodu yok, derleme hatası.

- [ ] **Step 3: `DirectoryUserRole.cs`'i oluştur**

```csharp
using EforTakip.Domain.Common;
using EforTakip.Domain.Exceptions;

namespace EforTakip.Domain.Directories;

public sealed class DirectoryUserRole : Entity
{
    public Guid DirectoryUserId { get; private set; }
    public Guid RoleId { get; private set; }
    public DateTime AssignedAtUtc { get; private set; }

    private DirectoryUserRole()
    {
        // EF Core
    }

    public static DirectoryUserRole Create(Guid directoryUserId, Guid roleId)
    {
        if (roleId == Guid.Empty)
            throw new BusinessRuleValidationException("Atanacak rol belirtilmelidir.");

        return new DirectoryUserRole
        {
            DirectoryUserId = directoryUserId,
            RoleId = roleId,
            AssignedAtUtc = DateTime.UtcNow
        };
    }
}
```

- [ ] **Step 4: `DirectoryUser.cs`'e rol koleksiyonu ve metotlarını ekle**

`backend/src/EforTakip.Domain/Directories/DirectoryUser.cs` içinde `_attributes` alanının
altına şunu ekle:

```csharp
    private readonly List<DirectoryUserRole> _roles = [];
    public IReadOnlyCollection<DirectoryUserRole> Roles => _roles.AsReadOnly();
```

`ClearAttributes()` metodunun hemen altına şunu ekle:

```csharp

    /// <summary>
    /// Zaten çağıranın _roles'ü önceden (Include ile) yüklemiş olması gerekir — aksi halde
    /// yinelenen kontrol her zaman "yok" der (bkz. Role.GrantPermission ile aynı desen). Yeni
    /// oluşturulan varlığı geri döner; çağıran taraf context'e açıkça eklemelidir.
    /// </summary>
    public DirectoryUserRole? AssignRole(Guid roleId)
    {
        if (_roles.Any(r => r.RoleId == roleId))
            return null;

        var created = DirectoryUserRole.Create(Id, roleId);
        _roles.Add(created);
        return created;
    }

    public void RemoveRole(Guid roleId)
    {
        var existing = _roles.FirstOrDefault(r => r.RoleId == roleId);
        if (existing is not null)
            _roles.Remove(existing);
    }
```

- [ ] **Step 5: Testi çalıştır, geçtiğini doğrula**

Run: `dotnet test backend/tests/EforTakip.Domain.Tests/EforTakip.Domain.Tests.csproj --filter DirectoryUserTests`
Expected: PASS (önceki testler + 3 yeni test).

- [ ] **Step 6: Commit**

```bash
git add backend/src/EforTakip.Domain/Directories/DirectoryUserRole.cs backend/src/EforTakip.Domain/Directories/DirectoryUser.cs backend/tests/EforTakip.Domain.Tests/Directories/DirectoryUserTests.cs
git commit -m "feat: add DirectoryUserRole and role assignment to DirectoryUser"
```

---

## Task 4: Persistence — EF konfigürasyonları, DbContext, DI, migration

**Files:**
- Create: `backend/src/EforTakip.Persistence/Configurations/RoleConfiguration.cs`
- Create: `backend/src/EforTakip.Persistence/Configurations/RolePermissionConfiguration.cs`
- Create: `backend/src/EforTakip.Persistence/Configurations/DirectoryUserRoleConfiguration.cs`
- Modify: `backend/src/EforTakip.Persistence/Configurations/DirectoryUserConfiguration.cs`
- Modify: `backend/src/EforTakip.Application/Common/Interfaces/IApplicationDbContext.cs`
- Modify: `backend/src/EforTakip.Persistence/EforTakipDbContext.cs`
- Modify: `backend/src/EforTakip.Persistence/DependencyInjection.cs`
- Modify: `backend/tests/EforTakip.Application.Tests/Directories/Commands/TestDbContext.cs`
- Create: migration dosyaları (`dotnet ef migrations add` ile üretilecek)

**Interfaces:**
- Consumes: `Role`, `RolePermission` (Task 2), `DirectoryUserRole`, `DirectoryUser.Roles` (Task 3).
- Produces: `IApplicationDbContext.Roles`, `.RolePermissions`, `.DirectoryUserRoles` — Task 5-10
  bunları kullanır. `Roles`, `RolePermissions`, `DirectoryUserRoles` tabloları + FK/unique index'ler.

- [ ] **Step 1: `RoleConfiguration.cs`'i oluştur**

```csharp
using EforTakip.Domain.Roles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EforTakip.Persistence.Configurations;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name).IsRequired().HasMaxLength(150);
        builder.Property(r => r.Description).HasMaxLength(500);

        builder.HasIndex(r => r.Name).IsUnique();

        builder.HasMany(r => r.Permissions)
            .WithOne()
            .HasForeignKey(p => p.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(Role.Permissions))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
```

- [ ] **Step 2: `RolePermissionConfiguration.cs`'i oluştur**

```csharp
using EforTakip.Domain.Roles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EforTakip.Persistence.Configurations;

public sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.PermissionKey).IsRequired().HasMaxLength(100);

        builder.HasIndex(p => new { p.RoleId, p.PermissionKey }).IsUnique();
    }
}
```

- [ ] **Step 3: `DirectoryUserRoleConfiguration.cs`'i oluştur**

```csharp
using EforTakip.Domain.Directories;
using EforTakip.Domain.Roles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EforTakip.Persistence.Configurations;

public sealed class DirectoryUserRoleConfiguration : IEntityTypeConfiguration<DirectoryUserRole>
{
    public void Configure(EntityTypeBuilder<DirectoryUserRole> builder)
    {
        builder.ToTable("DirectoryUserRoles");
        builder.HasKey(r => r.Id);

        builder.HasIndex(r => new { r.DirectoryUserId, r.RoleId }).IsUnique();

        builder.HasOne<Role>()
            .WithMany()
            .HasForeignKey(r => r.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

- [ ] **Step 4: `DirectoryUserConfiguration.cs`'e `Roles` navigation'ı ekle**

`backend/src/EforTakip.Persistence/Configurations/DirectoryUserConfiguration.cs` içinde
`HasOne<Directory>()...` bloğunun altına (kapanış `}`'dan önce) şunu ekle:

```csharp

        builder.HasMany(u => u.Roles)
            .WithOne()
            .HasForeignKey(r => r.DirectoryUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(DirectoryUser.Roles))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
```

- [ ] **Step 5: `IApplicationDbContext`'e DbSet'leri ekle**

`backend/src/EforTakip.Application/Common/Interfaces/IApplicationDbContext.cs` içinde
`using EforTakip.Domain.Projects;` satırının altına şunu ekle:

```csharp
using EforTakip.Domain.Roles;
```

`DbSet<DirectoryUserAttribute> DirectoryUserAttributes { get; }` satırının altına şunu ekle:

```csharp

    DbSet<Role> Roles { get; }

    DbSet<RolePermission> RolePermissions { get; }

    DbSet<DirectoryUserRole> DirectoryUserRoles { get; }
```

- [ ] **Step 6: `EforTakipDbContext.cs`'e aynı DbSet'leri ekle**

`backend/src/EforTakip.Persistence/EforTakipDbContext.cs` içinde
`using EforTakip.Domain.Projects;` satırının altına şunu ekle:

```csharp
using EforTakip.Domain.Roles;
```

`public DbSet<DirectoryUserAttribute> DirectoryUserAttributes => Set<DirectoryUserAttribute>();`
satırının altına şunu ekle:

```csharp

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<DirectoryUserRole> DirectoryUserRoles => Set<DirectoryUserRole>();
```

- [ ] **Step 7: `DependencyInjection.cs`'e repository kaydı ekle**

`backend/src/EforTakip.Persistence/DependencyInjection.cs` içinde
`using EforTakip.Domain.Directories;` satırının altına şunu ekle:

```csharp
using EforTakip.Domain.Roles;
```

`services.AddScoped<IRepository<DirectoryAttributeMapping>, RepositoryBase<DirectoryAttributeMapping>>();`
satırının altına şunu ekle:

```csharp
        services.AddScoped<IRepository<Role>, RepositoryBase<Role>>();
```

- [ ] **Step 8: `TestDbContext.cs`'e aynı DbSet'leri ve navigation konfigürasyonlarını ekle**

`backend/tests/EforTakip.Application.Tests/Directories/Commands/TestDbContext.cs` dosyasının
tamamını şununla değiştir:

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
using EforTakip.Domain.Roles;
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
    public DbSet<ProjectTask> ProjectTasks => Set<ProjectTask>();
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
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<DirectoryUserRole> DirectoryUserRoles => Set<DirectoryUserRole>();

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

        modelBuilder.Entity<DirectoryUser>()
            .HasMany(u => u.Roles)
            .WithOne()
            .HasForeignKey(r => r.DirectoryUserId);

        modelBuilder.Entity<DirectoryUser>()
            .Metadata
            .FindNavigation(nameof(DirectoryUser.Roles))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        modelBuilder.Entity<Role>()
            .HasMany(r => r.Permissions)
            .WithOne()
            .HasForeignKey(p => p.RoleId);

        modelBuilder.Entity<Role>()
            .Metadata
            .FindNavigation(nameof(Role.Permissions))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        base.OnModelCreating(modelBuilder);
    }
}
```

- [ ] **Step 9: Backend derlemesini doğrula**

Run: `dotnet build backend/EforTakip.sln`
Expected: Derleme başarılı, hata yok.

- [ ] **Step 10: Migration oluştur**

Run:
```bash
dotnet ef migrations add AddRolesAndPermissions \
  --project backend/src/EforTakip.Persistence \
  --startup-project backend/src/EforTakip.Api
```
Expected: `Migrations\<timestamp>_AddRolesAndPermissions.cs` ve `.Designer.cs` oluşur,
`EforTakipDbContextModelSnapshot.cs` güncellenir. `Up` metodunda `Roles`, `RolePermissions`,
`DirectoryUserRoles` tablolarının `CreateTable` ile oluşturulduğunu, `Roles.Name` üzerinde unique
index, `RolePermissions` üzerinde `(RoleId, PermissionKey)` unique index,
`DirectoryUserRoles` üzerinde `(DirectoryUserId, RoleId)` unique index ve ilgili FK'ların
(`ON DELETE CASCADE`) bulunduğunu doğrula.

- [ ] **Step 11: Migration'ı uygula**

Run: `cd backend/src/EforTakip.Api && dotnet ef database update --project ../EforTakip.Persistence && cd ../../..`
Expected: `Applying migration '..._AddRolesAndPermissions'.` ve hatasız tamamlanma.

- [ ] **Step 12: Backend derlemesini ve testleri doğrula**

Run: `dotnet build backend/EforTakip.sln && dotnet test backend/EforTakip.sln`
Expected: Derleme başarılı; tüm testler PASS (bilinen 2 önceden var olan `LogWorkCommandHandlerTests`
hatası hariç — bu görevle ilgisizdir, dokunulmaz).

- [ ] **Step 13: Commit**

```bash
git add backend/src/EforTakip.Persistence/Configurations/RoleConfiguration.cs backend/src/EforTakip.Persistence/Configurations/RolePermissionConfiguration.cs backend/src/EforTakip.Persistence/Configurations/DirectoryUserRoleConfiguration.cs backend/src/EforTakip.Persistence/Configurations/DirectoryUserConfiguration.cs backend/src/EforTakip.Application/Common/Interfaces/IApplicationDbContext.cs backend/src/EforTakip.Persistence/EforTakipDbContext.cs backend/src/EforTakip.Persistence/DependencyInjection.cs backend/src/EforTakip.Persistence/Migrations/ backend/tests/EforTakip.Application.Tests/Directories/Commands/TestDbContext.cs
git commit -m "feat: add Roles/RolePermissions/DirectoryUserRoles persistence"
```

---

## Task 5: `BootstrapAdminSeeder` — sistem yöneticisi rolünü tohumla

**Files:**
- Modify: `backend/src/EforTakip.Persistence/Seed/BootstrapAdminSeeder.cs`

**Interfaces:**
- Consumes: `Role.Create(name, description, isSystemAdmin)` (Task 2),
  `DirectoryUser.AssignRole(roleId)` (Task 3), `db.Roles`, `db.DirectoryUserRoles` (Task 4).
- Produces: İlk açılışta oluşturulan bootstrap admin artık `IsSystemAdmin = true` olan
  "Sistem Yöneticisi" rolüne sahiptir — Task 11'de authorization altyapısı devreye girdiğinde bu
  hesap kilitli kalmaz.

- [ ] **Step 1: `BootstrapAdminSeeder.cs`'i güncelle**

`backend/src/EforTakip.Persistence/Seed/BootstrapAdminSeeder.cs` dosyasının tamamını şununla
değiştir:

```csharp
using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Directories;
using EforTakip.Domain.Roles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Directory = EforTakip.Domain.Directories.Directory;

namespace EforTakip.Persistence.Seed;

/// <summary>
/// Sistemde hiç kullanıcı yokken ilk yönetici hesabını oluşturur. Endpoint'ler kimlik
/// doğrulama istediği için bu hesap olmadan kimse giriş yapıp kullanıcı oluşturamaz.
/// </summary>
public static class BootstrapAdminSeeder
{
    public const string InternalDirectoryName = "Internal Users";
    public const string SystemAdminRoleName = "Sistem Yöneticisi";

    public static async Task SeedAsync(
        EforTakipDbContext db,
        IPasswordHasher passwordHasher,
        string? username,
        string? password,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (await db.DirectoryUsers.AnyAsync(cancellationToken))
            return;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "Sistemde hiç kullanıcı yok ve Bootstrap:AdminUsername / Bootstrap:AdminPassword " +
                "tanımlı değil. Giriş yapılamayacak.");
            return;
        }

        var directory = await db.Directories
            .FirstOrDefaultAsync(d => d.Source == DirectorySource.Internal, cancellationToken);

        if (directory is null)
        {
            directory = Directory.CreateInternal(InternalDirectoryName, 0);
            db.Directories.Add(directory);
        }

        var adminRole = await db.Roles
            .FirstOrDefaultAsync(r => r.Name == SystemAdminRoleName, cancellationToken);

        if (adminRole is null)
        {
            adminRole = Role.Create(SystemAdminRoleName, "Sistemdeki tüm işlemlere erişebilir.", isSystemAdmin: true);
            db.Roles.Add(adminRole);
        }

        var admin = DirectoryUser.CreateInternal(
            directory.Id, username, null, null, username, null, passwordHasher.Hash(password));

        var assignment = admin.AssignRole(adminRole.Id);

        db.DirectoryUsers.Add(admin);
        if (assignment is not null)
            db.DirectoryUserRoles.Add(assignment);

        await db.SaveChangesAsync(cancellationToken);

        // Şifre bilinçli olarak loglanmaz.
        logger.LogInformation("İlk yönetici hesabı oluşturuldu: {Username}", admin.Username);
    }
}
```

- [ ] **Step 2: Backend derlemesini doğrula**

Run: `dotnet build backend/EforTakip.sln`
Expected: Derleme başarılı, hata yok.

- [ ] **Step 3: Commit**

```bash
git add backend/src/EforTakip.Persistence/Seed/BootstrapAdminSeeder.cs
git commit -m "feat: seed system-admin role for the bootstrap admin account"
```

---

## Task 6: Application — Role CRUD komutları (Create/Update/Delete)

**Files:**
- Create: `backend/src/EforTakip.Application/Roles/Commands/CreateRole/CreateRoleCommand.cs`
- Create: `backend/src/EforTakip.Application/Roles/Commands/CreateRole/CreateRoleCommandHandler.cs`
- Create: `backend/src/EforTakip.Application/Roles/Commands/CreateRole/CreateRoleCommandValidator.cs`
- Create: `backend/src/EforTakip.Application/Roles/Commands/UpdateRole/UpdateRoleCommand.cs`
- Create: `backend/src/EforTakip.Application/Roles/Commands/UpdateRole/UpdateRoleCommandHandler.cs`
- Create: `backend/src/EforTakip.Application/Roles/Commands/UpdateRole/UpdateRoleCommandValidator.cs`
- Create: `backend/src/EforTakip.Application/Roles/Commands/DeleteRole/DeleteRoleCommand.cs`
- Create: `backend/src/EforTakip.Application/Roles/Commands/DeleteRole/DeleteRoleCommandHandler.cs`
- Test: `backend/tests/EforTakip.Application.Tests/Roles/Commands/CreateRoleCommandHandlerTests.cs`
- Test: `backend/tests/EforTakip.Application.Tests/Roles/Commands/DeleteRoleCommandHandlerTests.cs`

**Interfaces:**
- Consumes: `Role.Create/Rename/UpdateDescription` (Task 2), `IRepository<Role>`,
  `IApplicationDbContext.Roles`, `IUnitOfWork` (mevcut).
- Produces: `CreateRoleCommand(string Name, string? Description): IRequest<Guid>`,
  `UpdateRoleCommand(Guid Id, string Name, string? Description): IRequest`,
  `DeleteRoleCommand(Guid Id): IRequest` — Task 12 (RolesController) bu tipleri kullanır.

- [ ] **Step 1: `CreateRoleCommandHandlerTests`'i yaz**

`backend/tests/EforTakip.Application.Tests/Roles/Commands/CreateRoleCommandHandlerTests.cs`:

```csharp
using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Roles.Commands.CreateRole;
using EforTakip.Application.Tests.Directories.Commands;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.Roles;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace EforTakip.Application.Tests.Roles.Commands;

public class CreateRoleCommandHandlerTests : IAsyncDisposable
{
    private readonly IRepository<Role> _repository = Substitute.For<IRepository<Role>>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly TestDbContext _db;

    public CreateRoleCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase($"create-role-tests-{Guid.NewGuid()}")
            .Options;
        _db = new TestDbContext(options);

        _repository.AddAsync(Arg.Any<Role>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                _db.Roles.Add(callInfo.Arg<Role>());
                return Task.CompletedTask;
            });
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(callInfo => _db.SaveChangesAsync(callInfo.Arg<CancellationToken>()));
    }

    public async ValueTask DisposeAsync() => await _db.DisposeAsync();

    private CreateRoleCommandHandler CreateHandler() => new(_db, _repository, _unitOfWork);

    [Fact]
    public async Task Handle_CreatesRole()
    {
        var result = await CreateHandler().Handle(
            new CreateRoleCommand("Proje Yöneticisi", "Açıklama"), CancellationToken.None);

        result.Should().NotBeEmpty();
        (await _db.Roles.FindAsync(result))!.Name.Should().Be("Proje Yöneticisi");
    }

    [Fact]
    public async Task Handle_DuplicateName_Throws()
    {
        await CreateHandler().Handle(new CreateRoleCommand("Proje Yöneticisi", null), CancellationToken.None);

        var act = async () => await CreateHandler().Handle(
            new CreateRoleCommand("Proje Yöneticisi", null), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleValidationException>();
    }
}
```

- [ ] **Step 2: Testi çalıştır, başarısız olduğunu doğrula**

Run: `dotnet test backend/tests/EforTakip.Application.Tests/EforTakip.Application.Tests.csproj --filter CreateRoleCommandHandlerTests`
Expected: FAIL — `CreateRoleCommand`/`CreateRoleCommandHandler` bulunamaz, derleme hatası.

- [ ] **Step 3: `CreateRole` komutunu, handler'ını ve validator'ını oluştur**

`backend/src/EforTakip.Application/Roles/Commands/CreateRole/CreateRoleCommand.cs`:

```csharp
using MediatR;

namespace EforTakip.Application.Roles.Commands.CreateRole;

public sealed record CreateRoleCommand(string Name, string? Description) : IRequest<Guid>;
```

`backend/src/EforTakip.Application/Roles/Commands/CreateRole/CreateRoleCommandHandler.cs`:

```csharp
using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.Roles;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.Roles.Commands.CreateRole;

public sealed class CreateRoleCommandHandler(
    IApplicationDbContext db, IRepository<Role> repository, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateRoleCommand, Guid>
{
    public async Task<Guid> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var nameTaken = await db.Roles.AnyAsync(r => r.Name == request.Name.Trim(), cancellationToken);
        if (nameTaken)
            throw new BusinessRuleValidationException($"'{request.Name}' adında bir rol zaten var.");

        // Sistem yöneticisi rolü yalnızca BootstrapAdminSeeder tarafından tohumlanır — API
        // üzerinden bir kullanıcının kendine sınırsız yetki tanımlamasının önüne geçilir.
        var role = Role.Create(request.Name, request.Description, isSystemAdmin: false);

        await repository.AddAsync(role, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return role.Id;
    }
}
```

`backend/src/EforTakip.Application/Roles/Commands/CreateRole/CreateRoleCommandValidator.cs`:

```csharp
using FluentValidation;

namespace EforTakip.Application.Roles.Commands.CreateRole;

public sealed class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Rol adı zorunludur.")
            .MaximumLength(150).WithMessage("Rol adı en fazla 150 karakter olabilir.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Açıklama en fazla 500 karakter olabilir.");
    }
}
```

- [ ] **Step 4: Testi çalıştır, geçtiğini doğrula**

Run: `dotnet test backend/tests/EforTakip.Application.Tests/EforTakip.Application.Tests.csproj --filter CreateRoleCommandHandlerTests`
Expected: PASS (2 test).

- [ ] **Step 5: `DeleteRoleCommandHandlerTests`'i yaz**

`backend/tests/EforTakip.Application.Tests/Roles/Commands/DeleteRoleCommandHandlerTests.cs`:

```csharp
using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Roles.Commands.DeleteRole;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.Roles;
using FluentAssertions;
using NSubstitute;

namespace EforTakip.Application.Tests.Roles.Commands;

public class DeleteRoleCommandHandlerTests
{
    private readonly IRepository<Role> _repository = Substitute.For<IRepository<Role>>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private DeleteRoleCommandHandler CreateHandler() => new(_repository, _unitOfWork);

    [Fact]
    public async Task Handle_ExistingRole_Removes()
    {
        var role = Role.Create("Proje Yöneticisi", null, false);
        _repository.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);

        await CreateHandler().Handle(new DeleteRoleCommand(role.Id), CancellationToken.None);

        _repository.Received(1).Remove(role);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownRole_ThrowsNotFound()
    {
        var act = async () => await CreateHandler().Handle(
            new DeleteRoleCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_SystemAdminRole_ThrowsBusinessRule()
    {
        var role = Role.Create("Sistem Yöneticisi", null, isSystemAdmin: true);
        _repository.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);

        var act = async () => await CreateHandler().Handle(new DeleteRoleCommand(role.Id), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleValidationException>();
    }
}
```

- [ ] **Step 6: Testi çalıştır, başarısız olduğunu doğrula**

Run: `dotnet test backend/tests/EforTakip.Application.Tests/EforTakip.Application.Tests.csproj --filter DeleteRoleCommandHandlerTests`
Expected: FAIL — `DeleteRoleCommand`/`DeleteRoleCommandHandler` bulunamaz, derleme hatası.

- [ ] **Step 7: `UpdateRole` ve `DeleteRole` komutlarını, handler'larını oluştur**

`backend/src/EforTakip.Application/Roles/Commands/UpdateRole/UpdateRoleCommand.cs`:

```csharp
using MediatR;

namespace EforTakip.Application.Roles.Commands.UpdateRole;

public sealed record UpdateRoleCommand(Guid Id, string Name, string? Description) : IRequest;
```

`backend/src/EforTakip.Application/Roles/Commands/UpdateRole/UpdateRoleCommandHandler.cs`:

```csharp
using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.Roles;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.Roles.Commands.UpdateRole;

public sealed class UpdateRoleCommandHandler(
    IApplicationDbContext db, IRepository<Role> repository, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateRoleCommand>
{
    public async Task Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Role), request.Id);

        var nameTaken = await db.Roles
            .AnyAsync(r => r.Id != request.Id && r.Name == request.Name.Trim(), cancellationToken);
        if (nameTaken)
            throw new BusinessRuleValidationException($"'{request.Name}' adında bir rol zaten var.");

        role.Rename(request.Name);
        role.UpdateDescription(request.Description);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
```

`backend/src/EforTakip.Application/Roles/Commands/UpdateRole/UpdateRoleCommandValidator.cs`:

```csharp
using FluentValidation;

namespace EforTakip.Application.Roles.Commands.UpdateRole;

public sealed class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Rol adı zorunludur.")
            .MaximumLength(150).WithMessage("Rol adı en fazla 150 karakter olabilir.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Açıklama en fazla 500 karakter olabilir.");
    }
}
```

`backend/src/EforTakip.Application/Roles/Commands/DeleteRole/DeleteRoleCommand.cs`:

```csharp
using MediatR;

namespace EforTakip.Application.Roles.Commands.DeleteRole;

public sealed record DeleteRoleCommand(Guid Id) : IRequest;
```

`backend/src/EforTakip.Application/Roles/Commands/DeleteRole/DeleteRoleCommandHandler.cs`:

```csharp
using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.Roles;
using MediatR;

namespace EforTakip.Application.Roles.Commands.DeleteRole;

public sealed class DeleteRoleCommandHandler(IRepository<Role> repository, IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteRoleCommand>
{
    public async Task Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Role), request.Id);

        if (role.IsSystemAdmin)
            throw new BusinessRuleValidationException("Sistem yöneticisi rolü silinemez.");

        repository.Remove(role);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
```

- [ ] **Step 8: Testleri çalıştır, geçtiğini doğrula**

Run: `dotnet test backend/tests/EforTakip.Application.Tests/EforTakip.Application.Tests.csproj --filter "CreateRoleCommandHandlerTests|DeleteRoleCommandHandlerTests"`
Expected: PASS (5 test).

- [ ] **Step 9: Commit**

```bash
git add backend/src/EforTakip.Application/Roles/Commands/CreateRole backend/src/EforTakip.Application/Roles/Commands/UpdateRole backend/src/EforTakip.Application/Roles/Commands/DeleteRole backend/tests/EforTakip.Application.Tests/Roles/Commands
git commit -m "feat: add Role create/update/delete commands"
```

---

## Task 7: Application — Grant/Revoke permission komutları

**Files:**
- Create: `backend/src/EforTakip.Application/Roles/Commands/GrantPermission/GrantPermissionCommand.cs`
- Create: `backend/src/EforTakip.Application/Roles/Commands/GrantPermission/GrantPermissionCommandHandler.cs`
- Create: `backend/src/EforTakip.Application/Roles/Commands/GrantPermission/GrantPermissionCommandValidator.cs`
- Create: `backend/src/EforTakip.Application/Roles/Commands/RevokePermission/RevokePermissionCommand.cs`
- Create: `backend/src/EforTakip.Application/Roles/Commands/RevokePermission/RevokePermissionCommandHandler.cs`
- Create: `backend/src/EforTakip.Application/Roles/Commands/RevokePermission/RevokePermissionCommandValidator.cs`
- Test: `backend/tests/EforTakip.Application.Tests/Roles/Commands/GrantPermissionCommandHandlerTests.cs`

**Interfaces:**
- Consumes: `Role.GrantPermission/RevokePermission` (Task 2), `Permissions.IsValidGrant` (Task 1),
  `IApplicationDbContext.Roles/.RolePermissions` (Task 4).
- Produces: `GrantPermissionCommand(Guid RoleId, string PermissionKey): IRequest`,
  `RevokePermissionCommand(Guid RoleId, string PermissionKey): IRequest` — Task 12 kullanır.

- [ ] **Step 1: Testi yaz**

`backend/tests/EforTakip.Application.Tests/Roles/Commands/GrantPermissionCommandHandlerTests.cs`:

```csharp
using EforTakip.Application.Roles.Commands.GrantPermission;
using EforTakip.Application.Tests.Directories.Commands;
using EforTakip.Domain.Authorization;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.Roles;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using EforTakip.Application.Common.Interfaces;

namespace EforTakip.Application.Tests.Roles.Commands;

public class GrantPermissionCommandHandlerTests : IAsyncDisposable
{
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly TestDbContext _db;

    public GrantPermissionCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase($"grant-permission-tests-{Guid.NewGuid()}")
            .Options;
        _db = new TestDbContext(options);

        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(callInfo => _db.SaveChangesAsync(callInfo.Arg<CancellationToken>()));
    }

    public async ValueTask DisposeAsync() => await _db.DisposeAsync();

    private GrantPermissionCommandHandler CreateHandler() => new(_db, _unitOfWork);

    [Fact]
    public async Task Handle_ValidPermission_GrantsIt()
    {
        var role = Role.Create("Proje Yöneticisi", null, false);
        _db.Roles.Add(role);
        await _db.SaveChangesAsync();

        await CreateHandler().Handle(
            new GrantPermissionCommand(role.Id, Permissions.Project.Read), CancellationToken.None);

        var reloaded = await _db.Roles.Include(r => r.Permissions).FirstAsync(r => r.Id == role.Id);
        reloaded.Permissions.Should().ContainSingle(p => p.PermissionKey == Permissions.Project.Read);
    }

    [Fact]
    public async Task Handle_AlreadyGranted_DoesNotDuplicate()
    {
        var role = Role.Create("Proje Yöneticisi", null, false);
        _db.Roles.Add(role);
        await _db.SaveChangesAsync();

        var handler = CreateHandler();
        await handler.Handle(new GrantPermissionCommand(role.Id, Permissions.Project.Read), CancellationToken.None);
        await handler.Handle(new GrantPermissionCommand(role.Id, Permissions.Project.Read), CancellationToken.None);

        var reloaded = await _db.Roles.Include(r => r.Permissions).FirstAsync(r => r.Id == role.Id);
        reloaded.Permissions.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_InvalidPermissionKey_Throws()
    {
        var role = Role.Create("Proje Yöneticisi", null, false);
        _db.Roles.Add(role);
        await _db.SaveChangesAsync();

        var act = async () => await CreateHandler().Handle(
            new GrantPermissionCommand(role.Id, "olmayan:izin"), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleValidationException>();
    }
}
```

- [ ] **Step 2: Testi çalıştır, başarısız olduğunu doğrula**

Run: `dotnet test backend/tests/EforTakip.Application.Tests/EforTakip.Application.Tests.csproj --filter GrantPermissionCommandHandlerTests`
Expected: FAIL — `GrantPermissionCommand`/`GrantPermissionCommandHandler` bulunamaz, derleme hatası.

- [ ] **Step 3: `GrantPermission` ve `RevokePermission` komutlarını oluştur**

`backend/src/EforTakip.Application/Roles/Commands/GrantPermission/GrantPermissionCommand.cs`:

```csharp
using MediatR;

namespace EforTakip.Application.Roles.Commands.GrantPermission;

public sealed record GrantPermissionCommand(Guid RoleId, string PermissionKey) : IRequest;
```

`backend/src/EforTakip.Application/Roles/Commands/GrantPermission/GrantPermissionCommandHandler.cs`:

```csharp
using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Authorization;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.Roles;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.Roles.Commands.GrantPermission;

public sealed class GrantPermissionCommandHandler(IApplicationDbContext db, IUnitOfWork unitOfWork)
    : IRequestHandler<GrantPermissionCommand>
{
    public async Task Handle(GrantPermissionCommand request, CancellationToken cancellationToken)
    {
        var role = await db.Roles
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken)
            ?? throw new NotFoundException(nameof(Role), request.RoleId);

        if (!Permissions.IsValidGrant(request.PermissionKey))
            throw new BusinessRuleValidationException($"'{request.PermissionKey}' geçerli bir izin anahtarı değil.");

        var created = role.GrantPermission(request.PermissionKey);
        if (created is not null)
            db.RolePermissions.Add(created);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
```

`backend/src/EforTakip.Application/Roles/Commands/GrantPermission/GrantPermissionCommandValidator.cs`:

```csharp
using FluentValidation;

namespace EforTakip.Application.Roles.Commands.GrantPermission;

public sealed class GrantPermissionCommandValidator : AbstractValidator<GrantPermissionCommand>
{
    public GrantPermissionCommandValidator()
    {
        RuleFor(x => x.PermissionKey).NotEmpty().WithMessage("İzin anahtarı zorunludur.");
    }
}
```

`backend/src/EforTakip.Application/Roles/Commands/RevokePermission/RevokePermissionCommand.cs`:

```csharp
using MediatR;

namespace EforTakip.Application.Roles.Commands.RevokePermission;

public sealed record RevokePermissionCommand(Guid RoleId, string PermissionKey) : IRequest;
```

`backend/src/EforTakip.Application/Roles/Commands/RevokePermission/RevokePermissionCommandHandler.cs`:

```csharp
using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.Roles;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.Roles.Commands.RevokePermission;

public sealed class RevokePermissionCommandHandler(IApplicationDbContext db, IUnitOfWork unitOfWork)
    : IRequestHandler<RevokePermissionCommand>
{
    public async Task Handle(RevokePermissionCommand request, CancellationToken cancellationToken)
    {
        var role = await db.Roles
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken)
            ?? throw new NotFoundException(nameof(Role), request.RoleId);

        role.RevokePermission(request.PermissionKey);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
```

`backend/src/EforTakip.Application/Roles/Commands/RevokePermission/RevokePermissionCommandValidator.cs`:

```csharp
using FluentValidation;

namespace EforTakip.Application.Roles.Commands.RevokePermission;

public sealed class RevokePermissionCommandValidator : AbstractValidator<RevokePermissionCommand>
{
    public RevokePermissionCommandValidator()
    {
        RuleFor(x => x.PermissionKey).NotEmpty().WithMessage("İzin anahtarı zorunludur.");
    }
}
```

- [ ] **Step 4: Testi çalıştır, geçtiğini doğrula**

Run: `dotnet test backend/tests/EforTakip.Application.Tests/EforTakip.Application.Tests.csproj --filter GrantPermissionCommandHandlerTests`
Expected: PASS (3 test).

- [ ] **Step 5: Commit**

```bash
git add backend/src/EforTakip.Application/Roles/Commands/GrantPermission backend/src/EforTakip.Application/Roles/Commands/RevokePermission backend/tests/EforTakip.Application.Tests/Roles/Commands/GrantPermissionCommandHandlerTests.cs
git commit -m "feat: add grant/revoke permission commands"
```

---

## Task 8: Application — kullanıcıya rol atama/kaldırma komutları

**Files:**
- Create: `backend/src/EforTakip.Application/Roles/Commands/AssignRoleToUser/AssignRoleToUserCommand.cs`
- Create: `backend/src/EforTakip.Application/Roles/Commands/AssignRoleToUser/AssignRoleToUserCommandHandler.cs`
- Create: `backend/src/EforTakip.Application/Roles/Commands/AssignRoleToUser/AssignRoleToUserCommandValidator.cs`
- Create: `backend/src/EforTakip.Application/Roles/Commands/RemoveRoleFromUser/RemoveRoleFromUserCommand.cs`
- Create: `backend/src/EforTakip.Application/Roles/Commands/RemoveRoleFromUser/RemoveRoleFromUserCommandHandler.cs`
- Test: `backend/tests/EforTakip.Application.Tests/Roles/Commands/AssignRoleToUserCommandHandlerTests.cs`

**Interfaces:**
- Consumes: `DirectoryUser.AssignRole/RemoveRole` (Task 3), `IApplicationDbContext.DirectoryUsers/.Roles/.DirectoryUserRoles` (Task 4).
- Produces: `AssignRoleToUserCommand(Guid UserId, Guid RoleId): IRequest`,
  `RemoveRoleFromUserCommand(Guid UserId, Guid RoleId): IRequest` — Task 12 kullanır.

- [ ] **Step 1: Testi yaz**

`backend/tests/EforTakip.Application.Tests/Roles/Commands/AssignRoleToUserCommandHandlerTests.cs`:

```csharp
using EforTakip.Application.Roles.Commands.AssignRoleToUser;
using EforTakip.Application.Tests.Directories.Commands;
using EforTakip.Domain.Directories;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.Roles;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using EforTakip.Application.Common.Interfaces;
using Directory = EforTakip.Domain.Directories.Directory;

namespace EforTakip.Application.Tests.Roles.Commands;

public class AssignRoleToUserCommandHandlerTests : IAsyncDisposable
{
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly TestDbContext _db;

    public AssignRoleToUserCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase($"assign-role-tests-{Guid.NewGuid()}")
            .Options;
        _db = new TestDbContext(options);

        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(callInfo => _db.SaveChangesAsync(callInfo.Arg<CancellationToken>()));
    }

    public async ValueTask DisposeAsync() => await _db.DisposeAsync();

    private AssignRoleToUserCommandHandler CreateHandler() => new(_db, _unitOfWork);

    private async Task<(DirectoryUser User, Role Role)> SeedUserAndRoleAsync()
    {
        var directory = Directory.CreateInternal("Internal Users", 0);
        var user = DirectoryUser.CreateInternal(directory.Id, "kullanici", null, null, null, null, "HASH");
        var role = Role.Create("Proje Yöneticisi", null, false);
        _db.Directories.Add(directory);
        _db.DirectoryUsers.Add(user);
        _db.Roles.Add(role);
        await _db.SaveChangesAsync();
        return (user, role);
    }

    [Fact]
    public async Task Handle_ValidUserAndRole_Assigns()
    {
        var (user, role) = await SeedUserAndRoleAsync();

        await CreateHandler().Handle(new AssignRoleToUserCommand(user.Id, role.Id), CancellationToken.None);

        var reloaded = await _db.DirectoryUsers.Include(u => u.Roles).FirstAsync(u => u.Id == user.Id);
        reloaded.Roles.Should().ContainSingle(r => r.RoleId == role.Id);
    }

    [Fact]
    public async Task Handle_AlreadyAssigned_DoesNotDuplicate()
    {
        var (user, role) = await SeedUserAndRoleAsync();
        var handler = CreateHandler();

        await handler.Handle(new AssignRoleToUserCommand(user.Id, role.Id), CancellationToken.None);
        await handler.Handle(new AssignRoleToUserCommand(user.Id, role.Id), CancellationToken.None);

        var reloaded = await _db.DirectoryUsers.Include(u => u.Roles).FirstAsync(u => u.Id == user.Id);
        reloaded.Roles.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_UnknownRole_ThrowsNotFound()
    {
        var directory = Directory.CreateInternal("Internal Users", 0);
        var user = DirectoryUser.CreateInternal(directory.Id, "kullanici", null, null, null, null, "HASH");
        _db.Directories.Add(directory);
        _db.DirectoryUsers.Add(user);
        await _db.SaveChangesAsync();

        var act = async () => await CreateHandler().Handle(
            new AssignRoleToUserCommand(user.Id, Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
```

- [ ] **Step 2: Testi çalıştır, başarısız olduğunu doğrula**

Run: `dotnet test backend/tests/EforTakip.Application.Tests/EforTakip.Application.Tests.csproj --filter AssignRoleToUserCommandHandlerTests`
Expected: FAIL — `AssignRoleToUserCommand`/`AssignRoleToUserCommandHandler` bulunamaz, derleme hatası.

- [ ] **Step 3: Komutları oluştur**

`backend/src/EforTakip.Application/Roles/Commands/AssignRoleToUser/AssignRoleToUserCommand.cs`:

```csharp
using MediatR;

namespace EforTakip.Application.Roles.Commands.AssignRoleToUser;

public sealed record AssignRoleToUserCommand(Guid UserId, Guid RoleId) : IRequest;
```

`backend/src/EforTakip.Application/Roles/Commands/AssignRoleToUser/AssignRoleToUserCommandHandler.cs`:

```csharp
using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Directories;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.Roles;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.Roles.Commands.AssignRoleToUser;

public sealed class AssignRoleToUserCommandHandler(IApplicationDbContext db, IUnitOfWork unitOfWork)
    : IRequestHandler<AssignRoleToUserCommand>
{
    public async Task Handle(AssignRoleToUserCommand request, CancellationToken cancellationToken)
    {
        var user = await db.DirectoryUsers
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(DirectoryUser), request.UserId);

        var roleExists = await db.Roles.AnyAsync(r => r.Id == request.RoleId, cancellationToken);
        if (!roleExists)
            throw new NotFoundException(nameof(Role), request.RoleId);

        var created = user.AssignRole(request.RoleId);
        if (created is not null)
            db.DirectoryUserRoles.Add(created);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
```

`backend/src/EforTakip.Application/Roles/Commands/AssignRoleToUser/AssignRoleToUserCommandValidator.cs`:

```csharp
using FluentValidation;

namespace EforTakip.Application.Roles.Commands.AssignRoleToUser;

public sealed class AssignRoleToUserCommandValidator : AbstractValidator<AssignRoleToUserCommand>
{
    public AssignRoleToUserCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("Kullanıcı belirtilmelidir.");
        RuleFor(x => x.RoleId).NotEmpty().WithMessage("Rol belirtilmelidir.");
    }
}
```

`backend/src/EforTakip.Application/Roles/Commands/RemoveRoleFromUser/RemoveRoleFromUserCommand.cs`:

```csharp
using MediatR;

namespace EforTakip.Application.Roles.Commands.RemoveRoleFromUser;

public sealed record RemoveRoleFromUserCommand(Guid UserId, Guid RoleId) : IRequest;
```

`backend/src/EforTakip.Application/Roles/Commands/RemoveRoleFromUser/RemoveRoleFromUserCommandHandler.cs`:

```csharp
using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Directories;
using EforTakip.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.Roles.Commands.RemoveRoleFromUser;

public sealed class RemoveRoleFromUserCommandHandler(IApplicationDbContext db, IUnitOfWork unitOfWork)
    : IRequestHandler<RemoveRoleFromUserCommand>
{
    public async Task Handle(RemoveRoleFromUserCommand request, CancellationToken cancellationToken)
    {
        var user = await db.DirectoryUsers
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(DirectoryUser), request.UserId);

        user.RemoveRole(request.RoleId);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
```

- [ ] **Step 4: Testi çalıştır, geçtiğini doğrula**

Run: `dotnet test backend/tests/EforTakip.Application.Tests/EforTakip.Application.Tests.csproj --filter AssignRoleToUserCommandHandlerTests`
Expected: PASS (3 test).

- [ ] **Step 5: Commit**

```bash
git add backend/src/EforTakip.Application/Roles/Commands/AssignRoleToUser backend/src/EforTakip.Application/Roles/Commands/RemoveRoleFromUser backend/tests/EforTakip.Application.Tests/Roles/Commands/AssignRoleToUserCommandHandlerTests.cs
git commit -m "feat: add assign/remove role-to-user commands"
```

---

## Task 9: Application — Role query'leri ve DTO'ları

**Files:**
- Create: `backend/src/EforTakip.Application/Roles/Dtos/RoleDto.cs`
- Create: `backend/src/EforTakip.Application/Roles/Dtos/RoleDetailDto.cs`
- Create: `backend/src/EforTakip.Application/Roles/Dtos/RoleAssignedUserDto.cs`
- Create: `backend/src/EforTakip.Application/Roles/RoleMappingConfig.cs`
- Create: `backend/src/EforTakip.Application/Roles/Queries/GetRoles/GetRolesQuery.cs`
- Create: `backend/src/EforTakip.Application/Roles/Queries/GetRoles/GetRolesQueryHandler.cs`
- Create: `backend/src/EforTakip.Application/Roles/Queries/GetRoleById/GetRoleByIdQuery.cs`
- Create: `backend/src/EforTakip.Application/Roles/Queries/GetRoleById/GetRoleByIdQueryHandler.cs`
- Create: `backend/src/EforTakip.Application/Roles/Queries/GetPermissionCatalog/GetPermissionCatalogQuery.cs`
- Create: `backend/src/EforTakip.Application/Roles/Queries/GetPermissionCatalog/GetPermissionCatalogQueryHandler.cs`
- Test: `backend/tests/EforTakip.Application.Tests/Roles/Queries/GetRoleByIdQueryHandlerTests.cs`

**Interfaces:**
- Consumes: `Role`, `Permissions.All` (Task 1, 2), `IApplicationDbContext.Roles/.DirectoryUserRoles/.DirectoryUsers`.
- Produces: `RoleDto { Id, Name, Description, IsSystemAdmin, PermissionCount }`,
  `RoleDetailDto { Id, Name, Description, IsSystemAdmin, Permissions: string[], AssignedUsers: RoleAssignedUserDto[] }`,
  `RoleAssignedUserDto { Id, Username, DisplayName }`,
  `GetRolesQuery: IRequest<IReadOnlyCollection<RoleDto>>`,
  `GetRoleByIdQuery(Guid RoleId): IRequest<RoleDetailDto>`,
  `GetPermissionCatalogQuery: IRequest<IReadOnlyCollection<string>>` — Task 12 (RolesController) bunları kullanır.

- [ ] **Step 1: Testi yaz**

`backend/tests/EforTakip.Application.Tests/Roles/Queries/GetRoleByIdQueryHandlerTests.cs`:

```csharp
using EforTakip.Application.Roles.Queries.GetRoleById;
using EforTakip.Application.Tests.Directories.Commands;
using EforTakip.Domain.Directories;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.Roles;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Directory = EforTakip.Domain.Directories.Directory;

namespace EforTakip.Application.Tests.Roles.Queries;

public class GetRoleByIdQueryHandlerTests : IAsyncDisposable
{
    private readonly TestDbContext _db;

    public GetRoleByIdQueryHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase($"get-role-tests-{Guid.NewGuid()}")
            .Options;
        _db = new TestDbContext(options);
    }

    public async ValueTask DisposeAsync() => await _db.DisposeAsync();

    private GetRoleByIdQueryHandler CreateHandler() => new(_db);

    [Fact]
    public async Task Handle_ExistingRole_ReturnsPermissionsAndAssignedUsers()
    {
        var directory = Directory.CreateInternal("Internal Users", 0);
        var user = DirectoryUser.CreateInternal(directory.Id, "kullanici", null, null, "Kullanıcı", null, "HASH");
        var role = Role.Create("Proje Yöneticisi", "Açıklama", false);
        role.GrantPermission("project:read");
        var assignment = user.AssignRole(role.Id);

        _db.Directories.Add(directory);
        _db.DirectoryUsers.Add(user);
        _db.Roles.Add(role);
        _db.DirectoryUserRoles.Add(assignment!);
        await _db.SaveChangesAsync();

        var result = await CreateHandler().Handle(new GetRoleByIdQuery(role.Id), CancellationToken.None);

        result.Name.Should().Be("Proje Yöneticisi");
        result.Permissions.Should().ContainSingle(p => p == "project:read");
        result.AssignedUsers.Should().ContainSingle(u => u.Username == "kullanici");
    }

    [Fact]
    public async Task Handle_UnknownRole_ThrowsNotFound()
    {
        var act = async () => await CreateHandler().Handle(new GetRoleByIdQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
```

- [ ] **Step 2: Testi çalıştır, başarısız olduğunu doğrula**

Run: `dotnet test backend/tests/EforTakip.Application.Tests/EforTakip.Application.Tests.csproj --filter GetRoleByIdQueryHandlerTests`
Expected: FAIL — `GetRoleByIdQuery`/`GetRoleByIdQueryHandler` bulunamaz, derleme hatası.

- [ ] **Step 3: DTO'ları ve mapping config'i oluştur**

`backend/src/EforTakip.Application/Roles/Dtos/RoleDto.cs`:

```csharp
namespace EforTakip.Application.Roles.Dtos;

public sealed class RoleDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public string? Description { get; init; }
    public bool IsSystemAdmin { get; init; }
    public int PermissionCount { get; init; }
}
```

`backend/src/EforTakip.Application/Roles/Dtos/RoleAssignedUserDto.cs`:

```csharp
namespace EforTakip.Application.Roles.Dtos;

public sealed class RoleAssignedUserDto
{
    public Guid Id { get; init; }
    public string Username { get; init; } = default!;
    public string? DisplayName { get; init; }
}
```

`backend/src/EforTakip.Application/Roles/Dtos/RoleDetailDto.cs`:

```csharp
namespace EforTakip.Application.Roles.Dtos;

public sealed class RoleDetailDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public string? Description { get; init; }
    public bool IsSystemAdmin { get; init; }
    public IReadOnlyCollection<string> Permissions { get; init; } = [];
    public IReadOnlyCollection<RoleAssignedUserDto> AssignedUsers { get; init; } = [];
}
```

`backend/src/EforTakip.Application/Roles/RoleMappingConfig.cs`:

```csharp
using EforTakip.Application.Roles.Dtos;
using EforTakip.Domain.Roles;
using Mapster;

namespace EforTakip.Application.Roles;

public sealed class RoleMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Role, RoleDto>()
            .Map(dest => dest.PermissionCount, src => src.Permissions.Count);
    }
}
```

- [ ] **Step 4: Query'leri ve handler'ları oluştur**

`backend/src/EforTakip.Application/Roles/Queries/GetRoles/GetRolesQuery.cs`:

```csharp
using EforTakip.Application.Roles.Dtos;
using MediatR;

namespace EforTakip.Application.Roles.Queries.GetRoles;

public sealed record GetRolesQuery : IRequest<IReadOnlyCollection<RoleDto>>;
```

`backend/src/EforTakip.Application/Roles/Queries/GetRoles/GetRolesQueryHandler.cs`:

```csharp
using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Roles.Dtos;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.Roles.Queries.GetRoles;

public sealed class GetRolesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetRolesQuery, IReadOnlyCollection<RoleDto>>
{
    public async Task<IReadOnlyCollection<RoleDto>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
        => await db.Roles
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .ProjectToType<RoleDto>()
            .ToListAsync(cancellationToken);
}
```

`backend/src/EforTakip.Application/Roles/Queries/GetRoleById/GetRoleByIdQuery.cs`:

```csharp
using EforTakip.Application.Roles.Dtos;
using MediatR;

namespace EforTakip.Application.Roles.Queries.GetRoleById;

public sealed record GetRoleByIdQuery(Guid RoleId) : IRequest<RoleDetailDto>;
```

`backend/src/EforTakip.Application/Roles/Queries/GetRoleById/GetRoleByIdQueryHandler.cs`:

```csharp
using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Roles.Dtos;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.Roles;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.Roles.Queries.GetRoleById;

public sealed class GetRoleByIdQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetRoleByIdQuery, RoleDetailDto>
{
    public async Task<RoleDetailDto> Handle(GetRoleByIdQuery request, CancellationToken cancellationToken)
    {
        var role = await db.Roles
            .AsNoTracking()
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken)
            ?? throw new NotFoundException(nameof(Role), request.RoleId);

        var assignedUsers = await (
            from userRole in db.DirectoryUserRoles.AsNoTracking()
            join user in db.DirectoryUsers.AsNoTracking() on userRole.DirectoryUserId equals user.Id
            where userRole.RoleId == request.RoleId
            orderby user.Username
            select new RoleAssignedUserDto
            {
                Id = user.Id,
                Username = user.Username,
                DisplayName = user.DisplayName
            }).ToListAsync(cancellationToken);

        return new RoleDetailDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            IsSystemAdmin = role.IsSystemAdmin,
            Permissions = role.Permissions.Select(p => p.PermissionKey).ToList(),
            AssignedUsers = assignedUsers
        };
    }
}
```

`backend/src/EforTakip.Application/Roles/Queries/GetPermissionCatalog/GetPermissionCatalogQuery.cs`:

```csharp
using MediatR;

namespace EforTakip.Application.Roles.Queries.GetPermissionCatalog;

public sealed record GetPermissionCatalogQuery : IRequest<IReadOnlyCollection<string>>;
```

`backend/src/EforTakip.Application/Roles/Queries/GetPermissionCatalog/GetPermissionCatalogQueryHandler.cs`:

```csharp
using EforTakip.Domain.Authorization;
using MediatR;

namespace EforTakip.Application.Roles.Queries.GetPermissionCatalog;

public sealed class GetPermissionCatalogQueryHandler
    : IRequestHandler<GetPermissionCatalogQuery, IReadOnlyCollection<string>>
{
    public Task<IReadOnlyCollection<string>> Handle(
        GetPermissionCatalogQuery request, CancellationToken cancellationToken)
        => Task.FromResult(Permissions.All);
}
```

- [ ] **Step 5: Testi çalıştır, geçtiğini doğrula**

Run: `dotnet test backend/tests/EforTakip.Application.Tests/EforTakip.Application.Tests.csproj --filter GetRoleByIdQueryHandlerTests`
Expected: PASS (2 test).

- [ ] **Step 6: Backend derlemesini ve tüm testleri doğrula**

Run: `dotnet build backend/EforTakip.sln && dotnet test backend/EforTakip.sln`
Expected: Derleme başarılı; tüm testler PASS (bilinen 2 önceden var olan `LogWorkCommandHandlerTests` hatası hariç).

- [ ] **Step 7: Commit**

```bash
git add backend/src/EforTakip.Application/Roles/Dtos backend/src/EforTakip.Application/Roles/RoleMappingConfig.cs backend/src/EforTakip.Application/Roles/Queries backend/tests/EforTakip.Application.Tests/Roles/Queries
git commit -m "feat: add role list/detail/permission-catalog queries"
```

---

## Task 10: Auth — JWT'ye izin claim'lerini göm

**Files:**
- Modify: `backend/src/EforTakip.Application/Common/Models/AuthenticatedUser.cs`
- Modify: `backend/src/EforTakip.Infrastructure/Security/JwtTokenService.cs`
- Modify: `backend/src/EforTakip.Application/Auth/Commands/Login/LoginCommandHandler.cs`
- Modify: `backend/tests/EforTakip.Application.Tests/Auth/LoginCommandHandlerTests.cs`

**Interfaces:**
- Consumes: `DirectoryUser.Roles` (Task 3), `Role.IsSystemAdmin`, `Role.Permissions` (Task 2),
  `IApplicationDbContext.Roles` (Task 4).
- Produces: `AuthenticatedUser(Id, Username, DisplayName, DirectoryId, Source, IsSystemAdmin,
  PermissionKeys)` — Task 11'deki `PermissionAuthorizationHandler` JWT'de gömülü
  `is_system_admin` ve `permission` claim'lerini okuyacak.

- [ ] **Step 1: `LoginCommandHandlerTests`'e izin claim testini ekle**

`backend/tests/EforTakip.Application.Tests/Auth/LoginCommandHandlerTests.cs` dosyasında,
`using Directory = EforTakip.Domain.Directories.Directory;` satırının altına şunu ekle:

```csharp
using EforTakip.Domain.Roles;
```

Dosyanın sonundaki kapanış `}`'dan hemen önce şu testi ekle:

```csharp

    [Fact]
    public async Task Handle_UserWithRole_PassesGrantedPermissionsToTokenService()
    {
        var directory = InternalDirectory();
        var user = await AddInternalUserAsync(directory);
        var role = Role.Create("Proje Yöneticisi", null, false);
        role.GrantPermission("project:read");
        var assignment = user.AssignRole(role.Id);
        _db.Roles.Add(role);
        _db.DirectoryUserRoles.Add(assignment!);
        await _db.SaveChangesAsync();
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        await CreateHandler().Handle(new LoginCommand("sanal.kullanici", "dogru-sifre"), CancellationToken.None);

        _tokenService.Received(1).CreateToken(Arg.Is<AuthenticatedUser>(u =>
            !u.IsSystemAdmin && u.PermissionKeys.Contains("project:read")));
    }
```

- [ ] **Step 2: Testi çalıştır, başarısız olduğunu doğrula**

Run: `dotnet test backend/tests/EforTakip.Application.Tests/EforTakip.Application.Tests.csproj --filter Handle_UserWithRole_PassesGrantedPermissionsToTokenService`
Expected: FAIL — `AuthenticatedUser` 5 parametre alıyor, 7 verilmiyor / `IsSystemAdmin` üyesi yok, derleme hatası.

- [ ] **Step 3: `AuthenticatedUser`'ı güncelle**

`backend/src/EforTakip.Application/Common/Models/AuthenticatedUser.cs` dosyasının tamamını
şununla değiştir:

```csharp
using EforTakip.Domain.Directories;

namespace EforTakip.Application.Common.Models;

public sealed record AuthenticatedUser(
    Guid Id,
    string Username,
    string? DisplayName,
    Guid DirectoryId,
    DirectorySource Source,
    bool IsSystemAdmin,
    IReadOnlyCollection<string> PermissionKeys);
```

- [ ] **Step 4: `LoginCommandHandler`'ı güncelle**

`backend/src/EforTakip.Application/Auth/Commands/Login/LoginCommandHandler.cs` dosyasının
tamamını şununla değiştir:

```csharp
using EforTakip.Application.Auth.Dtos;
using EforTakip.Application.Common.Exceptions;
using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Common.Models;
using EforTakip.Application.Directories.Ldap;
using EforTakip.Domain.Directories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Directory = EforTakip.Domain.Directories.Directory;

namespace EforTakip.Application.Auth.Commands.Login;

public sealed class LoginCommandHandler(
    IApplicationDbContext db,
    IRepository<Directory> directoryRepository,
    IPasswordHasher passwordHasher,
    ILdapService ldapService,
    ITokenService tokenService)
    : IRequestHandler<LoginCommand, LoginResultDto>
{
    public async Task<LoginResultDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // Kullanıcı adları normalize edilmiş (invariant küçük harf) olarak saklanır;
        // girdi de aynı şekilde normalize edilerek doğrudan karşılaştırılır.
        var username = request.Username.Trim().ToLowerInvariant();

        var user = await db.DirectoryUsers
            .AsNoTracking()
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);

        // Kullanıcının bulunamaması, pasif olması ve şifrenin yanlış olması aynı hatayı verir;
        // aksi halde saldırgan hangi kullanıcı adlarının var olduğunu öğrenebilir.
        if (user is null || !user.IsActive)
            throw new AuthenticationFailedException();

        // Dizin pasife alındıysa o dizindeki hiçbir kullanıcı giriş yapamaz — internal de AD de.
        var directory = await directoryRepository.GetByIdAsync(user.DirectoryId, cancellationToken);
        if (directory is null || !directory.IsActive)
            throw new AuthenticationFailedException();

        var authenticated = user.Source == DirectorySource.Internal
            ? VerifyInternalPassword(user, request.Password)
            : await ldapService.AuthenticateAsync(directory, user.Username, request.Password, cancellationToken);

        if (!authenticated)
            throw new AuthenticationFailedException();

        var roleIds = user.Roles.Select(r => r.RoleId).ToList();
        var roles = await db.Roles
            .AsNoTracking()
            .Include(r => r.Permissions)
            .Where(r => roleIds.Contains(r.Id))
            .ToListAsync(cancellationToken);

        var isSystemAdmin = roles.Any(r => r.IsSystemAdmin);
        var permissionKeys = roles
            .SelectMany(r => r.Permissions.Select(p => p.PermissionKey))
            .Distinct()
            .ToList();

        var (token, expiresAtUtc) = tokenService.CreateToken(new AuthenticatedUser(
            user.Id, user.Username, user.DisplayName, user.DirectoryId, user.Source,
            isSystemAdmin, permissionKeys));

        return new LoginResultDto
        {
            Token = token,
            ExpiresAtUtc = expiresAtUtc,
            UserId = user.Id,
            Username = user.Username,
            DisplayName = user.DisplayName,
            Source = user.Source
        };
    }

    /// <summary>AD kullanıcısının şifresi bizde saklanmaz; her girişte dizine sorulur.</summary>
    private bool VerifyInternalPassword(DirectoryUser user, string password)
        => !string.IsNullOrEmpty(user.PasswordHash) && passwordHasher.Verify(password, user.PasswordHash);
}
```

- [ ] **Step 5: `JwtTokenService`'i güncelle**

`backend/src/EforTakip.Infrastructure/Security/JwtTokenService.cs` dosyasında
`claims.Add(new Claim("display_name", user.DisplayName));` bloğunun (`if` gövdesi dahil) hemen
altına şunu ekle:

```csharp

        if (user.IsSystemAdmin)
            claims.Add(new Claim("is_system_admin", "true"));

        foreach (var permissionKey in user.PermissionKeys)
            claims.Add(new Claim("permission", permissionKey));
```

- [ ] **Step 6: Testi çalıştır, geçtiğini doğrula**

Run: `dotnet test backend/tests/EforTakip.Application.Tests/EforTakip.Application.Tests.csproj --filter Auth`
Expected: PASS (önceki 9 test + 1 yeni test).

- [ ] **Step 7: Backend derlemesini doğrula**

Run: `dotnet build backend/EforTakip.sln`
Expected: Derleme başarılı, hata yok.

- [ ] **Step 8: Commit**

```bash
git add backend/src/EforTakip.Application/Common/Models/AuthenticatedUser.cs backend/src/EforTakip.Infrastructure/Security/JwtTokenService.cs backend/src/EforTakip.Application/Auth/Commands/Login/LoginCommandHandler.cs backend/tests/EforTakip.Application.Tests/Auth/LoginCommandHandlerTests.cs
git commit -m "feat: embed granted permissions and system-admin flag in JWT"
```

---

## Task 11: API — Permission authorization altyapısı

**Files:**
- Create: `backend/src/EforTakip.Api/Authorization/PermissionRequirement.cs`
- Create: `backend/src/EforTakip.Api/Authorization/RequirePermissionAttribute.cs`
- Create: `backend/src/EforTakip.Api/Authorization/PermissionAuthorizationHandler.cs`
- Create: `backend/src/EforTakip.Api/Authorization/PermissionPolicyProvider.cs`
- Modify: `backend/src/EforTakip.Api/Extensions/ApiServiceCollectionExtensions.cs`

**Interfaces:**
- Consumes: JWT `permission` ve `is_system_admin` claim'leri (Task 10).
- Produces: `[RequirePermission(string permissionKey)]` attribute'u — Task 12
  (`RolesController`) bunu kullanır; ileride diğer controller'lara da (bu planın kapsamı dışında)
  uygulanabilir.

- [ ] **Step 1: `PermissionRequirement.cs`'i oluştur**

```csharp
using Microsoft.AspNetCore.Authorization;

namespace EforTakip.Api.Authorization;

public sealed class PermissionRequirement(string permissionKey) : IAuthorizationRequirement
{
    public string PermissionKey { get; } = permissionKey;
}
```

- [ ] **Step 2: `RequirePermissionAttribute.cs`'i oluştur**

```csharp
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
```

- [ ] **Step 3: `PermissionAuthorizationHandler.cs`'i oluştur**

```csharp
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
```

- [ ] **Step 4: `PermissionPolicyProvider.cs`'i oluştur**

```csharp
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
```

- [ ] **Step 5: DI'a kaydet**

`backend/src/EforTakip.Api/Extensions/ApiServiceCollectionExtensions.cs` dosyasında
`using EforTakip.Api.Middleware;` satırının altına şunu ekle:

```csharp
using EforTakip.Api.Authorization;
```

```csharp
using Microsoft.AspNetCore.Authorization;
```

(eğer zaten yoksa — dosyada `Microsoft.AspNetCore.Authorization` zaten import edilmiş olabilir,
o satırı tekrar eklemeye gerek yoktur, sadece `EforTakip.Api.Authorization` eklenir.)

`services.AddAuthorization(options => { ... });` bloğunun hemen altına şunu ekle:

```csharp

        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
```

- [ ] **Step 6: Backend derlemesini doğrula**

Run: `dotnet build backend/EforTakip.sln`
Expected: Derleme başarılı, hata yok.

- [ ] **Step 7: Commit**

```bash
git add backend/src/EforTakip.Api/Authorization/ backend/src/EforTakip.Api/Extensions/ApiServiceCollectionExtensions.cs
git commit -m "feat: add dynamic permission-based authorization policy"
```

---

## Task 12: API — `RolesController`

**Files:**
- Create: `backend/src/EforTakip.Api/Contracts/Roles/UpdateRoleRequestBody.cs`
- Create: `backend/src/EforTakip.Api/Controllers/v1/RolesController.cs`

**Interfaces:**
- Consumes: Tüm Task 6-9 komut/query'leri, `RequirePermissionAttribute` (Task 11),
  `Permissions.Role.Read/Manage` (Task 1).
- Produces: `GET/POST/PUT/DELETE /api/v1/roles`, `GET /api/v1/roles/permission-catalog`,
  `POST /api/v1/roles/{id}/permissions`, `POST /api/v1/roles/{id}/permissions/revoke`,
  `POST /api/v1/roles/{id}/users`, `DELETE /api/v1/roles/{id}/users/{userId}` — Task 13
  (frontend api client) bu route'ları çağıracak.

- [ ] **Step 1: `UpdateRoleRequestBody.cs`'i oluştur**

```csharp
namespace EforTakip.Api.Contracts.Roles;

public sealed record UpdateRoleRequestBody(string Name, string? Description);
```

- [ ] **Step 2: `RolesController.cs`'i oluştur**

```csharp
using Asp.Versioning;
using EforTakip.Api.Authorization;
using EforTakip.Api.Contracts.Roles;
using EforTakip.Application.Roles.Commands.AssignRoleToUser;
using EforTakip.Application.Roles.Commands.CreateRole;
using EforTakip.Application.Roles.Commands.DeleteRole;
using EforTakip.Application.Roles.Commands.GrantPermission;
using EforTakip.Application.Roles.Commands.RemoveRoleFromUser;
using EforTakip.Application.Roles.Commands.RevokePermission;
using EforTakip.Application.Roles.Commands.UpdateRole;
using EforTakip.Application.Roles.Dtos;
using EforTakip.Application.Roles.Queries.GetPermissionCatalog;
using EforTakip.Application.Roles.Queries.GetRoleById;
using EforTakip.Application.Roles.Queries.GetRoles;
using EforTakip.Domain.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EforTakip.Api.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class RolesController(ISender mediator) : ControllerBase
{
    public sealed record GrantPermissionRequestBody(string PermissionKey);
    public sealed record RevokePermissionRequestBody(string PermissionKey);
    public sealed record AssignUserRequestBody(Guid UserId);

    [RequirePermission(Permissions.Role.Read)]
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<RoleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<RoleDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await mediator.Send(new GetRolesQuery(), cancellationToken));

    [RequirePermission(Permissions.Role.Read)]
    [HttpGet("permission-catalog")]
    [ProducesResponseType(typeof(IReadOnlyCollection<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<string>>> GetPermissionCatalog(CancellationToken cancellationToken)
        => Ok(await mediator.Send(new GetPermissionCatalogQuery(), cancellationToken));

    [RequirePermission(Permissions.Role.Read)]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(RoleDetailDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<RoleDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new GetRoleByIdQuery(id), cancellationToken));

    [RequirePermission(Permissions.Role.Manage)]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateRoleCommand command, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { version = "1.0", id }, new { id });
    }

    [RequirePermission(Permissions.Role.Manage)]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(Guid id, UpdateRoleRequestBody body, CancellationToken cancellationToken)
    {
        await mediator.Send(new UpdateRoleCommand(id, body.Name, body.Description), cancellationToken);
        return NoContent();
    }

    [RequirePermission(Permissions.Role.Manage)]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteRoleCommand(id), cancellationToken);
        return NoContent();
    }

    [RequirePermission(Permissions.Role.Manage)]
    [HttpPost("{id:guid}/permissions")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GrantPermission(
        Guid id, GrantPermissionRequestBody body, CancellationToken cancellationToken)
    {
        await mediator.Send(new GrantPermissionCommand(id, body.PermissionKey), cancellationToken);
        return NoContent();
    }

    [RequirePermission(Permissions.Role.Manage)]
    [HttpPost("{id:guid}/permissions/revoke")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokePermission(
        Guid id, RevokePermissionRequestBody body, CancellationToken cancellationToken)
    {
        await mediator.Send(new RevokePermissionCommand(id, body.PermissionKey), cancellationToken);
        return NoContent();
    }

    [RequirePermission(Permissions.Role.Manage)]
    [HttpPost("{id:guid}/users")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AssignUser(Guid id, AssignUserRequestBody body, CancellationToken cancellationToken)
    {
        await mediator.Send(new AssignRoleToUserCommand(body.UserId, id), cancellationToken);
        return NoContent();
    }

    [RequirePermission(Permissions.Role.Manage)]
    [HttpDelete("{id:guid}/users/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveUser(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        await mediator.Send(new RemoveRoleFromUserCommand(userId, id), cancellationToken);
        return NoContent();
    }
}
```

- [ ] **Step 3: Backend derlemesini ve tüm testleri doğrula**

Run: `dotnet build backend/EforTakip.sln && dotnet test backend/EforTakip.sln`
Expected: Derleme başarılı; tüm testler PASS (bilinen 2 önceden var olan
`LogWorkCommandHandlerTests` hatası hariç).

- [ ] **Step 4: Commit**

```bash
git add backend/src/EforTakip.Api/Contracts/Roles backend/src/EforTakip.Api/Controllers/v1/RolesController.cs
git commit -m "feat: add RolesController"
```

---

## Task 13: Frontend — api client ve hook'lar

**Files:**
- Modify: `frontend/src/api/types.ts`
- Create: `frontend/src/api/roles.ts`
- Create: `frontend/src/hooks/useRoles.ts`

**Interfaces:**
- Consumes: `RolesController` route'ları (Task 12).
- Produces: `RoleDto`, `RoleDetailDto`, `RoleAssignedUserDto` (types), `useRoles()`,
  `useRole(id)`, `usePermissionCatalog()`, `useCreateRoleMutation()`, `useUpdateRoleMutation(id)`,
  `useDeleteRoleMutation()`, `useGrantPermissionMutation(roleId)`,
  `useRevokePermissionMutation(roleId)`, `useAssignUserToRoleMutation(roleId)`,
  `useRemoveUserFromRoleMutation(roleId)` — Task 14 (`RolesSection.tsx`) bunları kullanır.

- [ ] **Step 1: `types.ts`'e tipleri ekle**

`frontend/src/api/types.ts` dosyasının sonuna şunu ekle:

```ts

export interface RoleDto {
  id: string;
  name: string;
  description: string | null;
  isSystemAdmin: boolean;
  permissionCount: number;
}

export interface RoleAssignedUserDto {
  id: string;
  username: string;
  displayName: string | null;
}

export interface RoleDetailDto {
  id: string;
  name: string;
  description: string | null;
  isSystemAdmin: boolean;
  permissions: string[];
  assignedUsers: RoleAssignedUserDto[];
}
```

- [ ] **Step 2: `api/roles.ts`'i oluştur**

```ts
import { apiClient } from './client';
import type { RoleDetailDto, RoleDto } from './types';

export interface SaveRolePayload {
  name: string;
  description: string | null;
}

export function getRoles() {
  return apiClient.get<RoleDto[]>('/api/v1/roles');
}

export function getRoleById(id: string) {
  return apiClient.get<RoleDetailDto>(`/api/v1/roles/${id}`);
}

export function getPermissionCatalog() {
  return apiClient.get<string[]>('/api/v1/roles/permission-catalog');
}

export function createRole(payload: SaveRolePayload) {
  return apiClient.post<{ id: string }>('/api/v1/roles', payload);
}

export function updateRole(id: string, payload: SaveRolePayload) {
  return apiClient.put<void>(`/api/v1/roles/${id}`, payload);
}

export function deleteRole(id: string) {
  return apiClient.delete<void>(`/api/v1/roles/${id}`);
}

export function grantPermission(roleId: string, permissionKey: string) {
  return apiClient.post<void>(`/api/v1/roles/${roleId}/permissions`, { permissionKey });
}

export function revokePermission(roleId: string, permissionKey: string) {
  return apiClient.post<void>(`/api/v1/roles/${roleId}/permissions/revoke`, { permissionKey });
}

export function assignUserToRole(roleId: string, userId: string) {
  return apiClient.post<void>(`/api/v1/roles/${roleId}/users`, { userId });
}

export function removeUserFromRole(roleId: string, userId: string) {
  return apiClient.delete<void>(`/api/v1/roles/${roleId}/users/${userId}`);
}
```

- [ ] **Step 3: `hooks/useRoles.ts`'i oluştur**

```ts
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  assignUserToRole,
  createRole,
  deleteRole,
  getPermissionCatalog,
  getRoleById,
  getRoles,
  grantPermission,
  removeUserFromRole,
  revokePermission,
  updateRole,
  type SaveRolePayload,
} from '../api/roles';

export function useRoles() {
  return useQuery({ queryKey: ['roles'], queryFn: getRoles });
}

export function useRole(id: string | null) {
  return useQuery({
    queryKey: ['roles', id],
    queryFn: () => getRoleById(id!),
    enabled: id !== null,
  });
}

export function usePermissionCatalog() {
  return useQuery({ queryKey: ['roles', 'permission-catalog'], queryFn: getPermissionCatalog });
}

export function useCreateRoleMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: SaveRolePayload) => createRole(payload),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['roles'] }),
  });
}

export function useUpdateRoleMutation(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: SaveRolePayload) => updateRole(id, payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['roles'] });
      queryClient.invalidateQueries({ queryKey: ['roles', id] });
    },
  });
}

export function useDeleteRoleMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => deleteRole(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['roles'] }),
  });
}

export function useGrantPermissionMutation(roleId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (permissionKey: string) => grantPermission(roleId, permissionKey),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['roles', roleId] }),
  });
}

export function useRevokePermissionMutation(roleId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (permissionKey: string) => revokePermission(roleId, permissionKey),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['roles', roleId] }),
  });
}

export function useAssignUserToRoleMutation(roleId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (userId: string) => assignUserToRole(roleId, userId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['roles', roleId] }),
  });
}

export function useRemoveUserFromRoleMutation(roleId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (userId: string) => removeUserFromRole(roleId, userId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['roles', roleId] }),
  });
}
```

- [ ] **Step 4: Tip kontrolü**

Run: `cd frontend && npx tsc --noEmit && cd ..`
Expected: Hata yok (yeni dosyalar henüz hiçbir yerde kullanılmıyor).

- [ ] **Step 5: Commit**

```bash
git add frontend/src/api/types.ts frontend/src/api/roles.ts frontend/src/hooks/useRoles.ts
git commit -m "feat: add roles frontend api client and hooks"
```

---

## Task 14: Frontend — `RolesSection.tsx` ve `AdminPage.tsx` bağlantısı

**Files:**
- Create: `frontend/src/components/admin/roles/RolesSection.tsx`
- Modify: `frontend/src/pages/AdminPage.tsx`

**Interfaces:**
- Consumes: `useRoles`, `useRole`, `usePermissionCatalog`, `useCreateRoleMutation`,
  `useUpdateRoleMutation`, `useDeleteRoleMutation`, `useGrantPermissionMutation`,
  `useRevokePermissionMutation`, `useAssignUserToRoleMutation`,
  `useRemoveUserFromRoleMutation` (Task 13), `useDirectoryUsers` (mevcut,
  `frontend/src/hooks/useDirectoryUsers.ts`, değişmedi) — kullanıcı arama için.
- Produces: `RolesSection` bileşeni, prop almaz — `AdminPage.tsx` `'roles'` sekmesinde render eder.

- [ ] **Step 1: `RolesSection.tsx`'i oluştur**

`frontend/src/components/admin/roles/RolesSection.tsx`:

```tsx
import { useState, type FormEvent } from 'react';
import { ApiError } from '../../../api/client';
import { useDirectoryUsers } from '../../../hooks/useDirectoryUsers';
import {
  useAssignUserToRoleMutation,
  useCreateRoleMutation,
  useDeleteRoleMutation,
  useGrantPermissionMutation,
  usePermissionCatalog,
  useRemoveUserFromRoleMutation,
  useRevokePermissionMutation,
  useRole,
  useRoles,
  useUpdateRoleMutation,
} from '../../../hooks/useRoles';

const inputClass =
  'w-full rounded-md border border-slate-300 px-2 py-1.5 text-sm outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500';

function groupByModule(permissionKeys: string[]): Record<string, string[]> {
  const groups: Record<string, string[]> = {};
  for (const key of permissionKeys) {
    const module = key.split(':')[0];
    (groups[module] ??= []).push(key);
  }
  return groups;
}

function RoleForm({
  initialName,
  initialDescription,
  submitLabel,
  onSubmit,
  onCancel,
}: {
  initialName: string;
  initialDescription: string;
  submitLabel: string;
  onSubmit: (name: string, description: string) => Promise<void>;
  onCancel: () => void;
}) {
  const [name, setName] = useState(initialName);
  const [description, setDescription] = useState(initialDescription);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault();
    setErrorMessage(null);
    setIsSubmitting(true);
    try {
      await onSubmit(name.trim(), description.trim());
    } catch (error) {
      setErrorMessage(error instanceof ApiError ? error.message : 'Rol kaydedilemedi.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="mb-5 flex flex-wrap items-end gap-2">
      <label className="block">
        <span className="mb-1 block text-xs font-medium text-slate-600">Rol Adı</span>
        <input value={name} onChange={(e) => setName(e.target.value)} className={inputClass} />
      </label>
      <label className="block">
        <span className="mb-1 block text-xs font-medium text-slate-600">Açıklama</span>
        <input value={description} onChange={(e) => setDescription(e.target.value)} className={inputClass} />
      </label>
      <button
        type="submit"
        disabled={name.trim().length === 0 || isSubmitting}
        className="rounded-lg bg-indigo-600 px-3 py-2 text-sm font-medium text-white hover:bg-indigo-700 disabled:bg-slate-300"
      >
        {isSubmitting ? 'Kaydediliyor…' : submitLabel}
      </button>
      <button type="button" onClick={onCancel} className="text-sm text-slate-500 hover:text-slate-700">
        Vazgeç
      </button>
      {errorMessage && <p role="alert" className="w-full text-sm text-rose-700">{errorMessage}</p>}
    </form>
  );
}

function RoleDetail({ roleId, onBack }: { roleId: string; onBack: () => void }) {
  const role = useRole(roleId);
  const catalog = usePermissionCatalog();
  const updateMutation = useUpdateRoleMutation(roleId);
  const grantMutation = useGrantPermissionMutation(roleId);
  const revokeMutation = useRevokePermissionMutation(roleId);
  const assignMutation = useAssignUserToRoleMutation(roleId);
  const removeMutation = useRemoveUserFromRoleMutation(roleId);
  const [userSearch, setUserSearch] = useState('');
  const [isEditing, setIsEditing] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const users = useDirectoryUsers({ searchTerm: userSearch, pageSize: 10 });

  if (role.isLoading || !role.data) {
    return <div className="py-8 text-center text-sm text-slate-400">Yükleniyor…</div>;
  }

  const grantedSet = new Set(role.data.permissions);
  const modules = groupByModule(catalog.data ?? []);
  const assignedUserIds = new Set(role.data.assignedUsers.map((u) => u.id));

  if (isEditing) {
    return (
      <div>
        <div className="mb-4 flex items-center justify-between">
          <h2 className="text-base font-semibold text-slate-800">{role.data.name} — Düzenle</h2>
          <button type="button" onClick={onBack} className="text-sm text-slate-500 hover:text-slate-700">
            ← Listeye dön
          </button>
        </div>
        <RoleForm
          initialName={role.data.name}
          initialDescription={role.data.description ?? ''}
          submitLabel="Kaydet"
          onCancel={() => setIsEditing(false)}
          onSubmit={async (name, description) => {
            await updateMutation.mutateAsync({ name, description: description || null });
            setIsEditing(false);
          }}
        />
      </div>
    );
  }

  const togglePermission = async (key: string) => {
    setErrorMessage(null);
    try {
      if (grantedSet.has(key)) {
        await revokeMutation.mutateAsync(key);
      } else {
        await grantMutation.mutateAsync(key);
      }
    } catch (error) {
      setErrorMessage(error instanceof ApiError ? error.message : 'İzin güncellenemedi.');
    }
  };

  const handleAssignUser = async (userId: string) => {
    setErrorMessage(null);
    try {
      await assignMutation.mutateAsync(userId);
    } catch (error) {
      setErrorMessage(error instanceof ApiError ? error.message : 'Kullanıcı atanamadı.');
    }
  };

  const handleRemoveUser = async (userId: string) => {
    setErrorMessage(null);
    try {
      await removeMutation.mutateAsync(userId);
    } catch (error) {
      setErrorMessage(error instanceof ApiError ? error.message : 'Kullanıcı kaldırılamadı.');
    }
  };

  return (
    <div>
      <div className="mb-4 flex items-center justify-between">
        <h2 className="text-base font-semibold text-slate-800">
          {role.data.name}
          {role.data.isSystemAdmin && (
            <span className="ml-2 rounded-full bg-amber-50 px-2 py-0.5 text-xs font-medium text-amber-700">
              Sistem Yöneticisi
            </span>
          )}
        </h2>
        <div className="flex items-center gap-3">
          <button type="button" onClick={() => setIsEditing(true)} className="text-sm text-indigo-600 hover:underline">
            Adı/Açıklamayı Düzenle
          </button>
          <button type="button" onClick={onBack} className="text-sm text-slate-500 hover:text-slate-700">
            ← Listeye dön
          </button>
        </div>
      </div>

      {errorMessage && (
        <p role="alert" className="mb-4 rounded-md bg-rose-50 px-3 py-2 text-sm text-rose-700">
          {errorMessage}
        </p>
      )}

      {role.data.isSystemAdmin ? (
        <p className="mb-6 text-sm text-slate-500">
          Bu rol sistemdeki tüm işlemlere erişebilir; izin listesi ayrıca yönetilmez.
        </p>
      ) : (
        <div className="mb-6">
          <h3 className="mb-2 text-sm font-semibold text-slate-700">İzinler</h3>
          <div className="space-y-3">
            {Object.entries(modules).map(([module, keys]) => (
              <div key={module}>
                <p className="mb-1 text-xs font-medium uppercase tracking-wide text-slate-400">{module}</p>
                <div className="flex flex-wrap gap-3">
                  {keys.map((key) => (
                    <label key={key} className="flex items-center gap-1.5 text-sm text-slate-700">
                      <input
                        type="checkbox"
                        checked={grantedSet.has(key)}
                        onChange={() => togglePermission(key)}
                        className="h-4 w-4"
                      />
                      {key}
                    </label>
                  ))}
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      <div>
        <h3 className="mb-2 text-sm font-semibold text-slate-700">Atanmış Kullanıcılar</h3>
        {role.data.assignedUsers.length === 0 ? (
          <p className="mb-3 text-sm text-slate-400">Bu role henüz kimse atanmamış.</p>
        ) : (
          <ul className="mb-3 space-y-1">
            {role.data.assignedUsers.map((user) => (
              <li key={user.id} className="flex items-center justify-between text-sm">
                <span className="text-slate-700">{user.displayName ?? user.username}</span>
                <button
                  type="button"
                  onClick={() => handleRemoveUser(user.id)}
                  className="text-xs text-rose-600 hover:underline"
                >
                  Kaldır
                </button>
              </li>
            ))}
          </ul>
        )}

        <input
          value={userSearch}
          onChange={(e) => setUserSearch(e.target.value)}
          placeholder="Kullanıcı adı ile ara ve ekle"
          className={inputClass}
        />
        {userSearch.trim().length > 0 && (
          <ul className="mt-2 space-y-1">
            {(users.data?.items ?? [])
              .filter((user) => !assignedUserIds.has(user.id))
              .map((user) => (
                <li key={user.id} className="flex items-center justify-between text-sm">
                  <span className="text-slate-700">{user.displayName ?? user.username}</span>
                  <button
                    type="button"
                    onClick={() => handleAssignUser(user.id)}
                    className="text-xs text-indigo-600 hover:underline"
                  >
                    Ekle
                  </button>
                </li>
              ))}
          </ul>
        )}
      </div>
    </div>
  );
}

export function RolesSection() {
  const [selectedRoleId, setSelectedRoleId] = useState<string | null>(null);
  const [isCreating, setIsCreating] = useState(false);
  const roles = useRoles();
  const createMutation = useCreateRoleMutation();
  const deleteMutation = useDeleteRoleMutation();

  if (selectedRoleId) {
    return <RoleDetail roleId={selectedRoleId} onBack={() => setSelectedRoleId(null)} />;
  }

  const items = roles.data ?? [];

  const handleDelete = async (roleId: string, roleName: string) => {
    if (!window.confirm(`"${roleName}" rolünü silmek istediğinize emin misiniz?`)) return;
    await deleteMutation.mutateAsync(roleId);
  };

  return (
    <div>
      <div className="mb-4 flex items-center justify-between">
        <h2 className="text-base font-semibold text-slate-800">Roller ve İzinler</h2>
        {!isCreating && (
          <button
            type="button"
            onClick={() => setIsCreating(true)}
            className="rounded-lg bg-indigo-600 px-3 py-2 text-sm font-medium text-white hover:bg-indigo-700"
          >
            Yeni Rol
          </button>
        )}
      </div>

      {isCreating && (
        <RoleForm
          initialName=""
          initialDescription=""
          submitLabel="Oluştur"
          onCancel={() => setIsCreating(false)}
          onSubmit={async (name, description) => {
            await createMutation.mutateAsync({ name, description: description || null });
            setIsCreating(false);
          }}
        />
      )}

      {roles.isLoading ? (
        <div className="py-8 text-center text-sm text-slate-400">Yükleniyor…</div>
      ) : items.length === 0 ? (
        <div className="rounded-xl border border-dashed border-slate-200 py-12 text-center text-sm text-slate-500">
          Henüz rol tanımlanmamış.
        </div>
      ) : (
        <table className="w-full text-left text-sm">
          <thead>
            <tr className="border-b border-slate-100 text-xs uppercase tracking-wide text-slate-400">
              <th className="py-2 pr-4 font-medium">Ad</th>
              <th className="py-2 pr-4 font-medium">Açıklama</th>
              <th className="py-2 pr-4 font-medium">İzin Sayısı</th>
              <th className="py-2 font-medium">İşlem</th>
            </tr>
          </thead>
          <tbody>
            {items.map((role) => (
              <tr key={role.id} className="border-b border-slate-50 last:border-0">
                <td
                  onClick={() => setSelectedRoleId(role.id)}
                  className="cursor-pointer py-2 pr-4 text-indigo-600 hover:underline"
                >
                  {role.name}
                  {role.isSystemAdmin && (
                    <span className="ml-2 rounded-full bg-amber-50 px-2 py-0.5 text-xs font-medium text-amber-700">
                      Sistem Yöneticisi
                    </span>
                  )}
                </td>
                <td className="py-2 pr-4 text-slate-500">{role.description ?? '—'}</td>
                <td className="py-2 pr-4 text-slate-500">
                  {role.isSystemAdmin ? 'Tümü' : role.permissionCount}
                </td>
                <td className="py-2">
                  <div className="flex gap-2 text-xs">
                    <button
                      type="button"
                      onClick={() => setSelectedRoleId(role.id)}
                      className="text-indigo-600 hover:underline"
                    >
                      Yönet
                    </button>
                    {!role.isSystemAdmin && (
                      <button
                        type="button"
                        onClick={() => handleDelete(role.id, role.name)}
                        className="text-rose-600 hover:underline"
                      >
                        Sil
                      </button>
                    )}
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}
```

Liste satırındaki "Yönet" bağlantısı rol detayına götürür; detay ekranındaki "Adı/Açıklamayı
Düzenle" butonu `RoleForm`'u `updateMutation` ile açar, izin/kullanıcı yönetimi de aynı ekranda
yapılır.

- [ ] **Step 2: `AdminPage.tsx`'e bağla**

`frontend/src/pages/AdminPage.tsx` dosyasında importların en üstüne şunu ekle:

```tsx
import { RolesSection } from '../components/admin/roles/RolesSection';
```

`{ key: 'roles', label: 'Roller ve İzinler', kind: 'placeholder' },` satırını bul, şununla
değiştir:

```tsx
          { key: 'roles', label: 'Roller ve İzinler', kind: 'roles' },
```

`SectionKind` union'ında `'orgChart'` satırının altına şunu ekle:

```ts
  | 'roles'
```

`SectionContent` içindeki (`case 'users': return <UsersSection />;` civarı) switch bloğuna şunu ekle:

```tsx
    case 'roles':
      return <RolesSection />;
```

- [ ] **Step 3: Tip kontrolü**

Run: `cd frontend && npx tsc --noEmit && cd ..`
Expected: Hata yok.

- [ ] **Step 4: Commit**

```bash
git add frontend/src/components/admin/roles/RolesSection.tsx frontend/src/pages/AdminPage.tsx
git commit -m "feat: add Roles and Permissions admin screen"
```

---

## Task 15: `CLAUDE.md` konvansiyonu ve uçtan uca doğrulama

**Files:**
- Modify: `CLAUDE.md`

**Interfaces:** Yok (dokümantasyon + manuel doğrulama).

- [ ] **Step 1: `CLAUDE.md`'ye yeni feature kuralını ekle**

`CLAUDE.md` dosyasının sonuna şu bölümü ekle:

```markdown

## Yeni Feature Eklerken İzin Kuralı

Yeni bir API endpoint'i veya işlem eklerken şu iki adım zorunludur:

1. `backend/src/EforTakip.Domain/Authorization/Permissions.cs` içinde ilgili modül sınıfına
   (yoksa yeni bir modül sınıfı açarak) bir izin sabiti ekle: `public const string X = "modul:x";`
2. Controller action'ına `[RequirePermission(Permissions.Modül.X)]` ekle.

Bunun dışında hiçbir adım (migration, seed, DB kaydı) gerekmez — izin kataloğu kodda yaşar.
`Permissions.All` reflection ile otomatik günceldir. Bir role `"modul:*"` (wildcard) izni
verilmişse o moduldeki her mevcut ve gelecekteki izin otomatik kapsanır; `IsSystemAdmin` rolü
her izni otomatik geçer.
```

- [ ] **Step 2: Backend'i başlat**

Run: `cd backend/src/EforTakip.Api && dotnet run`
Expected: `Now listening on: http://localhost:5298`

- [ ] **Step 3: Frontend'i başlat**

Run: `cd frontend && npm run dev`
Expected: `Local: http://localhost:5173/`

- [ ] **Step 4: Tarayıcıda admin / (Bootstrap:AdminPassword) ile giriş yap, Ayarlar (⚙️) →
  Kullanıcı Yönetimi → "Roller ve İzinler"e git**

Expected: Boş liste + "Yeni Rol" butonu görünür (bootstrap admin'in "Sistem Yöneticisi" rolü
`role:read` iznine ihtiyaç duymadan görünür çünkü `IsSystemAdmin` her şeyi geçer).

- [ ] **Step 5: Yeni bir rol oluştur ("Proje Yöneticisi"), detayına gir, birkaç izin işaretle**

Expected: İzinler modül bazlı gruplanmış checkbox'lar olarak görünür; işaretleme/kaldırma anında
kaydedilir (hata yoksa sayfa yeniden yüklenmeden liste güncellenir).

- [ ] **Step 6: "Kullanıcılar" bölümünden mevcut bir kullanıcı adını not al, rol detayına dönüp
  arama kutusuna yaz, "Ekle"ye tıkla**

Expected: Kullanıcı "Atanmış Kullanıcılar" listesine eklenir; "Kaldır" ile listeden çıkarılabilir.

- [ ] **Step 7: Sistem Yöneticisi rolünü silmeyi dene**

Expected: "Sil" butonu bu rol için hiç görünmez (satırda gizli); API doğrudan çağrılırsa
422 "Sistem yöneticisi rolü silinemez." döner.

- [ ] **Step 8: Backend ve frontend süreçlerini durdur**

Run (PowerShell): `Get-Process -Name 'EforTakip.Api' -ErrorAction SilentlyContinue | Stop-Process -Force`

Frontend dev server'ı çalıştırdığın terminalde Ctrl+C ile durdur.

- [ ] **Step 9: Commit**

```bash
git add CLAUDE.md
git commit -m "docs: document the two-step convention for adding permission-gated features"
```
