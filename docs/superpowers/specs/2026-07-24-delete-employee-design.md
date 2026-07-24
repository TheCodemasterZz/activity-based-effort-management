# Employee Entity'sinin Silinmesi (Faz 4) — Tasarım

## Bağlam

Employee/User birleştirmesinin son fazı. Faz 3 sonunda `Employee`'ye işaret eden hiçbir FK ve
iş akışı kalmadı; entity, tablosu ve CRUD modülü artık ölü kod. Bu faz hepsini siler.
Kullanıcı kararı: arayüzdeki "Çalışanlar" sayfası kalır ama **Users API'den** beslenir;
Yönetim'deki mükerrer "Çalışanlar" bölümü kaldırılır (Kullanıcılar bölümü zaten var).

## Backend — silinecekler

- `Domain/Employees/Employee.cs` (+ klasör)
- `Application/Employees/**` (CreateEmployee, GetEmployees, GetEmployeeById, EmployeeDto,
  EmployeeMappingConfig)
- `Api/Controllers/v1/EmployeesController.cs`
- `Persistence/Configurations/EmployeeConfiguration.cs`
- `IApplicationDbContext` + `EforTakipDbContext` + test `TestDbContext`'ten `DbSet<Employee>`
- `DependencyInjection`'daki `IRepository<Employee>` kaydı
- `tests/EforTakip.Domain.Tests/Employees/EmployeeTests.cs`

## İzin kataloğu (LeavesController)

`LeavesController` hâlâ `Permissions.Employee.Read/Manage` kullanıyor. `Permissions.Employee`
sınıfı silinir, yerine `Permissions.Leave` eklenir (`leave:read`, `leave:manage`) ve
`LeavesController` buna geçer (CLAUDE.md kuralı: sabit + attribute, başka adım yok).

**Not:** DB'deki rol kayıtlarında `employee:read` / `employee:*` anahtarı verilmiş roller varsa
bu roller izin listesinden otomatik `leave:*` kazanmaz — admin'in rol ekranından yeniden
vermesi gerekir. `IsSystemAdmin` roller etkilenmez (her izni otomatik geçer). Şu an gerçek
DB'de yalnızca Sistem Yöneticisi rolü olduğu için pratik etki yok.

## Migration

`DropTable("Employees")` (+Down'da tabloyu FK'sız yeniden oluşturur — Faz 3 sonrası tabloya
işaret eden FK zaten yok). Gerçek DB'de tablo **boş** (daha önce doğrulandı, 0 satır);
`TestDataSeeder` Faz 3'ten beri Employee üretmiyor. Veri kaybı yok.

## Frontend

- Silinir: `api/employees.ts`, `hooks/useEmployees.ts`, `api/types.ts`'teki `EmployeeDto`.
- `EmployeesPage` ("Çalışanlar" menüsü): `useUserRoster()` (aktif kullanıcılar) ile beslenir;
  arama mevcut haliyle istemci tarafında kalır, izin takvimi modalı zaten User id'yle çalışıyor.
- `AdminPage`: `EmployeesSection` ve "Çalışanlar" sekmesi kaldırılır.
- Menü etiketi "Çalışanlar" ve sayfa kimliği `employees` değişmez.

## Kapsam dışı

- MQL alan adı `employee` ve görünen Türkçe metinler (değişmiyor, Faz 3 kararı).
- Rol izinlerinin DB'de otomatik taşınması (yukarıdaki not; kod dışı, admin işi).

## Doğrulama

1. `dotnet build` + `dotnet test` + `npm run build` yeşil.
2. `grep -ri "employee" backend/src frontend/src` çıktısında yalnızca Türkçe "Çalışan" metinleri,
   MQL alanı ve tarihsel migration dosyaları kalır.
3. Test Mode smoke: login, Çalışanlar sayfası kullanıcıları listeler, izin modalı açılır.
4. Migration gerçek DB'ye kullanıcı tarafından uygulanır (Users satır sayısı değişmez).
