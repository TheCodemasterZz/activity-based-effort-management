# EmployeeId → UserId Geçişi — Frontend (Faz 3) — Tasarım

## Bağlam

[2026-07-24-employee-to-user-backend-design.md](2026-07-24-employee-to-user-backend-design.md)'in
frontend ayağı. Backend'de route'lar (`/api/v1/employeeworklogs`→`/api/v1/worklogs`,
`/api/v1/employeeleaves`→`/api/v1/leaves`) ve alan adları (`employeeId`→`userId` vb.) değiştiği
için frontend zaten zorunlu olarak güncellenecek; kullanıcının kararı gereği bu, minimal uyum
değil **tam yeniden adlandırma** olarak yapılır (kod tanımlayıcılarında `employee` kalmaz).

**Görünen Türkçe metinler değişmez** — kullanıcının Faz 1'deki kararı: "Arayüzdeki isimler
Çalışanlar olarak kalabilir." Yani ekranlarda "Çalışan" yazmaya devam eder; sadece kod
tanımlayıcıları, tipler, route'lar ve alan adları değişir.

## API katmanı (`frontend/src/api/`)

| Dosya / öğe | Değişiklik |
|---|---|
| `workLogs.ts` | route `/api/v1/employeeworklogs`→`/api/v1/worklogs`; `employeeId` param/payload alanları →`userId` |
| `employeeLeaves.ts` → **`leaves.ts`** | route `/api/v1/employeeleaves`→`/api/v1/leaves`; `EmployeeLeaveDto`→`LeaveDto`, `GetEmployeeLeavesParams`→`GetLeavesParams`, `createEmployeeLeave`→`createLeave`, `deleteEmployeeLeave`→`deleteLeave`; `employeeId`→`userId` |
| `types.ts` | `EmployeeWorkLogDto`→`WorkLogDto` (`employeeId`→`userId`); `ProjectDto`/`ProjectDetailDto.projectManagerEmployeeId`→`projectManagerUserId`; `ProjectTaskDto.assignedEmployeeId`→`assignedUserId`; `ProjectRiskDto`/`ProjectIssueDto.ownerEmployeeId`→`ownerUserId`; `EmployeeSummaryDto`→`UserSummaryDto`, `ProjectDetailDto.employees`→`users` |
| `projects.ts`, `projectTasks.ts`, `projectRisks.ts`, `projectIssues.ts`, `workLogApprovals.ts` | payload/param alanlarında mekanik `employeeId`→`userId` rename |
| `employees.ts` | **kalır** (EmployeesPage hâlâ kullanıyor, Faz 4'te silinecek) |

## Kişi seçiminin kaynağı: Employees API → Users API (kritik)

Work log / plan / izin / proje atama ekranlarındaki kişi seçicileri şu an `useEmployees`
(Employees API) ile doluyor. Backend FK'ları artık `Users` tablosuna işaret ettiği için bir
`Employee.Id` göndermek FK ihlali olur — bu seçiciler **`useUsers` (Users API, `onlyActive: true`)**
kaynağına geçer:

- Seçici için `useUsers`'a `onlyActive` desteği eklenir (API'de var, hook'a parametre olarak
  geçirilir) ve seçicilerde `pageSize` yeterince büyük verilir (kullanıcı sayısı ~200-300;
  `pageSize: 1000`).
- Görünen ad: `UserDto`'da `name` yok; `displayName ?? username` kullanılır (küçük bir
  `userDisplayName(user)` yardımcı fonksiyonu, `lib/` altında).
- `EmployeesPage` ve AdminPage'in Çalışanlar bölümü **bu fazda dokunulmaz** (Faz 4).

## Kapasite sayfası (`CapacityManagementPage.tsx`)

Şu an `EmployeeDto.workCalendarId` (her zaman dolu) üzerinden hesap yapıyor. Users kaynağına
geçince `workCalendarId` **null olabilir** (Faz 2 tasarımı gereği). Davranış:

- Takvimi olmayan kullanıcılar kapasite hesabına **dahil edilmez** ve sayfada
  "N kullanıcının mesai takvimi atanmamış — kapasite hesabına dahil edilmedi" uyarısı gösterilir.
- Yanlış (uydurma varsayılan) hesap üretmek yerine eksikliği görünür kılmak, Faz 2'deki
  "senkronda default atama yok" kararıyla tutarlı.

## Hook'lar ve lib (`hooks/`, `lib/`, `components/`, `pages/`)

Mekanik rename (davranış değişikliği yok):

- `useEmployeeLeaves`→`useLeaves`, `useCreateEmployeeLeaveMutation`→`useCreateLeaveMutation`,
  `useDeleteEmployeeLeaveMutation`→`useDeleteLeaveMutation`; query key `'employeeLeaves'`→`'leaves'`.
- `useWorkLogs`, `useProjects`, `useConfidenceScoreContext`: `employeeId`→`userId`.
- `lib/groupWorkLogs.ts`, `groupWorkLogsAccuracy.ts`, `confidenceScore.ts`, `overtimeCheck.ts`,
  `projectRag.ts`: alan adı rename.
- `components/employees/EmployeeLeaveScheduleModal.tsx` → `components/leaves/LeaveScheduleModal.tsx`.
- Kalan tüm sayfa/bileşenlerde (`WorkLogForm`, `WorkLogTable`, `WorkLogApprovalModal`,
  `CellWorkLogsModal`, `TasksTab`, `PlanWorkPage`, `HomePage`, `WidgetsPage`, `WidgetLogWorkPage`,
  `ReportPage`, `PlanningAccuracyPage`, `ProjectsPage`, `ProjectDetailPage`, form modalları vb.)
  mekanik `employeeId`→`userId` / `assignedEmployeeId`→`assignedUserId` /
  `ownerEmployeeId`→`ownerUserId` / `projectManagerEmployeeId`→`projectManagerUserId` rename.

Envanter: 47 dosyada 272 eşleşme (grep ile çıkarıldı); rename sonrası
`grep -ri "employee" src/` çıktısında yalnızca `employees.ts`, `useEmployees.ts`,
`EmployeesPage` ve AdminPage'in Çalışanlar bölümü (Faz 4 kapsamı) + görünen Türkçe "Çalışan"
metinleri kalmalı — bu, doğrulama kriteridir.

## Yeni iş kuralının kullanıcıya yansıması

Takvimsiz kullanıcı efor/plan girmeye çalıştığında backend 422 (BusinessRuleValidationException)
döner; mevcut ProblemDetails hata gösterimi mesajı zaten kullanıcıya iletir. Frontend'e ayrıca
ön kontrol **eklenmez** (YAGNI — backend mesajı açıklayıcı).

## Kapsam dışı

- `EmployeesPage`, `api/employees.ts`, `useEmployees` silinmesi (Faz 4).
- Görünen Türkçe metinlerin "Kullanıcı" olarak değiştirilmesi (kullanıcı kararıyla kalıyor).
- Yeni frontend testi (projede frontend test altyapısı yok).

## Doğrulama

1. `npm run build` (tsc) hatasız.
2. Yukarıdaki grep kriteri sağlanır.
3. Backend `RealDb` ile ayaktayken tarayıcıdan: work log listesi açılır, takvimsiz kullanıcıyla
   efor girişi denendiğinde anlamlı hata görünür, kapasite sayfası takvimsiz uyarısını gösterir.
