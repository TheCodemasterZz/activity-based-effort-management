# EmployeeId → UserId Geçişi (Faz 3) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Work log / proje / izin / onay sistemini `Employee` yerine doğrudan `User` üzerinden çalıştırmak; `EmployeeWorkLog`→`WorkLog`, `EmployeeLeave`→`Leave`, `ProjectEmployeeAssignment`→`ProjectUserAssignment` tam rename; takvimsiz kullanıcı efor giremez kuralı; frontend tam rename + kişi seçicilerin Users API'ye geçişi.

**Architecture:** Spec'ler: `docs/superpowers/specs/2026-07-24-employee-to-user-backend-design.md` ve `...-frontend-design.md`. Katmanlar sıkı bağımlı olduğundan backend rename tek task'ta (Faz 1 emsali). Migration elle yazılır (Rename + FK repoint, veri taşıma yok — tablolar boş).

**Tech Stack:** .NET 8, EF Core 8 + Npgsql, MediatR, React + TS + TanStack Query.

## Global Constraints

- `Employee` entity/modülü (Domain/Employees, Application/Employees, EmployeesController, EmployeeConfiguration, EmployeeDto, EmployeeTests, frontend `employees.ts`/`useEmployees`/`EmployeesPage`/AdminPage Çalışanlar bölümü) **bu fazda dokunulmaz** — Faz 4.
- Görünen Türkçe metinler ("Çalışan" vb.) değişmez.
- Tüm yeni/taşınan FK'lar `Users` tablosuna, `DeleteBehavior.Restrict`.
- Migration'da hiçbir `DropTable`/`DropColumn`+`AddColumn` çifti olamaz — sadece Rename + FK/index drop/add.
- Commit mesaj sonu: `Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>`.

---

### Task 1: Backend tam rename (Domain+Persistence+Application+API+Tests, tek derleme birimi)

**Files:** yeniden adlandırılanlar aşağıdaki git mv listesi; içerik rename'i sed ile tüm `backend/src` + `backend/tests`.

- [ ] **Step 1: Dosya/dizin rename (git mv)**

```bash
cd backend
git mv src/EforTakip.Domain/WorkLogs/EmployeeWorkLog.cs src/EforTakip.Domain/WorkLogs/WorkLog.cs
git mv src/EforTakip.Domain/EmployeeLeaves src/EforTakip.Domain/Leaves
git mv src/EforTakip.Domain/Leaves/EmployeeLeave.cs src/EforTakip.Domain/Leaves/Leave.cs
git mv src/EforTakip.Domain/Projects/ProjectEmployeeAssignment.cs src/EforTakip.Domain/Projects/ProjectUserAssignment.cs
git mv src/EforTakip.Persistence/Configurations/EmployeeWorkLogConfiguration.cs src/EforTakip.Persistence/Configurations/WorkLogConfiguration.cs
git mv src/EforTakip.Persistence/Configurations/EmployeeLeaveConfiguration.cs src/EforTakip.Persistence/Configurations/LeaveConfiguration.cs
git mv src/EforTakip.Persistence/Configurations/ProjectEmployeeAssignmentConfiguration.cs src/EforTakip.Persistence/Configurations/ProjectUserAssignmentConfiguration.cs
git mv src/EforTakip.Application/EmployeeLeaves src/EforTakip.Application/Leaves
git mv src/EforTakip.Application/Leaves/Commands/CreateEmployeeLeave src/EforTakip.Application/Leaves/Commands/CreateLeave
git mv src/EforTakip.Application/Leaves/Commands/DeleteEmployeeLeave src/EforTakip.Application/Leaves/Commands/DeleteLeave
git mv src/EforTakip.Application/Leaves/Queries/GetEmployeeLeaves src/EforTakip.Application/Leaves/Queries/GetLeaves
git mv src/EforTakip.Application/WorkLogs/Dtos/EmployeeWorkLogDto.cs src/EforTakip.Application/WorkLogs/Dtos/WorkLogDto.cs
git mv src/EforTakip.Application/WorkLogs/Queries/GetEmployeeWorkLogs src/EforTakip.Application/WorkLogs/Queries/GetWorkLogs
git mv src/EforTakip.Application/Projects/Commands/AssignEmployeeToProject src/EforTakip.Application/Projects/Commands/AssignUserToProject
git mv src/EforTakip.Application/Projects/Dtos/EmployeeSummaryDto.cs src/EforTakip.Application/Projects/Dtos/UserSummaryDto.cs
git mv src/EforTakip.Api/Controllers/v1/EmployeeWorkLogsController.cs src/EforTakip.Api/Controllers/v1/WorkLogsController.cs
git mv src/EforTakip.Api/Controllers/v1/EmployeeLeavesController.cs src/EforTakip.Api/Controllers/v1/LeavesController.cs
git mv src/EforTakip.Api/Contracts/EmployeeLeaves src/EforTakip.Api/Contracts/Leaves
git mv src/EforTakip.Api/Contracts/Leaves/CreateEmployeeLeaveRequestBody.cs src/EforTakip.Api/Contracts/Leaves/CreateLeaveRequestBody.cs
git mv src/EforTakip.Api/Contracts/Projects/AssignEmployeeRequestBody.cs src/EforTakip.Api/Contracts/Projects/AssignUserRequestBody.cs
git mv tests/EforTakip.Domain.Tests/WorkLogs/EmployeeWorkLogTests.cs tests/EforTakip.Domain.Tests/WorkLogs/WorkLogTests.cs
```

İçindeki dosyaları rename ettikten sonra `Leaves/Commands/*/CreateEmployeeLeaveCommand*.cs` gibi dosya adlarını da aynı kalıpla `git mv` ile düzelt (CreateLeaveCommand.cs, CreateLeaveCommandHandler.cs, CreateLeaveCommandValidator.cs, DeleteLeaveCommand.cs, DeleteLeaveCommandHandler.cs, EmployeeLeaveDto.cs→LeaveDto.cs, GetEmployeeLeavesQuery*.cs→GetLeavesQuery*.cs, GetEmployeeWorkLogsQuery*.cs→GetWorkLogsQuery*.cs, AssignEmployeeToProjectCommand*.cs→AssignUserToProjectCommand*.cs).

- [ ] **Step 2: İçerik rename (sed, uzun token'dan kısaya)**

Migration dosyaları (`src/EforTakip.Persistence/Migrations/`) HARİÇ — mevcut migration'lar tarihsel kayıttır, dokunulmaz. Sed kapsamı: `src` + `tests`, `--exclude-dir=Migrations`.

```bash
cd backend
S="grep -rl --include=*.cs --exclude-dir=Migrations --exclude-dir=obj --exclude-dir=bin"
for pair in \
  "ProjectManagerEmployeeId/ProjectManagerUserId" \
  "AssignedEmployeeId/AssignedUserId" "assignedEmployeeId/assignedUserId" \
  "OwnerEmployeeId/OwnerUserId" "ownerEmployeeId/ownerUserId" \
  "ProjectEmployeeAssignment/ProjectUserAssignment" \
  "GetEmployeeWorkLogs/GetWorkLogs" "EmployeeWorkLogDto/WorkLogDto" \
  "EmployeeWorkLogs/WorkLogs" "EmployeeWorkLog/WorkLog" \
  "GetEmployeeLeaves/GetLeaves" "CreateEmployeeLeave/CreateLeave" "DeleteEmployeeLeave/DeleteLeave" \
  "EmployeeLeaveDto/LeaveDto" "EmployeeLeaves/Leaves" "EmployeeLeave/Leave" \
  "AssignEmployeeToProject/AssignUserToProject" \
  "EmployeeSummaryDto/UserSummaryDto" \
  "employeeworklogs/worklogs" "employeeleaves/leaves" \
  "EmployeeIds/UserIds" "EmployeeId/UserId" "employeeId/userId" "employeeIds/userIds" \
  ; do
  from="${pair%%/*}"; to="${pair##*/}"
  eval "$S \"$from\" src tests" | xargs -r sed -i "s/$from/$to/g"
done
```

- [ ] **Step 3: Sed sonrası el işçiliği (mekanik olmayanlar)**

1. `Project.cs`: `AssignEmployee`→`AssignUser`, `_employeeAssignments`→`_userAssignments`, XML yorumlarını User diline çevir. `ProjectsController` + `AssignUserRequestBody` + `ProjectRepository` aynı metod adına uyar.
2. `AssignUserToProjectCommandHandler`: `IRepository<Employee>` → `IRepository<User>` (`using EforTakip.Domain.Users;`); NotFound mesajı `nameof(User)`.
3. `IApplicationDbContext` + `ApplicationDbContext` + `TestDbContext`: `DbSet<WorkLog> WorkLogs`, `DbSet<Leave> Leaves`, `DbSet<ProjectUserAssignment> ProjectUserAssignments` (sed büyük kısmını yapmış olacak; using'leri ve `EforTakip.Domain.Leaves` namespace'ini doğrula).
4. Tablo eşlemeleri: `WorkLogConfiguration.ToTable("WorkLogs")`, `LeaveConfiguration.ToTable("Leaves")`, `ProjectUserAssignmentConfiguration.ToTable("ProjectUsers")`; üçünde de FK artık `Users` tablosuna (`HasOne`/`HasForeignKey` hedefi User olacak şekilde principal tipini değiştir; mevcut Restrict davranışı korunur). `ProjectConfiguration`, `ProjectTaskConfiguration`, `ProjectRiskConfiguration`, `ProjectIssueConfiguration` içindeki Employee principal'ları da User'a çevir.
5. `WorkLogApprovalConfiguration`: eksik FK eklenir:

```csharp
builder.HasOne<Domain.Users.User>()
    .WithMany()
    .HasForeignKey(a => a.UserId)
    .OnDelete(DeleteBehavior.Restrict);
```

6. `TestDataSeeder.cs`: sahte `Employee.Create(...)` yerine sahte internal `User` üretimi (mevcut internal directory'nin Id'si seed'de zaten biliniyor; bilinmiyorsa `context.Directories.First(d => d.Source == DirectorySource.Internal)`), her birine `user.AssignWorkCalendar(employeeCalendarIds[i % 2])`:

```csharp
var users = Enumerable.Range(0, 25)
    .Select(i =>
    {
        var u = User.CreateInternal(
            internalDirectoryId,
            username: $"testuser{i:D2}",
            firstName: bogus.Name.FirstName(),
            lastName: bogus.Name.LastName(),
            displayName: bogus.Name.FullName(),
            email: bogus.Internet.Email(),
            passwordHash: "seed-only-not-a-real-hash");
        u.AssignWorkCalendar(calendarIds[i % 2]);
        return u;
    })
    .ToList();
context.Users.AddRange(users);
```

Seeder'ın geri kalanı `users` listesinin Id'leriyle aynı akışı sürdürür (proje ataması, work log, izin, onay).

- [ ] **Step 4: Build + kalıntı denetimi**

```bash
cd backend && dotnet build
grep -rn --include=*.cs --exclude-dir=Migrations --exclude-dir=obj "Employee" src tests
```

Beklenen kalıntı SADECE: Domain/Employees, Application/Employees, EmployeesController, EmployeeConfiguration, EmployeeTests, DbSet<Employee> Employees satırları, Permissions'taki Employee modülü. Başka `Employee` kalırsa düzelt.

- [ ] **Step 5: Mevcut test suite'i çalıştır, rename'e uyan asserts'leri düzelt**

Run: `dotnet test` → 189 test PASS (yeni kural testi Task 2'de).

- [ ] **Step 6: Commit** — `refactor: work log/proje/izin/onay modüllerinde Employee->User tam rename`

### Task 2: "Takvimsiz kullanıcı efor/plan giremez" kuralı (TDD)

**Files:**
- Modify: `backend/src/EforTakip.Application/WorkLogs/WorkLogValidationHelper.cs`
- Modify: `backend/src/EforTakip.Application/WorkLogs/Commands/LogWork/LogWorkCommandHandler.cs`, `.../UpdateWorkLog/UpdateWorkLogCommandHandler.cs` (helper'a `IRepository<User>` geçirilir)
- Test: `backend/tests/EforTakip.Application.Tests/WorkLogs/Commands/LogWorkCommandHandlerTests.cs`

**Interfaces:** `WorkLogValidationHelper.ValidateAsync` imzasına `IRepository<User> userRepository` parametresi eklenir; kullanıcı yoksa `NotFoundException(nameof(User))`, `WorkCalendarId is null` ise `BusinessRuleValidationException`.

- [ ] **Step 1: Failing test** — TestDbContext'e takvimsiz bir User ekle, projeye ata, `LogWorkCommand` gönder; `BusinessRuleValidationException` ve mesajda "Mesai takvimi atanmamış" beklenir. Mevcut geçen senaryolardaki test kullanıcılarına `AssignWorkCalendar` verilerek onların geçmeye devam etmesi sağlanır.
- [ ] **Step 2: Run** `dotnet test --filter LogWorkCommandHandlerTests` → yeni test FAIL.
- [ ] **Step 3: Implement** — helper'ın başına (proje kontrolünden önce):

```csharp
var user = await userRepository.GetByIdAsync(userId, cancellationToken)
    ?? throw new NotFoundException(nameof(User), userId);
if (user.WorkCalendarId is null)
    throw new BusinessRuleValidationException(
        "Mesai takvimi atanmamış kullanıcılar efor/plan girişi yapamaz. " +
        "Lütfen yöneticinizden Kullanıcılar ekranından bir mesai takvimi atamasını isteyin.");
```

- [ ] **Step 4: Run** tüm suite PASS. **Step 5: Commit** — `feat: takvimsiz kullanıcı efor/plan girisi engellendi`

### Task 3: Migration (elle yazılmış, veri güvenli)

**Files:** Create: `backend/src/EforTakip.Persistence/Migrations/*_RenameEmployeeRefsToUser.cs` (+Designer, snapshot güncellemesi `dotnet ef` üretir)

- [ ] **Step 1:** `dotnet ef migrations add RenameEmployeeRefsToUser --project src/EforTakip.Persistence --startup-project src/EforTakip.Api`
- [ ] **Step 2: Up() gövdesini elle değiştir.** Otomatik üretim Drop+Create içerirse TAMAMEN değiştirilir. Beklenen op listesi:
  - `RenameTable`: EmployeeWorkLogs→WorkLogs, EmployeeLeaves→Leaves, ProjectEmployees→ProjectUsers.
  - `RenameColumn`: WorkLogs.EmployeeId→UserId; Leaves.EmployeeId→UserId; ProjectUsers.EmployeeId→UserId; Projects.ProjectManagerEmployeeId→ProjectManagerUserId; ProjectTasks.AssignedEmployeeId→AssignedUserId; ProjectRisks.OwnerEmployeeId→OwnerUserId; ProjectIssues.OwnerEmployeeId→OwnerUserId; WorkLogApprovals.EmployeeId→UserId.
  - `RenameIndex`: rename'lerden etkilenen tüm indexler (Designer diff'inin verdiği adlarla).
  - `DropForeignKey` (Employees'e işaret eden 6 FK) + `AddForeignKey` (aynı kolonlar → `Users`, `onDelete: ReferentialAction.Restrict`).
  - `AddForeignKey`: WorkLogApprovals.UserId → Users (Restrict) — yeni.
  - Down() bire bir tersi.
- [ ] **Step 3:** `dotnet build` + snapshot (`ApplicationDbContextModelSnapshot`) ile migration'ın tutarlı olduğunu `dotnet ef migrations list` üzerinden doğrula. Migration henüz gerçek DB'ye uygulanmaz (Task 6'da kullanıcı komutuyla).
- [ ] **Step 4: Commit** — `feat: EmployeeId->UserId rename migration (FK'lar Users'a repoint, veri kaybi yok)`

### Task 4: Frontend mekanik rename

**Files:** `frontend/src` genelinde sed + `git mv`:
`api/employeeLeaves.ts`→`api/leaves.ts`, `hooks/useEmployeeLeaves.ts`→`useLeaves.ts`, `hooks/useCreateEmployeeLeaveMutation.ts`→`useCreateLeaveMutation.ts`, `hooks/useDeleteEmployeeLeaveMutation.ts`→`useDeleteLeaveMutation.ts`, `components/employees/EmployeeLeaveScheduleModal.tsx`→`components/leaves/LeaveScheduleModal.tsx`.

- [ ] **Step 1: git mv + sed** (aynı sıra mantığı; `--include=*.ts --include=*.tsx`, `api/employees.ts`, `hooks/useEmployees.ts`, `pages/EmployeesPage.tsx`, `pages/AdminPage.tsx` Çalışanlar bölümü hariç). Token listesi: `projectManagerEmployeeId/projectManagerUserId`, `assignedEmployeeId/assignedUserId`, `ownerEmployeeId/ownerUserId`, `EmployeeWorkLogDto/WorkLogDto`, `GetEmployeeLeavesParams/GetLeavesParams`, `createEmployeeLeave/createLeave`, `deleteEmployeeLeave/deleteLeave`, `EmployeeLeaveDto/LeaveDto`, `useEmployeeLeaves/useLeaves`, `useCreateEmployeeLeaveMutation/useCreateLeaveMutation`, `useDeleteEmployeeLeaveMutation/useDeleteLeaveMutation`, `EmployeeLeaveScheduleModal/LeaveScheduleModal`, `EmployeeSummaryDto/UserSummaryDto`, `'employeeLeaves'/'leaves'` (query key), `/api/v1/employeeworklogs` → `/api/v1/worklogs`, `/api/v1/employeeleaves` → `/api/v1/leaves`, `employeeId/userId`, `employeeIds/userIds`. `ProjectDetailDto.employees`→`users` elle.
- [ ] **Step 2:** `npm run build` hatasız; `grep -rn "employee" src/` kalıntısı yalnızca Faz 4 kapsamı + Türkçe metinler.
- [ ] **Step 3: Commit** — `refactor(frontend): employeeId->userId tam rename, yeni /worklogs ve /leaves route'lari`

### Task 5: Kişi seçicileri Users API'ye geçir + kapasite uyarısı

**Files:**
- Create: `frontend/src/lib/userDisplayName.ts`
- Modify: kişi seçicisi kullanan bileşenler (`WorkLogForm.tsx`, `PlanWorkPage.tsx`, `WorkLogApprovalModal.tsx`, `LeaveScheduleModal.tsx`, `TasksTab.tsx`, `ProjectFormModal.tsx`, `ProjectTaskFormModal.tsx`, `RiskFormModal.tsx`, `IssueFormModal.tsx`, `CapacityManagementPage.tsx`, `ReportPage.tsx`, `PlanningAccuracyPage.tsx`, `WidgetsPage.tsx`, `WidgetLogWorkPage.tsx`, `HomePage.tsx` — grep ile `useEmployees(` çağıran tam liste çıkarılır)
- Modify: `frontend/src/hooks/useUsers.ts` (`onlyActive` parametresi + `useActiveUsers()` yardımcı hook'u)

- [ ] **Step 1:** `lib/userDisplayName.ts`:

```ts
import type { UserDto } from '../api/types';
export function userDisplayName(user: Pick<UserDto, 'displayName' | 'username'>): string {
  return user.displayName?.trim() || user.username;
}
```

- [ ] **Step 2:** `useUsers.ts`'e ekle (getUsers zaten `onlyActive` destekliyor):

```ts
/** Kişi seçicileri için — tüm aktif kullanıcılar, tek sayfada. */
export function useActiveUsers() {
  return useQuery({
    queryKey: ['users', 'active-all'],
    queryFn: () => getUsers({ onlyActive: true, pageSize: 1000 }),
  });
}
```

- [ ] **Step 3:** `useEmployees()` çağıran her work-tracking bileşeninde `useActiveUsers()` + `userDisplayName` kullan (`employee.name` → `userDisplayName(user)`); `EmployeesPage`/AdminPage hariç.
- [ ] **Step 4:** `CapacityManagementPage.tsx`: takvimsiz (`workCalendarId === null`) kullanıcıları hesap dışı bırak, sayfa üstünde uyarı:

```tsx
{usersWithoutCalendar.length > 0 && (
  <div className="rounded-md border border-amber-300 bg-amber-50 px-3 py-2 text-sm text-amber-800">
    {usersWithoutCalendar.length} kullanıcının mesai takvimi atanmamış — kapasite hesabına dahil edilmedi.
  </div>
)}
```

- [ ] **Step 5:** `npm run build` hatasız. **Step 6: Commit** — `feat(frontend): kisi secicileri Users API'ye gecti, kapasite sayfasina takvimsiz uyarisi`

### Task 6: Uçtan uca doğrulama

- [ ] **Step 1:** `dotnet build && dotnet test` (tümü) + `npm run build` yeşil.
- [ ] **Step 2:** Kullanıcıya migration komutu verilir (kendisi çalıştırır — user-secrets):
  `dotnet ef database update --project src/EforTakip.Persistence --startup-project src/EforTakip.Api -- --environment RealDb`
  Öncesi/sonrası satır sayıları psql ile karşılaştırılır (Users=mevcut sayı, diğerleri 0 kalmalı).
- [ ] **Step 3:** Backend RealDb + frontend dev server; tarayıcıdan: login, work log listesi (`/api/v1/worklogs` 200), takvimsiz kullanıcıyla efor girişi → anlamlı 422 mesajı, kapasite sayfası uyarısı.
- [ ] **Step 4:** finishing-a-development-branch: test doğrulama → merge seçenekleri.
