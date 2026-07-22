# Active Directory Entegrasyonu — Faz 3: JWT Auth + Login Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Tek bir login formundan hem internal (local şifreli) hem de Active Directory kullanıcılarının kimliğini doğrulayıp JWT üreten altyapıyı kurmak; dizin bind şifresini şifreli saklamak.

**Architecture:** Kimlik doğrulama mantığı Application'da (`LoginCommandHandler`), kriptografi ve LDAP bind Infrastructure'da (`BCryptPasswordHasher`, `AesSettingsEncryptor`, `JwtTokenService`, `LdapService.AuthenticateAsync`). Sözleşmeler (`IPasswordHasher`, `ISettingsEncryptor`, `ITokenService`) Application'da tanımlanır; böylece login akışı gerçek AD ve gerçek kripto olmadan birim testi edilebilir.

**Tech Stack:** .NET 8, BCrypt.Net-Next 4.2.1, Microsoft.AspNetCore.Authentication.JwtBearer 8.0.29, AES-GCM (System.Security.Cryptography), System.DirectoryServices.Protocols, MediatR, FluentValidation, xUnit, FluentAssertions, NSubstitute.

## Global Constraints

- Faz 1–2'nin tüm kuralları geçerlidir (sealed domain entity + factory, MediatR command/handler, FluentValidation, `Directory` için `using Directory = EforTakip.Domain.Directories.Directory;` alias'ı, Application katmanında loglama yok, Türkçe kullanıcı metinleri).
- **Şifre, token ve bind bilgisi hiçbir koşulda loglanmaz** — hata loglarında bile.
- **Boş/whitespace şifre, LDAP bind denemesi yapılmadan reddedilir.** LDAP simple bind boş şifreyle anonim bind'e dönüşebilir ve sunucu "başarılı" yanıtı verebilir; bu bir kimlik doğrulama atlatmasıdır.
- **Login hatası her durumda aynı mesajı döner:** "Kullanıcı adı veya şifre hatalı." Kullanıcının var olup olmadığı, pasif olduğu veya hangi dizine ait olduğu sızdırılmaz (kullanıcı sayımı/enumeration önlemi).
- **Pasif kullanıcı giriş yapamaz.** (Faz 2'de AD'de devre dışı bırakılan hesaplar pasife alınıyor — bu kural o işi anlamlı kılar.)
- JWT imzalama anahtarı ve ayar şifreleme anahtarı **appsettings'e yazılmaz**; environment variable / user-secrets üzerinden gelir. Üretimde eksikse uygulama **açılışta hata verip durur** (fail fast).

## Kapsam Kararı: Endpoint koruması bu fazda AÇILMAZ

Şu an hiçbir endpoint `[Authorize]` ile korumalı değil ve frontend'de login yok. Bu fazda kimlik doğrulama altyapısı ve `/auth/login` endpoint'i kurulur, **mevcut endpoint'ler korumaya alınmaz**. Aksi halde login ekranı Faz 4'te gelene kadar uygulama tamamen kullanılamaz hale gelirdi.

Koruma (`[Authorize]` + frontend token gönderimi) **Faz 4'te frontend login sayfasıyla birlikte** devreye alınır. Bu faz mevcut güvenlik durumunu kötüleştirmez, iyileştirmenin altyapısını kurar.

---

## Dosya Yapısı

**Application (`backend/src/EforTakip.Application/`):**
- `Common/Interfaces/IPasswordHasher.cs`
- `Common/Interfaces/ISettingsEncryptor.cs`
- `Common/Interfaces/ITokenService.cs`
- `Common/Models/AuthenticatedUser.cs` — token üretimi için gereken kullanıcı bilgisi
- `Directories/Ldap/ILdapService.cs` — değişiklik: `AuthenticateAsync`
- `Directories/Commands/CreateDirectory/CreateDirectoryCommandHandler.cs` — değişiklik: şifreleme
- `Directories/Commands/UpdateDirectory/UpdateDirectoryCommandHandler.cs` — değişiklik: şifreleme
- `Directories/Commands/CreateInternalUser/` — Command, Handler, Validator
- `Auth/Commands/Login/` — Command, Handler, Validator
- `Auth/Dtos/LoginResultDto.cs`

**Infrastructure (`backend/src/EforTakip.Infrastructure/`):**
- `EforTakip.Infrastructure.csproj` — değişiklik: BCrypt paketi
- `Security/BCryptPasswordHasher.cs`
- `Security/AesSettingsEncryptor.cs`
- `Security/SecurityOptions.cs`
- `Security/JwtTokenService.cs`
- `Security/JwtOptions.cs`
- `Ldap/LdapService.cs` — değişiklik: `AuthenticateAsync` + şifre çözme
- `DependencyInjection.cs` — değişiklik: servis kayıtları + options binding

**API (`backend/src/EforTakip.Api/`):**
- `Controllers/v1/AuthController.cs`
- `Controllers/v1/DirectoryUsersController.cs` — değişiklik: internal kullanıcı oluşturma
- `Extensions/ApiServiceCollectionExtensions.cs` — değişiklik: JWT authentication + Swagger auth
- `Program.cs` — değişiklik: `UseAuthentication()`
- `appsettings.Development.json` — değişiklik: geliştirme anahtarları

**Tests:**
- `backend/tests/EforTakip.Application.Tests/Auth/LoginCommandHandlerTests.cs`
- `backend/tests/EforTakip.Application.Tests/Directories/Commands/CreateInternalUserCommandHandlerTests.cs`
- `backend/tests/EforTakip.Application.Tests/Directories/Commands/CreateDirectoryCommandHandlerTests.cs` — değişiklik: yeni ctor

---

## Task 1: Şifreleme ve hash sözleşmeleri + implementasyonları

**Files:**
- Create: `backend/src/EforTakip.Application/Common/Interfaces/IPasswordHasher.cs`
- Create: `backend/src/EforTakip.Application/Common/Interfaces/ISettingsEncryptor.cs`
- Create: `backend/src/EforTakip.Infrastructure/Security/SecurityOptions.cs`
- Create: `backend/src/EforTakip.Infrastructure/Security/BCryptPasswordHasher.cs`
- Create: `backend/src/EforTakip.Infrastructure/Security/AesSettingsEncryptor.cs`
- Modify: `backend/src/EforTakip.Infrastructure/EforTakip.Infrastructure.csproj`

**Interfaces:**
- Produces:
  - `IPasswordHasher` — `string Hash(string password)`, `bool Verify(string password, string hash)`
  - `ISettingsEncryptor` — `string Encrypt(string plainText)`, `string Decrypt(string cipherText)`
  - `SecurityOptions` — `SettingsEncryptionKey` (base64, 32 bayt)

**Not:** Faz 1–2'de oluşturulmuş dizinlerin bind şifresi düz metindir. Bu faz sonrası tüm değerler şifreli kabul edilir — geriye dönük "düz metin ise olduğu gibi kullan" yedeği **bilinçli olarak eklenmez** (güvenliği zayıflatır). Uygulama henüz üretime alınmadığı için mevcut dizinlerin yeniden kaydedilmesi yeterlidir.

- [ ] **Step 1: Sözleşmeleri yaz**

`IPasswordHasher.cs`:
```csharp
namespace EforTakip.Application.Common.Interfaces;

public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string hash);
}
```

`ISettingsEncryptor.cs`:
```csharp
namespace EforTakip.Application.Common.Interfaces;

/// <summary>Dizin bind şifresi gibi ayar sırlarını veritabanında şifreli saklamak için.</summary>
public interface ISettingsEncryptor
{
    string Encrypt(string plainText);

    string Decrypt(string cipherText);
}
```

- [ ] **Step 2: BCrypt paketini ekle**

`backend/src/EforTakip.Infrastructure/EforTakip.Infrastructure.csproj` — paket ItemGroup'una ekle:
```xml
    <PackageReference Include="BCrypt.Net-Next" Version="4.2.1" />
```

Run: `dotnet restore backend/src/EforTakip.Infrastructure/EforTakip.Infrastructure.csproj`
Expected: Restore başarılı.

- [ ] **Step 3: BCryptPasswordHasher'ı yaz**

`backend/src/EforTakip.Infrastructure/Security/BCryptPasswordHasher.cs`:
```csharp
using EforTakip.Application.Common.Interfaces;

namespace EforTakip.Infrastructure.Security;

public sealed class BCryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // Bozuk/eksik hash doğrulama başarısızlığıdır, çökme sebebi değil.
            return false;
        }
    }
}
```

- [ ] **Step 4: SecurityOptions ve AesSettingsEncryptor'ı yaz**

`backend/src/EforTakip.Infrastructure/Security/SecurityOptions.cs`:
```csharp
namespace EforTakip.Infrastructure.Security;

public sealed class SecurityOptions
{
    public const string SectionName = "Security";

    /// <summary>Base64 kodlu 32 baytlık AES anahtarı. Environment variable / secret üzerinden gelir.</summary>
    public string SettingsEncryptionKey { get; set; } = string.Empty;
}
```

`backend/src/EforTakip.Infrastructure/Security/AesSettingsEncryptor.cs`:
```csharp
using System.Security.Cryptography;
using System.Text;
using EforTakip.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace EforTakip.Infrastructure.Security;

/// <summary>
/// AES-GCM ile ayar sırlarını şifreler. Saklanan biçim: base64(nonce || tag || ciphertext).
/// GCM aynı zamanda bütünlük doğrular — kurcalanan değer çözülmeye çalışıldığında hata verir.
/// </summary>
public sealed class AesSettingsEncryptor : ISettingsEncryptor
{
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int KeySize = 32;

    private readonly byte[] _key;

    public AesSettingsEncryptor(IOptions<SecurityOptions> options)
    {
        var configuredKey = options.Value.SettingsEncryptionKey;

        if (string.IsNullOrWhiteSpace(configuredKey))
            throw new InvalidOperationException(
                "Security:SettingsEncryptionKey tanımlı değil. Dizin şifreleri şifrelenemez.");

        byte[] key;
        try
        {
            key = Convert.FromBase64String(configuredKey);
        }
        catch (FormatException)
        {
            throw new InvalidOperationException(
                "Security:SettingsEncryptionKey geçerli bir base64 değeri değil.");
        }

        if (key.Length != KeySize)
            throw new InvalidOperationException(
                $"Security:SettingsEncryptionKey {KeySize} bayt olmalıdır (base64 çözülmüş hâli).");

        _key = key;
    }

    public string Encrypt(string plainText)
    {
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

        var payload = new byte[NonceSize + TagSize + cipherBytes.Length];
        nonce.CopyTo(payload, 0);
        tag.CopyTo(payload, NonceSize);
        cipherBytes.CopyTo(payload, NonceSize + TagSize);

        return Convert.ToBase64String(payload);
    }

    public string Decrypt(string cipherText)
    {
        byte[] payload;
        try
        {
            payload = Convert.FromBase64String(cipherText);
        }
        catch (FormatException)
        {
            throw new InvalidOperationException("Saklanan dizin şifresi çözülemedi.");
        }

        if (payload.Length < NonceSize + TagSize)
            throw new InvalidOperationException("Saklanan dizin şifresi çözülemedi.");

        var nonce = payload.AsSpan(0, NonceSize);
        var tag = payload.AsSpan(NonceSize, TagSize);
        var cipherBytes = payload.AsSpan(NonceSize + TagSize);
        var plainBytes = new byte[cipherBytes.Length];

        try
        {
            using var aes = new AesGcm(_key, TagSize);
            aes.Decrypt(nonce, cipherBytes, tag, plainBytes);
        }
        catch (CryptographicException)
        {
            // Anahtar değişmiş veya değer kurcalanmış olabilir; ham kripto hatası dışarı sızmaz.
            throw new InvalidOperationException("Saklanan dizin şifresi çözülemedi.");
        }

        return Encoding.UTF8.GetString(plainBytes);
    }
}
```

- [ ] **Step 5: Derle**

Run: `dotnet build backend/EforTakip.sln`
Expected: Build succeeded.

- [ ] **Step 6: Commit**

```bash
git add backend/src/EforTakip.Application/Common/Interfaces/ backend/src/EforTakip.Infrastructure/
git commit -m "feat: add password hashing and settings encryption"
```

---

## Task 2: Dizin bind şifresinin şifreli saklanması

**Files:**
- Modify: `backend/src/EforTakip.Application/Directories/Commands/CreateDirectory/CreateDirectoryCommandHandler.cs`
- Modify: `backend/src/EforTakip.Application/Directories/Commands/UpdateDirectory/UpdateDirectoryCommandHandler.cs`
- Modify: `backend/src/EforTakip.Infrastructure/Ldap/LdapService.cs`
- Modify: `backend/tests/EforTakip.Application.Tests/Directories/Commands/CreateDirectoryCommandHandlerTests.cs`
- Modify: `backend/tests/EforTakip.Application.Tests/Directories/Commands/UpdateDirectoryCommandHandlerTests.cs`

**Interfaces:**
- Consumes: `ISettingsEncryptor` (Task 1).
- Produces: `CreateDirectoryCommandHandler(IRepository<Directory>, IUnitOfWork, ISettingsEncryptor)` ve `UpdateDirectoryCommandHandler(IRepository<Directory>, IUnitOfWork, ISettingsEncryptor)` — **ctor imzaları değişir**, mevcut testler güncellenmelidir.

- [ ] **Step 1: CreateDirectory handler'ını güncelle**

`CreateDirectoryCommandHandler.cs` — ctor'a `ISettingsEncryptor settingsEncryptor` ekle ve şifreleme satırını değiştir:
```csharp
public sealed class CreateDirectoryCommandHandler(
    IRepository<Directory> repository, IUnitOfWork unitOfWork, ISettingsEncryptor settingsEncryptor)
    : IRequestHandler<CreateDirectoryCommand, Guid>
{
    public async Task<Guid> Handle(CreateDirectoryCommand request, CancellationToken cancellationToken)
    {
        var directory = request.Source == DirectorySource.Internal
            ? Directory.CreateInternal(request.Name, request.SortOrder)
            : Directory.CreateActiveDirectory(
                request.Name, request.DirectoryType!, request.Hostname!, request.Port, request.UseSsl,
                request.BindUsername!, EncryptBindPassword(request.BindPassword), request.BaseDn!,
                request.AdditionalUserDn, request.AdditionalGroupDn, request.Permission,
                request.UserObjectClass!, request.UserObjectFilter!, request.UsernameAttribute!,
                request.UsernameRdnAttribute!, request.FirstNameAttribute!, request.LastNameAttribute!,
                request.DisplayNameAttribute!, request.EmailAttribute!, request.UniqueIdAttribute!,
                request.SyncSchedule, request.SortOrder);

        await repository.AddAsync(directory, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return directory.Id;
    }

    private string EncryptBindPassword(string? bindPassword)
        => string.IsNullOrEmpty(bindPassword) ? string.Empty : settingsEncryptor.Encrypt(bindPassword);
}
```

`using EforTakip.Application.Common.Interfaces;` zaten mevcut (IRepository için).

- [ ] **Step 2: UpdateDirectory handler'ını güncelle**

`UpdateDirectoryCommandHandler.cs` — ctor'a `ISettingsEncryptor settingsEncryptor` ekle ve `UpdateActiveDirectorySettings` çağrısındaki `request.BindPassword` argümanını değiştir:
```csharp
public sealed class UpdateDirectoryCommandHandler(
    IRepository<Directory> repository, IUnitOfWork unitOfWork, ISettingsEncryptor settingsEncryptor)
    : IRequestHandler<UpdateDirectoryCommand>
```

ve `UpdateActiveDirectorySettings(...)` çağrısında:
```csharp
                request.BindUsername!, EncryptBindPasswordOrKeepExisting(request.BindPassword), request.BaseDn!,
```

Sınıfın sonuna ekle:
```csharp
    /// <summary>Boş şifre "değiştirme" anlamına gelir; domain mevcut değeri korur.</summary>
    private string? EncryptBindPasswordOrKeepExisting(string? bindPassword)
        => string.IsNullOrWhiteSpace(bindPassword) ? null : settingsEncryptor.Encrypt(bindPassword);
```

- [ ] **Step 3: LdapService'i şifre çözecek şekilde güncelle**

`backend/src/EforTakip.Infrastructure/Ldap/LdapService.cs`:

using ekle:
```csharp
using EforTakip.Application.Common.Interfaces;
```

Sınıfı primary constructor'a çevir:
```csharp
public sealed class LdapService(ISettingsEncryptor settingsEncryptor) : ILdapService
```

`CreateConnection` metodunu static olmaktan çıkar ve şifreyi çöz:
```csharp
    private LdapConnection CreateConnection(Directory directory)
    {
        var identifier = new LdapDirectoryIdentifier(
            directory.Hostname, directory.Port, fullyQualifiedDnsHostName: false, connectionless: false);

        var bindPassword = string.IsNullOrEmpty(directory.BindPasswordEncrypted)
            ? string.Empty
            : settingsEncryptor.Decrypt(directory.BindPasswordEncrypted);

        var credential = new NetworkCredential(directory.BindUsername, bindPassword);

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
```

`SearchUsers` metodu `CreateConnection` çağırdığı için o da static olmaktan çıkarılmalı:
```csharp
    private List<LdapUser> SearchUsers(
        Directory directory, IReadOnlyCollection<string> extraAttributeNames, CancellationToken cancellationToken)
```

- [ ] **Step 4: Mevcut testleri yeni ctor'a uyarla**

`CreateDirectoryCommandHandlerTests.cs` — alan ekle:
```csharp
    private readonly ISettingsEncryptor _settingsEncryptor = Substitute.For<ISettingsEncryptor>();
```
ve `using EforTakip.Application.Common.Interfaces;` mevcut. Her `new CreateDirectoryCommandHandler(_repository, _unitOfWork)` çağrısını şu hâle getir:
```csharp
        var handler = new CreateDirectoryCommandHandler(_repository, _unitOfWork, _settingsEncryptor);
```

`UpdateDirectoryCommandHandlerTests.cs` — aynı şekilde alan ekle ve her `new UpdateDirectoryCommandHandler(_repository, _unitOfWork)` çağrısını şu hâle getir:
```csharp
        var handler = new UpdateDirectoryCommandHandler(_repository, _unitOfWork, _settingsEncryptor);
```

- [ ] **Step 5: Şifrelemenin çağrıldığını doğrulayan test ekle**

`CreateDirectoryCommandHandlerTests.cs` — sınıfın sonuna ekle:
```csharp
    [Fact]
    public async Task Handle_ActiveDirectory_EncryptsBindPasswordBeforeStoring()
    {
        _settingsEncryptor.Encrypt("gizli").Returns("ENCRYPTED");
        var handler = new CreateDirectoryCommandHandler(_repository, _unitOfWork, _settingsEncryptor);
        var command = new CreateDirectoryCommand(
            "AD", DirectorySource.ActiveDirectory, "Microsoft Active Directory",
            "kizilay.local", 389, false, "u", "gizli", "DC=kizilay,DC=local", null, null,
            DirectoryPermission.ReadOnly, "user", "(x)", "sAMAccountName", "cn",
            "givenName", "sn", "displayName", "mail", "objectGUID", SyncScheduleKind.Off, 0);

        await handler.Handle(command, CancellationToken.None);

        await _repository.Received(1).AddAsync(
            Arg.Is<Directory>(d => d.BindPasswordEncrypted == "ENCRYPTED"), Arg.Any<CancellationToken>());
    }
```

- [ ] **Step 6: Testleri çalıştır**

Run: `dotnet test backend/tests/EforTakip.Application.Tests/EforTakip.Application.Tests.csproj --filter Directory`
Expected: PASS (mevcut Directory testleri + yeni şifreleme testi).

- [ ] **Step 7: Commit**

```bash
git add backend/src/EforTakip.Application/Directories/ backend/src/EforTakip.Infrastructure/Ldap/ backend/tests/EforTakip.Application.Tests/Directories/
git commit -m "feat: encrypt directory bind password at rest"
```

---

## Task 3: AD kullanıcı kimlik doğrulaması (LDAP bind)

**Files:**
- Modify: `backend/src/EforTakip.Application/Directories/Ldap/ILdapService.cs`
- Modify: `backend/src/EforTakip.Infrastructure/Ldap/LdapService.cs`

**Interfaces:**
- Produces: `ILdapService.AuthenticateAsync(Directory directory, string username, string password, CancellationToken cancellationToken)` → `Task<bool>`

**Yaklaşım — ara-sonra-bağlan (search-then-bind):** Microsoft AD'de simple bind, çıplak `sAMAccountName` ile çalışmaz; `DOMAIN\kullanıcı` veya UPN gerekir. Kullanıcının DN'ini saklamak yerine servis hesabıyla arayıp DN'i bulur, sonra o DN ile kullanıcının şifresini deneriz. Bu, kullanıcı adı biçiminden bağımsız olarak çalışır.

- [ ] **Step 1: Sözleşmeye metodu ekle**

`ILdapService.cs` — interface içine ekle:
```csharp
    /// <summary>
    /// Kullanıcının dizindeki şifresini doğrular. Şifre hiçbir yerde saklanmaz veya loglanmaz.
    /// </summary>
    Task<bool> AuthenticateAsync(
        Directory directory, string username, string password, CancellationToken cancellationToken);
```

- [ ] **Step 2: LdapService'e implementasyonu ekle**

`LdapService.cs` — `SearchUsersAsync` metodundan sonra ekle:
```csharp
    public Task<bool> AuthenticateAsync(
        Directory directory, string username, string password, CancellationToken cancellationToken)
        => Task.Run(() =>
        {
            // Boş şifre ile simple bind, sunucuda anonim bind'e dönüşüp "başarılı" sayılabilir.
            // Bu bir kimlik doğrulama atlatmasıdır — bind denemeden önce reddedilir.
            if (string.IsNullOrWhiteSpace(password))
                return false;

            try
            {
                var userDn = FindUserDistinguishedName(directory, username);
                if (userDn is null)
                    return false;

                var identifier = new LdapDirectoryIdentifier(
                    directory.Hostname, directory.Port, fullyQualifiedDnsHostName: false, connectionless: false);

                using var connection = new LdapConnection(
                    identifier, new NetworkCredential(userDn, password), AuthType.Basic)
                {
                    Timeout = ConnectionTimeout
                };
                connection.SessionOptions.ProtocolVersion = 3;
                connection.SessionOptions.SecureSocketLayer = directory.UseSsl;

                connection.Bind();
                return true;
            }
            catch (LdapException)
            {
                // Hatalı şifre de dahil tüm bind hataları "doğrulanamadı" demektir.
                return false;
            }
        }, cancellationToken);

    /// <summary>Servis hesabıyla bağlanıp kullanıcının DN'ini bulur.</summary>
    private string? FindUserDistinguishedName(Directory directory, string username)
    {
        using var connection = CreateConnection(directory);
        connection.Bind();

        var searchBase = BuildSearchBase(directory);
        var usernameAttribute = string.IsNullOrWhiteSpace(directory.UsernameAttribute)
            ? "sAMAccountName"
            : directory.UsernameAttribute;

        var filter = $"({usernameAttribute}={EscapeLdapFilterValue(username)})";
        var request = new SearchRequest(searchBase, filter, SearchScope.Subtree, usernameAttribute);

        var response = (SearchResponse)connection.SendRequest(request);
        if (response.Entries.Count != 1)
            return null;

        return response.Entries[0].DistinguishedName;
    }

    /// <summary>RFC 4515 — kullanıcı girdisinin LDAP filtresini bozmasını/enjeksiyonu önler.</summary>
    private static string EscapeLdapFilterValue(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            switch (c)
            {
                case '\\': builder.Append("\\5c"); break;
                case '*': builder.Append("\\2a"); break;
                case '(': builder.Append("\\28"); break;
                case ')': builder.Append("\\29"); break;
                case '\0': builder.Append("\\00"); break;
                case '/': builder.Append("\\2f"); break;
                default: builder.Append(c); break;
            }
        }
        return builder.ToString();
    }
```

- [ ] **Step 3: Derle**

Run: `dotnet build backend/EforTakip.sln`
Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add backend/src/EforTakip.Application/Directories/Ldap/ backend/src/EforTakip.Infrastructure/Ldap/
git commit -m "feat: add LDAP user authentication via search-then-bind"
```

---

## Task 4: JWT token servisi

**Files:**
- Create: `backend/src/EforTakip.Application/Common/Interfaces/ITokenService.cs`
- Create: `backend/src/EforTakip.Application/Common/Models/AuthenticatedUser.cs`
- Create: `backend/src/EforTakip.Infrastructure/Security/JwtOptions.cs`
- Create: `backend/src/EforTakip.Infrastructure/Security/JwtTokenService.cs`

**Interfaces:**
- Produces:
  - `AuthenticatedUser(Guid Id, string Username, string? DisplayName, Guid DirectoryId, DirectorySource Source)` — record
  - `ITokenService.CreateToken(AuthenticatedUser user)` → `(string Token, DateTime ExpiresAtUtc)`
  - `JwtOptions { SigningKey, Issuer, Audience, ExpiryMinutes }`

- [ ] **Step 1: Application sözleşmelerini yaz**

`AuthenticatedUser.cs`:
```csharp
using EforTakip.Domain.Directories;

namespace EforTakip.Application.Common.Models;

public sealed record AuthenticatedUser(
    Guid Id,
    string Username,
    string? DisplayName,
    Guid DirectoryId,
    DirectorySource Source);
```

`ITokenService.cs`:
```csharp
using EforTakip.Application.Common.Models;

namespace EforTakip.Application.Common.Interfaces;

public interface ITokenService
{
    (string Token, DateTime ExpiresAtUtc) CreateToken(AuthenticatedUser user);
}
```

- [ ] **Step 2: JwtBearer paketini API projesine ekle**

`backend/src/EforTakip.Api/EforTakip.Api.csproj` — paket ItemGroup'una ekle:
```xml
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.29" />
```

Run: `dotnet restore backend/src/EforTakip.Api/EforTakip.Api.csproj`
Expected: Restore başarılı.

- [ ] **Step 3: JwtOptions ve JwtTokenService'i yaz**

`backend/src/EforTakip.Infrastructure/Security/JwtOptions.cs`:
```csharp
namespace EforTakip.Infrastructure.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>En az 32 karakter. Environment variable / secret üzerinden gelir.</summary>
    public string SigningKey { get; set; } = string.Empty;

    public string Issuer { get; set; } = "Mesainame";

    public string Audience { get; set; } = "Mesainame";

    public int ExpiryMinutes { get; set; } = 480;
}
```

`backend/src/EforTakip.Infrastructure/Security/JwtTokenService.cs`:
```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Common.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EforTakip.Infrastructure.Security;

public sealed class JwtTokenService : ITokenService
{
    public const int MinimumSigningKeyLength = 32;

    private readonly JwtOptions _options;
    private readonly SigningCredentials _signingCredentials;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;

        if (string.IsNullOrWhiteSpace(_options.SigningKey))
            throw new InvalidOperationException("Jwt:SigningKey tanımlı değil. Token üretilemez.");

        if (_options.SigningKey.Length < MinimumSigningKeyLength)
            throw new InvalidOperationException(
                $"Jwt:SigningKey en az {MinimumSigningKeyLength} karakter olmalıdır.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        _signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    }

    public (string Token, DateTime ExpiresAtUtc) CreateToken(AuthenticatedUser user)
    {
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_options.ExpiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new("directory_id", user.DirectoryId.ToString()),
            new("directory_source", user.Source.ToString())
        };

        if (!string.IsNullOrWhiteSpace(user.DisplayName))
            claims.Add(new Claim("display_name", user.DisplayName));

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAtUtc,
            signingCredentials: _signingCredentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
    }
}
```

- [ ] **Step 4: Derle**

Run: `dotnet build backend/EforTakip.sln`
Expected: Build succeeded. (`System.IdentityModel.Tokens.Jwt` JwtBearer paketiyle transitif gelir; Infrastructure derlenmezse aynı paketi Infrastructure'a da ekle.)

- [ ] **Step 5: Commit**

```bash
git add backend/src/EforTakip.Application/Common/ backend/src/EforTakip.Infrastructure/Security/ backend/src/EforTakip.Api/EforTakip.Api.csproj
git commit -m "feat: add JWT token service"
```

---

## Task 5: Internal kullanıcı oluşturma

**Files:**
- Create: `backend/src/EforTakip.Application/Directories/Commands/CreateInternalUser/CreateInternalUserCommand.cs`
- Create: `backend/src/EforTakip.Application/Directories/Commands/CreateInternalUser/CreateInternalUserCommandValidator.cs`
- Create: `backend/src/EforTakip.Application/Directories/Commands/CreateInternalUser/CreateInternalUserCommandHandler.cs`
- Test: `backend/tests/EforTakip.Application.Tests/Directories/Commands/CreateInternalUserCommandHandlerTests.cs`

**Interfaces:**
- Consumes: `IApplicationDbContext`, `IRepository<Directory>`, `IUnitOfWork`, `IPasswordHasher`, `DirectoryUser.CreateInternal`.
- Produces: `CreateInternalUserCommand(Guid DirectoryId, string Username, string Password, string? FirstName, string? LastName, string? DisplayName, string? Email) : IRequest<Guid>`

**Kurallar:** Hedef dizin `Internal` kaynaklı olmalı; kullanıcı adı sistem genelinde tekil olmalı.

- [ ] **Step 1: Command ve validator'ı yaz**

`CreateInternalUserCommand.cs`:
```csharp
using MediatR;

namespace EforTakip.Application.Directories.Commands.CreateInternalUser;

public sealed record CreateInternalUserCommand(
    Guid DirectoryId,
    string Username,
    string Password,
    string? FirstName,
    string? LastName,
    string? DisplayName,
    string? Email) : IRequest<Guid>;
```

`CreateInternalUserCommandValidator.cs`:
```csharp
using FluentValidation;

namespace EforTakip.Application.Directories.Commands.CreateInternalUser;

public sealed class CreateInternalUserCommandValidator : AbstractValidator<CreateInternalUserCommand>
{
    public CreateInternalUserCommandValidator()
    {
        RuleFor(x => x.DirectoryId).NotEmpty().WithMessage("Dizin seçilmelidir.");

        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Kullanıcı adı zorunludur.")
            .MaximumLength(150).WithMessage("Kullanıcı adı en fazla 150 karakter olabilir.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Şifre zorunludur.")
            .MinimumLength(8).WithMessage("Şifre en az 8 karakter olmalıdır.")
            .MaximumLength(128).WithMessage("Şifre en fazla 128 karakter olabilir.");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}
```

- [ ] **Step 2: Testi yaz**

`CreateInternalUserCommandHandlerTests.cs`:
```csharp
using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Directories.Commands.CreateInternalUser;
using EforTakip.Domain.Directories;
using EforTakip.Domain.Exceptions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Directory = EforTakip.Domain.Directories.Directory;

namespace EforTakip.Application.Tests.Directories.Commands;

public class CreateInternalUserCommandHandlerTests : IAsyncDisposable
{
    private readonly IRepository<Directory> _directoryRepository = Substitute.For<IRepository<Directory>>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly TestDbContext _db;

    public CreateInternalUserCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase($"internal-user-tests-{Guid.NewGuid()}")
            .Options;
        _db = new TestDbContext(options);

        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(callInfo => _db.SaveChangesAsync(callInfo.Arg<CancellationToken>()));
        _passwordHasher.Hash(Arg.Any<string>()).Returns("HASHED");
    }

    public async ValueTask DisposeAsync() => await _db.DisposeAsync();

    private CreateInternalUserCommandHandler CreateHandler()
        => new(_db, _directoryRepository, _passwordHasher, _unitOfWork);

    private static CreateInternalUserCommand Command(Guid directoryId, string username = "sanal.kullanici") =>
        new(directoryId, username, "GucluSifre123", "Sanal", "Kullanıcı", "Sanal Kullanıcı", null);

    [Fact]
    public async Task Handle_ValidCommand_CreatesUserWithHashedPassword()
    {
        var directory = Directory.CreateInternal("Internal Users", 0);
        _directoryRepository.GetByIdAsync(directory.Id, Arg.Any<CancellationToken>()).Returns(directory);

        var result = await CreateHandler().Handle(Command(directory.Id), CancellationToken.None);

        result.Should().NotBeEmpty();
        var user = _db.DirectoryUsers.Single();
        user.Username.Should().Be("sanal.kullanici");
        user.Source.Should().Be(DirectorySource.Internal);
        user.PasswordHash.Should().Be("HASHED");
    }

    [Fact]
    public async Task Handle_PlainPasswordIsNeverStored()
    {
        var directory = Directory.CreateInternal("Internal Users", 0);
        _directoryRepository.GetByIdAsync(directory.Id, Arg.Any<CancellationToken>()).Returns(directory);

        await CreateHandler().Handle(Command(directory.Id), CancellationToken.None);

        _db.DirectoryUsers.Single().PasswordHash.Should().NotBe("GucluSifre123");
        _passwordHasher.Received(1).Hash("GucluSifre123");
    }

    [Fact]
    public async Task Handle_ActiveDirectoryDirectory_ThrowsBusinessRule()
    {
        var directory = Directory.CreateActiveDirectory(
            "AD", "Microsoft Active Directory", "kizilay.local", 389, false, "u", "p",
            "DC=kizilay,DC=local", null, null, DirectoryPermission.ReadOnly, "user", "(x)",
            "sAMAccountName", "cn", "givenName", "sn", "displayName", "mail", "objectGUID",
            SyncScheduleKind.Off, 0);
        _directoryRepository.GetByIdAsync(directory.Id, Arg.Any<CancellationToken>()).Returns(directory);

        var act = async () => await CreateHandler().Handle(Command(directory.Id), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleValidationException>();
    }

    [Fact]
    public async Task Handle_DuplicateUsername_ThrowsBusinessRule()
    {
        var directory = Directory.CreateInternal("Internal Users", 0);
        _directoryRepository.GetByIdAsync(directory.Id, Arg.Any<CancellationToken>()).Returns(directory);
        _db.DirectoryUsers.Add(DirectoryUser.CreateInternal(
            directory.Id, "sanal.kullanici", null, null, null, null, "HASHED"));
        await _db.SaveChangesAsync();

        var act = async () => await CreateHandler().Handle(Command(directory.Id), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleValidationException>();
    }

    [Fact]
    public async Task Handle_NonExistingDirectory_ThrowsNotFound()
    {
        _directoryRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Directory?)null);

        var act = async () => await CreateHandler().Handle(Command(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
```

- [ ] **Step 3: Testi çalıştır, başarısız olduğunu doğrula**

Run: `dotnet test backend/tests/EforTakip.Application.Tests/EforTakip.Application.Tests.csproj --filter CreateInternalUser`
Expected: FAIL — `CreateInternalUserCommandHandler` bulunamıyor.

- [ ] **Step 4: Handler'ı yaz**

`CreateInternalUserCommandHandler.cs`:
```csharp
using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Directories;
using EforTakip.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Directory = EforTakip.Domain.Directories.Directory;

namespace EforTakip.Application.Directories.Commands.CreateInternalUser;

public sealed class CreateInternalUserCommandHandler(
    IApplicationDbContext db,
    IRepository<Directory> directoryRepository,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateInternalUserCommand, Guid>
{
    public async Task<Guid> Handle(CreateInternalUserCommand request, CancellationToken cancellationToken)
    {
        var directory = await directoryRepository.GetByIdAsync(request.DirectoryId, cancellationToken)
            ?? throw new NotFoundException(nameof(Directory), request.DirectoryId);

        if (directory.Source != DirectorySource.Internal)
            throw new BusinessRuleValidationException(
                "Kullanıcı yalnızca internal dizinlerde elle oluşturulabilir. AD kullanıcıları senkronizasyonla gelir.");

        var username = request.Username.Trim();

        var usernameTaken = await db.DirectoryUsers
            .AnyAsync(u => u.Username.ToLower() == username.ToLower(), cancellationToken);

        if (usernameTaken)
            throw new BusinessRuleValidationException($"'{username}' kullanıcı adı zaten kullanılıyor.");

        var user = DirectoryUser.CreateInternal(
            directory.Id, username, request.FirstName, request.LastName,
            request.DisplayName, request.Email, passwordHasher.Hash(request.Password));

        db.DirectoryUsers.Add(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return user.Id;
    }
}
```

- [ ] **Step 5: Testi çalıştır, geçtiğini doğrula**

Run: `dotnet test backend/tests/EforTakip.Application.Tests/EforTakip.Application.Tests.csproj --filter CreateInternalUser`
Expected: PASS (5 test).

- [ ] **Step 6: Commit**

```bash
git add backend/src/EforTakip.Application/Directories/Commands/CreateInternalUser/ backend/tests/EforTakip.Application.Tests/Directories/Commands/CreateInternalUserCommandHandlerTests.cs
git commit -m "feat: add internal user creation with hashed password"
```

---

## Task 6: Login komutu

**Files:**
- Create: `backend/src/EforTakip.Application/Auth/Dtos/LoginResultDto.cs`
- Create: `backend/src/EforTakip.Application/Auth/Commands/Login/LoginCommand.cs`
- Create: `backend/src/EforTakip.Application/Auth/Commands/Login/LoginCommandValidator.cs`
- Create: `backend/src/EforTakip.Application/Auth/Commands/Login/LoginCommandHandler.cs`
- Create: `backend/src/EforTakip.Application/Common/Exceptions/AuthenticationFailedException.cs`
- Test: `backend/tests/EforTakip.Application.Tests/Auth/LoginCommandHandlerTests.cs`
- Modify: `backend/src/EforTakip.Api/Middleware/GlobalExceptionHandler.cs`

**Interfaces:**
- Consumes: `IApplicationDbContext`, `IRepository<Directory>`, `IPasswordHasher`, `ILdapService`, `ITokenService`, `AuthenticatedUser`.
- Produces:
  - `AuthenticationFailedException` — 401'e eşlenir
  - `LoginCommand(string Username, string Password) : IRequest<LoginResultDto>`
  - `LoginResultDto { Token, ExpiresAtUtc, UserId, Username, DisplayName, Source }`

**Akış:** kullanıcıyı bul → yoksa/pasifse hata → Internal ise hash doğrula, AD ise dizinini yükleyip LDAP bind → başarılıysa JWT üret. Her başarısızlık **aynı** mesajı döner.

- [ ] **Step 1: Exception ve DTO'yu yaz**

`AuthenticationFailedException.cs`:
```csharp
namespace EforTakip.Application.Common.Exceptions;

/// <summary>
/// Kimlik doğrulama başarısız. Mesaj bilinçli olarak geneldir — kullanıcının var olup
/// olmadığı, pasif olduğu veya hangi adımda başarısız olunduğu sızdırılmaz.
/// </summary>
public sealed class AuthenticationFailedException()
    : Exception("Kullanıcı adı veya şifre hatalı.");
```

`LoginResultDto.cs`:
```csharp
using EforTakip.Domain.Directories;

namespace EforTakip.Application.Auth.Dtos;

public sealed class LoginResultDto
{
    public string Token { get; init; } = default!;
    public DateTime ExpiresAtUtc { get; init; }
    public Guid UserId { get; init; }
    public string Username { get; init; } = default!;
    public string? DisplayName { get; init; }
    public DirectorySource Source { get; init; }
}
```

- [ ] **Step 2: Command ve validator'ı yaz**

`LoginCommand.cs`:
```csharp
using EforTakip.Application.Auth.Dtos;
using MediatR;

namespace EforTakip.Application.Auth.Commands.Login;

public sealed record LoginCommand(string Username, string Password) : IRequest<LoginResultDto>;
```

`LoginCommandValidator.cs`:
```csharp
using FluentValidation;

namespace EforTakip.Application.Auth.Commands.Login;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty().WithMessage("Kullanıcı adı zorunludur.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Şifre zorunludur.");
    }
}
```

- [ ] **Step 3: Testi yaz**

`backend/tests/EforTakip.Application.Tests/Auth/LoginCommandHandlerTests.cs`:
```csharp
using EforTakip.Application.Auth.Commands.Login;
using EforTakip.Application.Common.Exceptions;
using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Common.Models;
using EforTakip.Application.Directories.Ldap;
using EforTakip.Application.Tests.Directories.Commands;
using EforTakip.Domain.Directories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Directory = EforTakip.Domain.Directories.Directory;

namespace EforTakip.Application.Tests.Auth;

public class LoginCommandHandlerTests : IAsyncDisposable
{
    private readonly IRepository<Directory> _directoryRepository = Substitute.For<IRepository<Directory>>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly ILdapService _ldapService = Substitute.For<ILdapService>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly TestDbContext _db;

    public LoginCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase($"login-tests-{Guid.NewGuid()}")
            .Options;
        _db = new TestDbContext(options);

        _tokenService.CreateToken(Arg.Any<AuthenticatedUser>())
            .Returns(("TOKEN", DateTime.UtcNow.AddHours(8)));
    }

    public async ValueTask DisposeAsync() => await _db.DisposeAsync();

    private LoginCommandHandler CreateHandler()
        => new(_db, _directoryRepository, _passwordHasher, _ldapService, _tokenService);

    private static Directory InternalDirectory() => Directory.CreateInternal("Internal Users", 0);

    private static Directory AdDirectory() =>
        Directory.CreateActiveDirectory(
            "Kızılay AD", "Microsoft Active Directory", "kizilay.local", 389, false, "u", "p",
            "DC=kizilay,DC=local", null, null, DirectoryPermission.ReadOnly, "user", "(x)",
            "sAMAccountName", "cn", "givenName", "sn", "displayName", "mail", "objectGUID",
            SyncScheduleKind.Off, 0);

    /// <summary>Kullanıcıyı ekler ve dizinini repository mock'una tanıtır (handler dizini yükler).</summary>
    private async Task<DirectoryUser> AddInternalUserAsync(Directory directory, string username = "sanal.kullanici")
    {
        var user = DirectoryUser.CreateInternal(
            directory.Id, username, "Sanal", "Kullanıcı", "Sanal Kullanıcı", null, "HASHED");
        _db.DirectoryUsers.Add(user);
        await _db.SaveChangesAsync();
        _directoryRepository.GetByIdAsync(directory.Id, Arg.Any<CancellationToken>()).Returns(directory);
        return user;
    }

    private async Task<DirectoryUser> AddAdUserAsync(Directory directory, string username = "serkan.gultepe")
    {
        var user = DirectoryUser.CreateFromActiveDirectory(
            directory.Id, username, "Serkan", "Gültepe", "Serkan Gültepe", null, "guid-1");
        _db.DirectoryUsers.Add(user);
        await _db.SaveChangesAsync();
        _directoryRepository.GetByIdAsync(directory.Id, Arg.Any<CancellationToken>()).Returns(directory);
        return user;
    }

    [Fact]
    public async Task Handle_InternalUserWithCorrectPassword_ReturnsToken()
    {
        var user = await AddInternalUserAsync(InternalDirectory());
        _passwordHasher.Verify("dogru-sifre", "HASHED").Returns(true);

        var result = await CreateHandler().Handle(
            new LoginCommand("sanal.kullanici", "dogru-sifre"), CancellationToken.None);

        result.Token.Should().Be("TOKEN");
        result.UserId.Should().Be(user.Id);
        result.Source.Should().Be(DirectorySource.Internal);
    }

    [Fact]
    public async Task Handle_InternalUserWithWrongPassword_ThrowsAuthenticationFailed()
    {
        await AddInternalUserAsync(InternalDirectory());
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var act = async () => await CreateHandler().Handle(
            new LoginCommand("sanal.kullanici", "yanlis"), CancellationToken.None);

        await act.Should().ThrowAsync<AuthenticationFailedException>();
    }

    [Fact]
    public async Task Handle_AdUserWithValidCredentials_ReturnsToken()
    {
        var directory = AdDirectory();
        var user = await AddAdUserAsync(directory);
        _ldapService.AuthenticateAsync(directory, "serkan.gultepe", "ad-sifre", Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await CreateHandler().Handle(
            new LoginCommand("serkan.gultepe", "ad-sifre"), CancellationToken.None);

        result.Token.Should().Be("TOKEN");
        result.UserId.Should().Be(user.Id);
        result.Source.Should().Be(DirectorySource.ActiveDirectory);
    }

    [Fact]
    public async Task Handle_AdUserRejectedByDirectory_ThrowsAuthenticationFailed()
    {
        await AddAdUserAsync(AdDirectory());
        _ldapService.AuthenticateAsync(
                Arg.Any<Directory>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var act = async () => await CreateHandler().Handle(
            new LoginCommand("serkan.gultepe", "yanlis"), CancellationToken.None);

        await act.Should().ThrowAsync<AuthenticationFailedException>();
    }

    [Fact]
    public async Task Handle_AdUserPasswordIsNeverCheckedLocally()
    {
        await AddAdUserAsync(AdDirectory());
        _ldapService.AuthenticateAsync(
                Arg.Any<Directory>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);

        await CreateHandler().Handle(new LoginCommand("serkan.gultepe", "ad-sifre"), CancellationToken.None);

        _passwordHasher.DidNotReceive().Verify(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_InactiveUser_ThrowsAuthenticationFailed()
    {
        var user = await AddInternalUserAsync(InternalDirectory());
        user.Deactivate();
        await _db.SaveChangesAsync();
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        var act = async () => await CreateHandler().Handle(
            new LoginCommand("sanal.kullanici", "dogru-sifre"), CancellationToken.None);

        await act.Should().ThrowAsync<AuthenticationFailedException>();
    }

    [Fact]
    public async Task Handle_InactiveDirectory_ThrowsAuthenticationFailed()
    {
        var directory = InternalDirectory();
        await AddInternalUserAsync(directory);
        directory.Deactivate();
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        var act = async () => await CreateHandler().Handle(
            new LoginCommand("sanal.kullanici", "dogru-sifre"), CancellationToken.None);

        await act.Should().ThrowAsync<AuthenticationFailedException>();
    }

    [Fact]
    public async Task Handle_UnknownUsername_ThrowsAuthenticationFailedWithSameMessage()
    {
        var act = async () => await CreateHandler().Handle(
            new LoginCommand("olmayan.kullanici", "herhangi"), CancellationToken.None);

        var assertion = await act.Should().ThrowAsync<AuthenticationFailedException>();
        assertion.Which.Message.Should().Be("Kullanıcı adı veya şifre hatalı.");
    }

    [Fact]
    public async Task Handle_UsernameIsMatchedCaseInsensitively()
    {
        await AddInternalUserAsync(InternalDirectory());
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        var result = await CreateHandler().Handle(
            new LoginCommand("Sanal.Kullanici", "dogru-sifre"), CancellationToken.None);

        result.Username.Should().Be("sanal.kullanici");
    }
}
```

- [ ] **Step 4: Testi çalıştır, başarısız olduğunu doğrula**

Run: `dotnet test backend/tests/EforTakip.Application.Tests/EforTakip.Application.Tests.csproj --filter LoginCommandHandler`
Expected: FAIL — `LoginCommandHandler` bulunamıyor.

- [ ] **Step 5: Handler'ı yaz**

`LoginCommandHandler.cs`:
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
        var username = request.Username.Trim().ToLower();

        var user = await db.DirectoryUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username, cancellationToken);

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

        var (token, expiresAtUtc) = tokenService.CreateToken(new AuthenticatedUser(
            user.Id, user.Username, user.DisplayName, user.DirectoryId, user.Source));

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

- [ ] **Step 6: Middleware'e 401 eşlemesini ekle**

`backend/src/EforTakip.Api/Middleware/GlobalExceptionHandler.cs` — `DirectoryConnectionException` bloğundan önce ekle:
```csharp
            Application.Common.Exceptions.AuthenticationFailedException authenticationFailedException => new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Kimlik doğrulama başarısız",
                Detail = authenticationFailedException.Message
            },
```

- [ ] **Step 7: Testi çalıştır, geçtiğini doğrula**

Run: `dotnet test backend/tests/EforTakip.Application.Tests/EforTakip.Application.Tests.csproj --filter LoginCommandHandler`
Expected: PASS (9 test).

- [ ] **Step 8: Commit**

```bash
git add backend/src/EforTakip.Application/Auth/ backend/src/EforTakip.Application/Common/Exceptions/ backend/src/EforTakip.Api/Middleware/ backend/tests/EforTakip.Application.Tests/Auth/
git commit -m "feat: add login command for internal and directory users"
```

---

## Task 7: AuthController, DI kayıtları ve JWT middleware

**Files:**
- Create: `backend/src/EforTakip.Api/Controllers/v1/AuthController.cs`
- Modify: `backend/src/EforTakip.Api/Controllers/v1/DirectoryUsersController.cs`
- Modify: `backend/src/EforTakip.Infrastructure/DependencyInjection.cs`
- Modify: `backend/src/EforTakip.Api/Extensions/ApiServiceCollectionExtensions.cs`
- Modify: `backend/src/EforTakip.Api/Program.cs`
- Modify: `backend/src/EforTakip.Api/appsettings.Development.json`

**Interfaces:**
- Produces: `POST /api/v1/auth/login`, `POST /api/v1/directoryusers/internal`; JWT authentication pipeline'a eklenir (endpoint'ler henüz korunmaz).

- [ ] **Step 1: Infrastructure DI kayıtlarını güncelle**

`backend/src/EforTakip.Infrastructure/DependencyInjection.cs`:
```csharp
using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Directories.Ldap;
using EforTakip.Infrastructure.Ldap;
using EforTakip.Infrastructure.Security;
using EforTakip.Infrastructure.Sync;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EforTakip.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SecurityOptions>(configuration.GetSection(SecurityOptions.SectionName));
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<ISettingsEncryptor, AesSettingsEncryptor>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        services.AddScoped<ILdapService, LdapService>();
        services.AddHostedService<DirectorySyncBackgroundService>();

        return services;
    }
}
```

`Program.cs` içindeki `.AddInfrastructure()` çağrısını `.AddInfrastructure(builder.Configuration)` yap.

- [ ] **Step 2: JWT authentication'ı ekle**

`backend/src/EforTakip.Api/EforTakip.Api.csproj` — Infrastructure'a proje referansı zaten var; `ApiServiceCollectionExtensions.cs` using'lerine ekle:
```csharp
using System.Text;
using EforTakip.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
```

`services.AddExceptionHandler<GlobalExceptionHandler>();` satırından önce ekle:
```csharp
        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

        // Fail fast: anahtar yoksa uygulama açılmamalı, sessizce güvensiz çalışmamalı.
        if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey))
            throw new InvalidOperationException(
                "Jwt:SigningKey tanımlı değil. Environment variable veya user-secrets ile sağlayın.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        services.AddAuthorization();
```

Swagger'da token girilebilmesi için `services.AddSwaggerGen();` çağrısını değiştir:
```csharp
        services.AddSwaggerGen(options =>
        {
            options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Description = "JWT token. Örnek: Bearer {token}"
            });

            options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
            {
                {
                    new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                    {
                        Reference = new Microsoft.OpenApi.Models.OpenApiReference
                        {
                            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });
```

- [ ] **Step 3: Program.cs'e UseAuthentication ekle**

`backend/src/EforTakip.Api/Program.cs` — `app.UseAuthorization();` satırından **önce** ekle:
```csharp
app.UseAuthentication();
```

- [ ] **Step 4: Geliştirme anahtarlarını ekle**

`backend/src/EforTakip.Api/appsettings.Development.json` — kök nesneye ekle:
```json
  "Jwt": {
    "SigningKey": "gelistirme-ortami-icin-en-az-32-karakterlik-anahtar",
    "Issuer": "Mesainame",
    "Audience": "Mesainame",
    "ExpiryMinutes": 480
  },
  "Security": {
    "SettingsEncryptionKey": "3q2+796tvu/erb7v3q2+796tvu/erb7v3q2+796tvu8="
  }
```

**Not:** Bunlar yalnızca geliştirme değerleridir. Üretimde `Jwt__SigningKey` ve `Security__SettingsEncryptionKey` environment variable'ları ile sağlanmalıdır; `appsettings.json`'a (Development olmayan) asla yazılmaz.

- [ ] **Step 5: AuthController'ı yaz**

`backend/src/EforTakip.Api/Controllers/v1/AuthController.cs`:
```csharp
using Asp.Versioning;
using EforTakip.Application.Auth.Commands.Login;
using EforTakip.Application.Auth.Dtos;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EforTakip.Api.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class AuthController(ISender mediator) : ControllerBase
{
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResultDto>> Login(
        LoginCommand command, CancellationToken cancellationToken)
        => Ok(await mediator.Send(command, cancellationToken));
}
```

- [ ] **Step 6: Internal kullanıcı oluşturma endpoint'ini ekle**

`backend/src/EforTakip.Api/Controllers/v1/DirectoryUsersController.cs` — using ekle:
```csharp
using EforTakip.Application.Directories.Commands.CreateInternalUser;
```

ve `GetById` metodundan sonra ekle:
```csharp
    [HttpPost("internal")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateInternal(
        CreateInternalUserCommand command, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id, version = "1.0" }, null);
    }
```

- [ ] **Step 7: Derle ve tüm testleri çalıştır**

Run:
```bash
dotnet build backend/EforTakip.sln
dotnet test backend/tests/EforTakip.Domain.Tests/EforTakip.Domain.Tests.csproj
dotnet test backend/tests/EforTakip.Application.Tests/EforTakip.Application.Tests.csproj
```
Expected: Build succeeded; Domain PASS; Application'da yalnızca bilinen 2 `LogWorkCommandHandlerTests` hatası kalır.

- [ ] **Step 8: Uçtan uca canlı doğrulama**

API'yi başlat (`dotnet run --project backend/src/EforTakip.Api --urls http://localhost:5298`), sonra:

1. Internal dizin oluştur:
```bash
curl -s -X POST http://localhost:5298/api/v1/directories -H "Content-Type: application/json" \
  -d '{"name":"Internal Users","source":0,"port":0,"permission":2,"syncSchedule":0,"sortOrder":0}'
```

2. Dizin kimliğini al ve internal kullanıcı oluştur:
```bash
DIR_ID=$(curl -s "http://localhost:5298/api/v1/directories" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)
curl -s -X POST http://localhost:5298/api/v1/directoryusers/internal -H "Content-Type: application/json" \
  -d "{\"directoryId\":\"$DIR_ID\",\"username\":\"sanal.kullanici\",\"password\":\"GucluSifre123\",\"firstName\":\"Sanal\",\"lastName\":\"Kullanici\",\"displayName\":\"Sanal Kullanici\"}" \
  -w "\nSTATUS:%{http_code}\n"
```
Expected: 201.

3. Doğru şifreyle giriş:
```bash
curl -s -X POST http://localhost:5298/api/v1/auth/login -H "Content-Type: application/json" \
  -d '{"username":"sanal.kullanici","password":"GucluSifre123"}' -w "\nSTATUS:%{http_code}\n"
```
Expected: 200, gövdede `token` alanı dolu.

4. Yanlış şifreyle giriş:
```bash
curl -s -X POST http://localhost:5298/api/v1/auth/login -H "Content-Type: application/json" \
  -d '{"username":"sanal.kullanici","password":"yanlis"}' -w "\nSTATUS:%{http_code}\n"
```
Expected: 401, `"detail":"Kullanıcı adı veya şifre hatalı."`

5. Olmayan kullanıcıyla giriş — **aynı** yanıtı vermeli:
```bash
curl -s -X POST http://localhost:5298/api/v1/auth/login -H "Content-Type: application/json" \
  -d '{"username":"olmayan","password":"herhangi"}' -w "\nSTATUS:%{http_code}\n"
```
Expected: 401, aynı mesaj (kullanıcı sayımı sızıntısı yok).

6. Logda şifre sızıntısı olmadığını doğrula:
```bash
grep -icE "GucluSifre123|password" logs/log-*.txt || echo "temiz"
```
Expected: Şifre değeri logda geçmiyor.

API'yi durdur.

- [ ] **Step 9: Commit**

```bash
git add backend/src/EforTakip.Api/ backend/src/EforTakip.Infrastructure/DependencyInjection.cs
git commit -m "feat: add auth endpoint and JWT authentication pipeline"
```

---

## Faz 3 Tamamlanma Kriteri

- [ ] Domain testleri geçiyor (69).
- [ ] `LoginCommandHandlerTests` (9) ve `CreateInternalUserCommandHandlerTests` (5) geçiyor.
- [ ] `dotnet build backend/EforTakip.sln` başarılı.
- [ ] Internal kullanıcı doğru şifreyle giriş yapabiliyor, token alıyor.
- [ ] Yanlış şifre, olmayan kullanıcı ve pasif kullanıcı **aynı** 401 mesajını alıyor.
- [ ] AD kullanıcısının şifresi local olarak doğrulanmıyor (test ile kanıtlı).
- [ ] Dizin bind şifresi veritabanında şifreli saklanıyor.
- [ ] Şifre hiçbir logda geçmiyor.
- [ ] `Jwt:SigningKey` yoksa uygulama açılışta hata veriyor.

## Bilinen Sınırlar (Faz 4'e devrediliyor)

- **Endpoint'ler hâlâ korumasız.** `[Authorize]` ve frontend token gönderimi Faz 4'te login sayfasıyla birlikte devreye alınır.
- Token yenileme (refresh token) yok — token süresi dolunca yeniden giriş gerekir. Gerçek ihtiyaç doğarsa eklenir (YAGNI).
- Faz 1–2'de oluşturulmuş dizinlerin bind şifresi düz metindir; bu dizinler yeniden kaydedilmelidir.
- Internal kullanıcının şifresini değiştirme/sıfırlama komutu yok; ihtiyaç doğduğunda eklenir.

## Sonraki Faz

- **Faz 4 — Frontend:** Kullanıcı Klasörü UI (dizin listesi/formu, global Alan Eşlemeleri, kullanıcı listesi ve kartı), `LoginPage`, token saklama ve API isteklerine ekleme, `[Authorize]` ile endpoint korumasının açılması.
