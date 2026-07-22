# Active Directory Entegrasyonu — Tasarım Dokümanı

**Tarih:** 2026-07-21
**Branch:** `feature/active-directory-integration`
**Kapsam:** Mesainame için çoklu AD/dizin tanımı, kullanıcı senkronizasyonu ve AD/internal login altyapısı.

## 1. Amaç ve Kapsam

Mesainame'ye kurumsal dizin (Active Directory / LDAP) entegrasyonu eklenir. Özellik uçtan uca çalışır:

- **Çoklu dizin tanımı:** Birden fazla AD/LDAP dizini tanımlanabilir. İhtiyaç halinde yeni dizinler eklenir.
- **Kullanıcı senkronizasyonu:** Dizinlerden kullanıcılar çekilip veritabanına yazılır (manuel + zamanlanmış).
- **Kimlik doğrulama:** Gerçek kişiler AD credentials ile, sanal (internal) kullanıcılar local şifre ile giriş yapar.
- **Login altyapısı:** Projede mevcut olmayan JWT tabanlı authentication bu kapsamda kurulur.

### Temel kararlar

- **Tekil kullanıcı:** Kullanıcılar sistem genelinde tekil `Username` ile temsil edilir (ör. `serkan.gultepe`), hangi dizinden gelirse gelsin.
- **Ortak attribute mapping:** Tüm dizinler aynı attribute mapping'i kullanır (global tanım).
- **Salt-okunur senkron veri:** AD'den senkronize edilen attribute değerleri sistemde düzenlenemez (read-only). Yalnızca tanım/mapping ekranından hangi alanların çekileceği yönetilir.
- **Dizin bazlı izin:** Her dizinin LDAP izin seviyesi (ReadOnly / ReadOnly+Local Groups / ReadWrite) vardır; sistemin AD'de değişiklik yapıp yapamayacağını bu belirler.

## 2. Veri Modeli

AD'den gelen veriler ayrı tablolarda tutulur. Beş tablo:

### 2.1 `Directory`
Tanımlanan her AD/dizin kaydı.

| Alan | Açıklama |
|------|----------|
| `Id` | Tekil kimlik |
| `Name` | Görünen ad (ör. "Active Directory server") |
| `Source` | `Internal` / `ActiveDirectory` |
| `DirectoryType` | Dizin tipi (ör. Microsoft Active Directory) |
| `Hostname` | LDAP sunucu adresi |
| `Port` | Port (varsayılan 389) |
| `UseSsl` | SSL kullanımı |
| `BindUsername` | Bağlantı kullanıcısı |
| `BindPasswordEncrypted` | Şifrelenmiş bind şifresi (düz metin değil) |
| `BaseDn` | Root DN |
| `AdditionalUserDn` | Kullanıcı arama kapsamı (opsiyonel) |
| `AdditionalGroupDn` | Grup arama kapsamı (opsiyonel) |
| `Permission` | `ReadOnly` / `ReadOnlyLocalGroups` / `ReadWrite` |
| `UserObjectClass` | Ör. `user` |
| `UserObjectFilter` | LDAP filtresi |
| `UsernameAttribute` | Ör. `sAMAccountName` |
| `UsernameRdnAttribute` | Ör. `cn` |
| `FirstNameAttribute` | Ör. `givenName` |
| `LastNameAttribute` | Ör. `sn` |
| `DisplayNameAttribute` | Ör. `displayName` |
| `EmailAttribute` | Ör. `mail` |
| `UniqueIdAttribute` | Ör. `objectGUID` |
| `SyncSchedule` | `Off` / `Hourly` / `Daily` / `Weekly` |
| `IsActive` | Aktif/pasif |
| `SortOrder` | Sıralama |

### 2.2 `DirectoryAttributeMapping`
Alan eşlemeleri. Tüm dizinler için ortak (global) tanım.

| Alan | Açıklama |
|------|----------|
| `Id` | Tekil kimlik |
| `AdAttributeName` | AD'deki alan adı (ör. `company`) |
| `SystemFieldName` | Sistemdeki ad (ör. `Kurum`) |
| `FieldType` | `text` / `user` |
| `IsSynced` | Bu alan senkronize edilsin mi |
| `SortOrder` | Sıralama |

### 2.3 `DirectoryUser`
Senkronize/internal kullanıcılar (birleşik kayıt).

| Alan | Açıklama |
|------|----------|
| `Id` | Tekil kimlik |
| `DirectoryId` | Hangi dizinden (FK → Directory) |
| `Source` | `Internal` / `ActiveDirectory` |
| `Username` | Sistem geneli tekil (`serkan.gultepe`) |
| `FirstName` | Ad |
| `LastName` | Soyad |
| `DisplayName` | Görünen ad |
| `Email` | E-posta |
| `ObjectGuid` | AD unique ID (yalnızca AD kullanıcıları) |
| `PasswordHash` | Yalnızca internal kullanıcılar (BCrypt) |
| `IsActive` | Aktif/pasif |
| `LastSyncedUtc` | Son senkron zamanı (yalnızca AD) |

**Not:** `Source` alanı internal ve AD kullanıcılarını net şekilde ayırır; `DirectoryId` kullanıcının hangi dizine ait olduğunu tutar.

### 2.4 `DirectoryUserAttribute`
Kullanıcının dinamik attribute değerleri (key-value). Hangi alanların çekileceği admin tarafından seçilebildiği için esnek yapı.

| Alan | Açıklama |
|------|----------|
| `Id` | Tekil kimlik |
| `DirectoryUserId` | FK → DirectoryUser |
| `AttributeMappingId` | FK → DirectoryAttributeMapping |
| `Value` | Attribute değeri |

## 3. Senkronizasyon ve Login Akışı

### 3.1 Senkronizasyon (AD → Sistem)

- **Manuel:** Admin, Kullanıcı Klasörü sayfasında bir dizinin "Şimdi Senkronize Et" butonuna basar.
- **Zamanlanmış:** `DirectorySyncBackgroundService` (BackgroundService), dizinin `SyncSchedule` ayarına göre (saatlik/günlük/haftalık) otomatik çalışır.

**Adımlar:**
1. Dizin ayarlarıyla AD'ye bağlan (LDAP bind).
2. `UserObjectFilter`'a uyan kullanıcıları çek.
3. Her kullanıcı için `ObjectGuid`'e göre eşleştir: varsa güncelle, yoksa ekle.
4. Yalnızca `IsSynced: true` olan attribute'ları çek, `DirectoryUserAttribute`'a yaz.
5. AD'de artık olmayan kullanıcıları pasif işaretle (`IsActive: false`), silme yapılmaz.
6. `LastSyncedUtc` güncelle.

### 3.2 Login akışı (tek form, otomatik tespit)

```
Kullanıcı username + şifre girer
        │
        ▼
Username'e göre DirectoryUser bul
        │
        ├─ Bulunamadı ──────────► "Kullanıcı adı veya şifre hatalı"
        │
        ├─ Source = Internal ──► PasswordHash ile doğrula (local, BCrypt)
        │
        └─ Source = ActiveDirectory
                 │
                 ▼
        DirectoryId'den AD ayarlarını al
                 │
                 ▼
        AD'ye bind et (username + şifre) ──► başarılı/başarısız
```

- Başarılı doğrulama → JWT token üretilir, frontend'e döner.
- **AD kullanıcı şifresi hiçbir zaman veritabanında saklanmaz.** Her login'de doğrudan AD'ye sorulur.

## 4. Backend Katman Yapısı

Mevcut Clean Architecture'a uyumlu; her sorumluluk kendi katmanında.

### Domain (`EforTakip.Domain/Directories/`)
- `Directory` — Aggregate root (dizin, ayarlar, iş kuralları).
- `DirectoryUser` — Aggregate root (senkronize/internal kullanıcı).
- `DirectoryAttributeMapping` — Alan eşleme.
- Value object'ler: `LdapConnectionSettings`, `LdapSchemaSettings`, `SyncSchedule`.
- Enum'lar: `DirectorySource` (Internal/ActiveDirectory), `DirectoryPermission` (ReadOnly/ReadOnlyLocalGroups/ReadWrite), `SyncScheduleKind`.

### Application (`EforTakip.Application/Directories/`)
- Command'lar: `CreateDirectory`, `UpdateDirectory`, `DeleteDirectory`, `TestConnection`, `SyncDirectory`, `Login`.
- Query'ler: `GetDirectories`, `GetDirectoryUsers`, `GetDirectoryUserDetail`.
- FluentValidation ile input doğrulama.
- Arayüzler: `ILdapService`, `IPasswordHasher`, `ITokenService`.

### Infrastructure (`EforTakip.Infrastructure/`)
- `LdapService` — LDAP bağlantı/query. **`System.DirectoryServices.Protocols`** (cross-platform, Docker/Linux uyumlu).
- `PasswordHasher` — Internal şifre hash (BCrypt).
- `TokenService` — JWT üretimi.
- `DirectorySyncBackgroundService` — Zamanlanmış senkronizasyon.
- `SettingsEncryptor` — Dizin bind şifresini şifreleyerek saklama.

### Persistence (`EforTakip.Persistence/`)
- EF Core Configuration'lar, Migration, Repository kayıtları.

### API (`EforTakip.Api/Controllers/v1/`)
- `DirectoriesController` — dizin CRUD, test, sync.
- `DirectoryUsersController` — kullanıcı listeleme/detay.
- `AuthController` — login endpoint.
- JWT authentication middleware kurulumu (Program.cs).

## 5. Frontend

### 5.1 Settings > Kullanıcı Yönetimi > Kullanıcı Klasörü

`AdminPage.tsx`'teki "Kullanıcı Yönetimi" sekmesine yeni bölüm: **Kullanıcı Klasörü**.

**Dizin listesi (ana görünüm):**
- Tanımlı dizinlerin tablosu (Ad, Tip, Hostname, Durum, Son senkron).
- "Yeni Dizin Ekle" butonu.
- Satır aksiyonları: Düzenle / Senkronize Et / Sil.

**Dizin ekle/düzenle formu (bölümler halinde):**
- Server Settings — Ad, tip, hostname, port, SSL, kullanıcı adı, şifre.
- LDAP Schema — Base DN, ek user/group DN.
- LDAP Permissions — ReadOnly / ReadOnly+Local / ReadWrite (radio).
- User Schema Settings — object class, filter, attribute isimleri.
- Sync Schedule — dropdown (kapalı / saatlik / günlük / haftalık).
- "Bağlantıyı Test Et" butonu.

**Attribute Mapping (global, ayrı bölüm):**
Alan eşlemeleri tüm dizinler için ortak olduğundan dizin formunun içinde değil, Kullanıcı Klasörü altında **tek bir global "Alan Eşlemeleri" bölümünde** yönetilir: AD alanı → sistem adı → tip → senkronize toggle (satır ekle/sil). Bir kez tanımlanır, tüm dizinlere uygulanır.

**Kullanıcı listesi ve kartı:**
- Bir dizine ait senkronize kullanıcılar (username, ad, email, kaynak, durum).
- Kullanıcıya tıklayınca Kullanıcı Kartı: tüm senkronize attribute'lar (company, department, title, manager, phone, mobile, city, email vb.). Tüm attribute'lar her yerde görünür.

### 5.2 Login sayfası
- Yeni `LoginPage.tsx` — tek form (username + şifre).
- Backend otomatik tespit eder (internal/AD); frontend yalnızca gönderir.
- Başarılı login → JWT token localStorage'da saklanır, API isteklerine eklenir.

### 5.3 Yapısal not
`AdminPage.tsx` mevcut section'ları tek dosyada tutuyor. Kullanıcı Klasörü ciddi bir UI (form + tablo + kart) olduğundan `components/admin/directory/` altında ayrı bileşenlere bölünür (`DirectoryList`, `DirectoryForm`, `DirectoryUserList`, `DirectoryUserCard`). Mevcut section'lar bozulmaz.

## 6. Güvenlik ve Hata Yönetimi

**Sırlar ve şifreler:**
- Dizin bind şifresi DB'de şifrelenmiş saklanır (düz metin asla).
- Şifreleme anahtarı ve JWT signing key → environment variable / secret (appsettings'te değil; Docker deployment ile uyumlu).
- AD kullanıcı şifreleri asla saklanmaz, yalnızca bind için anlık kullanılır.
- Internal kullanıcı şifreleri BCrypt ile hash'lenir.

**Loglama:**
- Şifre, token, bind credential asla loglanmaz.
- Senkronizasyon sonuçları (eklenen/güncellenen kullanıcı sayısı) hassas veri olmadan loglanır.

**Hata yönetimi:**
- Mevcut Global Exception Middleware + RFC 7807 Problem Details.
- LDAP bağlantı hataları → kullanıcıya anlamlı mesaj, iç detay gizli.
- Login hataları → "Kullanıcı adı veya şifre hatalı" (hangi kısmın yanlış olduğu belli edilmez).
- Bağlantı testi → admin'e yardımcı olacak net hata mesajları.

**Doğrulama (çok katmanlı):**
- FluentValidation → form input (hostname formatı, port aralığı, zorunlu alanlar).
- Domain → iş kuralları (username tekilliği, geçerli permission kombinasyonları).

## 7. Kapsam Dışı (YAGNI)

- Grup/rol senkronizasyonu (yalnızca kullanıcı senkronizasyonu bu kapsamda).
- Attribute görünürlük ayarları (User profile / Hover profile / group restrictions) — tüm attribute'lar her yerde görünür.
- AD'ye yazma işlemleri (ReadWrite permission altyapısı hazırlanır ancak aktif yazma senaryoları bu kapsamda uygulanmaz).
