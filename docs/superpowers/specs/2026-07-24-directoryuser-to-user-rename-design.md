# DirectoryUser → User Yeniden Adlandırma ve Modül Ayrımı (Faz 1)

## Bağlam ve amaç

Uygulamada bugün iki ayrı "kişi" kavramı var:

- **DirectoryUser**: login/kimlik doğrulama için kullanılan, Active Directory'den senkronize
  edilebilen ya da internal oluşturulabilen kullanıcı kaydı.
- **Employee**: work log, proje ataması, izin, onay gibi iş verisinin bağlandığı, tamamen ayrı
  bir domain nesnesi (`Name`, `Email`, `WorkCalendarId`).

Bu iki kavram arasında bugün hiçbir bağlantı (foreign key) yok. Hedef: bu ayrımı ortadan kaldırıp
tek bir kavrama (**User**) indirgemek — AD'den mi internal mi geldiği bir "tür" farkı değil, sadece
`Source` alanıyla ifade edilen bir detay.

Bu iş tek oturumda güvenle bitirilemeyecek kadar büyük (backend'de 125+ dosya + frontend), bu
yüzden fazlara bölündü:

- **Faz 1 (bu doküman)**: `DirectoryUser` → `User` yeniden adlandırması + ayrı bir `Users`
  modülüne taşınması. Employee'ye dokunulmuyor.
- Faz 2: User'a `WorkCalendarId` eklenmesi.
- Faz 3: `EmployeeId` kullanan tüm modüllerin (Project, ProjectTask, ProjectRisk, ProjectIssue,
  WorkLog, Leave, WorkLogApproval) `UserId`'ye geçmesi + ilgili tablo/sınıf isimlerinin
  sadeleşmesi (ör. `EmployeeWorkLog` → `WorkLog`).
- Faz 4: `Employee` entity'sinin, tablosunun ve `EmployeesController`'ın tamamen kaldırılması.
- Faz 5: Frontend'in Employee'ye özgü kalan kısımlarının (varsa) User'a taşınması.

Dummy veri üretimi bu doküman kapsamı dışında; kullanıcı bunu doğrudan veritabanı üzerinden
kendisi çözecek.

## Veri güvenliği ilkesi (tüm fazlar için geçerli)

Kullanıcı verisinin (ve ona bağlı iş verisinin) sessizce kaybolmaması kritik bir gereksinim:

- Directory senkronizasyonu bugün de kullanıcıları **asla silmiyor**, sadece `Deactivate()` ile
  pasife alıyor (`SyncDirectoryCommandHandler`). Bu davranış korunacak.
- `Users.DirectoryId → Directories.Id` foreign key'i `ReferentialAction.Restrict` — bir Directory,
  altında User varken silinemiyor. Bu davranış korunacak.
- Sistemde bugün bir "kullanıcı sil" endpoint'i yok. Faz 1 kapsamında da eklenmiyor.
- Faz 3'te work log/proje/izin/onay tablolarının `UserId` foreign key'leri de **Restrict** olacak
  — bir kullanıcıya bağlı iş verisi varsa o kullanıcı asla cascade ile silinemeyecek.

## Kapsam: modül ayrımı

### Domain katmanı

`Domain/Directories/` klasöründen yeni `Domain/Users/` modülüne taşınacaklar (sınıf adları da
değişiyor):

| Eski | Yeni |
|---|---|
| `Directories/DirectoryUser.cs` | `Users/User.cs` |
| `Directories/DirectoryUserAttribute.cs` | `Users/UserAttribute.cs` |
| `Directories/DirectoryUserRole.cs` | `Users/UserRole.cs` |

`Domain/Directories/`'de **kalacaklar** (AD bağlantı/senkron ayarına ait, User'a özel değil):
`Directory.cs`, `DirectoryAttributeMapping.cs`, `DirectoryPermission.cs`, `DirectorySource.cs`
(hem `Directory.Source` hem `User.Source` tarafından paylaşılan enum), `SyncScheduleKind.cs`.

### Application katmanı

`Application/Directories/`'den yeni `Application/Users/` modülüne taşınacaklar:

- `Commands/CreateInternalUser/*`
- `Commands/ResetInternalUserPassword/*`
- `Queries/GetDirectoryUsers/*` → `Queries/GetUsers/*`
- `Queries/GetDirectoryUserById/*` → `Queries/GetUserById/*`
- `Queries/GetOrgChart/*`
- `Dtos/DirectoryUserDto.cs` → `Dtos/UserDto.cs`
- `Dtos/DirectoryUserDetailDto.cs` → `Dtos/UserDetailDto.cs`
- `Dtos/OrgChartResultDto.cs`

`Application/Directories/`'de **kalacaklar**: `CreateDirectory`, `UpdateDirectory`,
`DeleteDirectory`, `SyncDirectory` (içeride `User` tipini kullanacak ama kendisi bir dizin
operasyonu olarak kalıyor), `TestDirectoryConnection`, `CreateAttributeMapping`,
`UpdateAttributeMapping`, `DeleteAttributeMapping`, `GetAttributeMappings`, `GetDirectories`,
`GetDirectoryById`, `Ldap/*`.

`Roles` modülü (`Role.cs`, `AssignRoleToUserCommandHandler.cs`,
`RemoveRoleFromUserCommandHandler.cs`, `GetRoleByIdQueryHandler.cs`) zaten "User" merkezli
adlandırılmış — taşınmıyor, sadece içindeki `DirectoryUser` tip referansları `User` olarak
güncelleniyor.

### Persistence katmanı

- `Configurations/DirectoryUserConfiguration.cs` → `UserConfiguration.cs`
- `Configurations/DirectoryUserAttributeConfiguration.cs` → `UserAttributeConfiguration.cs`
- `Configurations/DirectoryUserRoleConfiguration.cs` → `UserRoleConfiguration.cs`
- `EforTakipDbContext` DbSet'leri: `DirectoryUsers` → `Users`, `DirectoryUserAttributes` →
  `UserAttributes`, `DirectoryUserRoles` → `UserRoles`.
- Yeni bir migration: tabloları **veri kaybı olmadan** `RenameTable`/`RenameColumn` ile yeniden
  adlandırır (`DirectoryUsers`→`Users`, `DirectoryUserAttributes`→`UserAttributes`,
  `DirectoryUserRoles`→`UserRoles`, ilgili FK/index isimleri). Mevcut AD senkron verisi korunur,
  yeniden sync gerekmez.

### API katmanı

- `Controllers/v1/DirectoryUsersController.cs` → `UsersController.cs`. `[Route("api/v{version}/[controller]")]`
  kuralı sayesinde route otomatik `/api/v1/directory-users` → `/api/v1/users` olur.
- `Permissions.User.*` zaten "User" olarak adlandırılmış (`Permissions.cs`), değişiklik yok.

### Frontend

API route değiştiği için bu kısım Faz 5'i beklemeden **şimdi** güncellenmek zorunda (yoksa admin
ekranları kırılır):

- `api/directoryUsers.ts` → `api/users.ts`
- `hooks/useDirectoryUsers.ts` → `hooks/useUsers.ts`
- `components/admin/directory/DirectoryUserCard.tsx` → `UserCard.tsx`
- `components/admin/directory/DirectoryUserCardModal.tsx` → `UserCardModal.tsx`
- `api/types.ts` içindeki `DirectoryUserDto`/`DirectoryUserDetailDto` tipleri → `UserDto`/`UserDetailDto`
- `RolesSection.tsx`, `OrgChartSection.tsx`, `useDirectoryMutations.ts` içindeki referanslar
  güncellenir (dosya adları aynı kalabilir, bunlar zaten Directory'ye özel mutasyonlar da
  içeriyor olabilir — implementasyon planında netleştirilecek).

Employee'ye özgü frontend ekranları (varsa) bu fazda **değişmiyor** — Faz 5'e bırakılıyor.

### Testler

- `tests/EforTakip.Domain.Tests/Directories/DirectoryUserTests.cs` → `Users/UserTests.cs`
- `CreateInternalUserCommandHandlerTests.cs`, `ResetInternalUserPasswordCommandHandlerTests.cs`,
  `GetOrgChartQueryHandlerTests.cs` → `Application.Tests/Users/...` altına taşınır.
- `SyncDirectoryCommandHandlerTests.cs`, `SyncDirectoryCommandHandlerRealDbContextTests.cs`,
  `TestDbContext.cs` → `Directories`'de kalır (içeride `User` tipini kullanacak şekilde güncellenir).
- `LoginCommandHandlerTests.cs`, Roles altındaki testler → yerinde kalır, sadece tip referansları
  güncellenir.

## Doğrulama

1. `dotnet build` (tüm çözüm) hatasız geçmeli.
2. `dotnet test` (tüm test projeleri) yeşil olmalı.
3. Bu worktree'deki gerçek PostgreSQL'e yeni migration uygulanır; mevcut AD senkron kullanıcıları
   hâlâ görünür ve login olabilir olduğu doğrulanır.
4. Frontend `npm run build` hatasız geçmeli; tarayıcıda login + admin Users ekranı + Org Chart
   manuel kontrol edilir.

## Kapsam dışı (bu fazda yapılmayacaklar)

- `Employee` entity'sine hiç dokunulmuyor.
- `WorkCalendarId` User'a eklenmiyor (Faz 2).
- Dummy veri üretimi yapılmıyor (kullanıcı DB üzerinden kendisi yapacak).
- Kullanıcı silme (delete) endpoint'i eklenmiyor.
