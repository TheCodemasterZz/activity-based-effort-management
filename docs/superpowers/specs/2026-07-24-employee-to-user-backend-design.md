# EmployeeId → UserId Geçişi — Backend (Faz 3) — Tasarım

## Bağlam

[2026-07-24-directoryuser-to-user-rename-design.md](2026-07-24-directoryuser-to-user-rename-design.md)
ve [2026-07-24-user-workcalendar-design.md](2026-07-24-user-workcalendar-design.md)'de tanımlanan
Employee/User birleştirme yol haritasının üçüncü fazı. Faz 1'de `DirectoryUser`→`User` rename'i,
Faz 2'de `User.WorkCalendarId` eklendi. Bu faz, work log/proje/izin/onay sisteminin `Employee`
yerine doğrudan `User` kullanmasını sağlar — `Employee` entity'sinin kendisi bu fazda **silinmiyor**
(Faz 4'e bırakıldı), sadece artık hiçbir yerden referans alınmıyor olacak.

## Kapsam ve envanter

Aşağıdaki 7 entity/modülde `EmployeeId` → `UserId` geçişi yapılacak (Domain, Persistence,
Application, API — hepsi aynı anda, çünkü katmanlar birbirine sıkı bağımlı ve ayrı ayrı derlenemez;
bkz. Faz 1'in aynı sebeple Task 1-2-3'ü birleştirmesi):

| Entity | Alan | Yeni alan |
|---|---|---|
| `Project` | `ProjectManagerEmployeeId` | `ProjectManagerUserId` |
| `ProjectEmployeeAssignment` (tablo `ProjectEmployees`) | `EmployeeId` | → **tamamen yeniden adlandırılır**: `ProjectUserAssignment` (tablo `ProjectUsers`), alan `UserId` |
| `ProjectTask` | `AssignedEmployeeId` | `AssignedUserId` |
| `ProjectRisk` | `OwnerEmployeeId` | `OwnerUserId` |
| `ProjectIssue` | `OwnerEmployeeId` | `OwnerUserId` |
| `EmployeeWorkLog` (tablo `EmployeeWorkLogs`) | `EmployeeId` | → **tamamen yeniden adlandırılır**: `WorkLog` (tablo `WorkLogs`), alan `UserId` |
| `EmployeeLeave` (tablo `EmployeeLeaves`, namespace `EmployeeLeaves`) | `EmployeeId` | → **tamamen yeniden adlandırılır**: `Leave` (tablo `Leaves`, namespace `Leaves`), alan `UserId` |
| `WorkLogApproval` | `EmployeeId` | `UserId` (+ eksik olan FK bu fazda eklenir, `Restrict`) |

Tüm FK'lar `Employee`'ye değil artık `User`'a, hepsi `DeleteBehavior.Restrict` (mevcut
`Employee` FK'larıyla birebir aynı davranış — sadece hedef tablo değişiyor).

## Veri kaybı riski yok

Gerçek PostgreSQL'de şu an **sıfır** `Employees`/`EmployeeWorkLogs`/`EmployeeLeaves`/... satırı var
(sadece `admin` adında 1 `Users` kaydı, hiç proje/work log/izin yok — daha önce doğrulandı). Bu
yüzden migration, `Employee*` tablolarındaki veriyi `User*`'a taşımaya çalışmaz; FK'lar sadece
`Employees` yerine `Users` tablosuna işaret edecek şekilde `DropForeignKey`+`AddForeignKey` ile
yeniden bağlanır. `Employee` tablosunun kendisi bu fazda silinmiyor (Faz 4), sadece artık ona
işaret eden FK kalmıyor.

## Kapasite hesaplaması riski (önemli, kayıt altına alınmalı)

`CapacityManagementPage.tsx`, bir kişinin günlük beklenen çalışma saatini `Employee.WorkCalendarId`
üzerinden hesaplıyor. Bu geçişten sonra aynı hesaplama `User.WorkCalendarId`'yi kullanacak — ki bu
alan Faz 2 gereği **varsayılan olarak boş**. Bu, veri kaybı değil, zaten bilinen ve Faz 2'de bilinçli
olarak tasarlanmış bir durum: adminin Kullanıcılar ekranından (Faz 2'de eklendi) her kullanıcıya
takvim ataması gerekiyor. Bu fazda bunun için ek bir işlem yapılmıyor; sadece not düşülüyor.

## Yeni iş kuralı: takvimsiz kullanıcı efor/plan giremez

Faz 2'de bilinçli olarak ertelenen kural burada eklenir. `WorkLogValidationHelper.ValidateAsync`
(LogWork ve UpdateWorkLog'un ortak doğrulaması), proje ataması kontrolünden hemen sonra:

```csharp
var user = await userRepository.GetByIdAsync(userId, cancellationToken)
    ?? throw new NotFoundException(nameof(User), userId);
if (user.WorkCalendarId is null)
    throw new BusinessRuleValidationException(
        "Mesai takvimi atanmamış kullanıcılar efor/plan girişi yapamaz. Lütfen yöneticinizden Kullanıcılar ekranından bir mesai takvimi atamasını isteyin.");
```

Bu kontrol PlanWork'ü de kapsar çünkü `LogWorkCommand`'ın `EntryType` alanı zaten Actual/Planned
ayrımını taşıyor — aynı handler/validator ikisine de hizmet ediyor.

## Application & API katmanı

Aşağıdaki dosyalarda `EmployeeId`/`employeeId`/`Employee` (iş takibi bağlamında) → `UserId`/
`userId`/`User` olacak şekilde mekanik rename (Faz 1'de kanıtlanmış `sed` stratejisiyle):

- **WorkLogs**: `EmployeeWorkLogDto`→`WorkLogDto`, `GetEmployeeWorkLogs*`→`GetWorkLogs*`,
  `WorkLogValidationHelper`, `LogWork*`, `UpdateWorkLog*`.
- **WorkLogApprovals**: `WorkLogApprovalGuard`, `GetWorkLogApprovals*`, `CreateWorkLogApproval*`,
  `WorkLogApprovalDto`.
- **Projects**: `GetProjects*`, `GetProjectById*`, `ProjectDetailDto`, `ProjectDto`,
  `ProjectIssueDto`, `ProjectRiskDto`, `ProjectTaskDto`, `UpdateProjectTask*`, `UpdateProjectRisk*`,
  `UpdateProjectIssue*`, `UpdateProject*`, `CreateProjectTask*`, `CreateProjectRisk*`,
  `CreateProjectIssue*`, `CreateProject*`, `AssignEmployeeToProject*`→`AssignUserToProject*`
  (bu handler'ın `IRepository<Employee>` bağımlılığı `IRepository<User>`'a değişir — projeye artık
  Employee değil User atanır, bu geçişin asıl amacı).
- **Leaves** (eski `EmployeeLeaves`): `GetEmployeeLeaves*`→`GetLeaves*`, `EmployeeLeaveDto`→
  `LeaveDto`, `CreateEmployeeLeave*`→`CreateLeave*`.

API: `EmployeeWorkLogsController`→`WorkLogsController`, `EmployeeLeavesController`→
`LeavesController`, ilgili `Contracts/*RequestBody.cs` dosyaları. `WorkLogApprovalsController`,
`ProjectsController`, `ProjectTasksController`, `ProjectRisksController`, `ProjectIssuesController`
yerinde kalır, sadece alan adları değişir.

## Testler ve seed data

- Mevcut testler (`ProjectTests`, `EmployeeWorkLogTests`→`WorkLogTests`,
  `CreateProjectCommandHandlerTests`, `CreateProjectCommandValidatorTests`,
  `LogWorkCommandHandlerTests`, `TestDbContext`) rename'e uygun güncellenir.
- `ProjectTask`, `ProjectRisk`, `ProjectIssue`, `EmployeeLeave`/`Leave`, `WorkLogApproval`,
  `AssignEmployeeToProject`/`AssignUserToProject` için **hiç mevcut test yok** — bu fazda yeni
  testler yazılmıyor (kapsam dışı, mevcut açığı büyütmüyoruz ama kapatmıyoruz da); sadece derlenen
  ve çalışan kodun regresyona uğramadığından build+mevcut test suite ile emin oluyoruz.
- `TestDataSeeder.cs` (in-memory Test Mode) güncellenir — hâlâ sahte `Employee` kayıtları
  üretiyor olabilir, artık sahte `User` kayıtları (WorkCalendarId atanmış olarak, aksi halde yeni
  iş kuralı yüzünden hiç work log seed edilemez) üretecek şekilde değişir.

## Kapsam dışı (bu fazda yapılmayacaklar)

- `Employee` entity'sinin, tablosunun, `EmployeesController`'ın silinmesi (Faz 4).
- Frontend rename'i (ayrı doküman: `2026-07-24-employee-to-user-frontend-design.md`).
- `ProjectTask`/`ProjectRisk`/`ProjectIssue`/`Leave`/`WorkLogApproval` için yeni test yazımı.

## Doğrulama

1. `dotnet build` + `dotnet test` (tüm suite) yeşil.
2. Migration gerçek Postgres'e uygulanır; mevcut `admin` kullanıcısı ve boş tablo durumu korunur
   (satır sayıları önce/sonra karşılaştırılır).
3. Backend `RealDb` profiliyle ayağa kaldırılıp Swagger/curl ile yeni route'lar (`/api/v1/worklogs`,
   `/api/v1/leaves`) ve yeni alan adları (`userId`) doğrulanır.
