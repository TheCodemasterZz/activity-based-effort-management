# Employee Entity'sinin Silinmesi (Faz 4) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ölü `Employee` entity'sini, tablosunu, CRUD modülünü ve frontend istemcisini silmek; "Çalışanlar" sayfasını Users API'ye bağlamak.

**Architecture:** Spec: `docs/superpowers/specs/2026-07-24-delete-employee-design.md`. Faz 3 sonrası Employee'ye işaret eden FK/iş akışı yok — silme mekanik. Migration `DropTable` (tablo boş, 0 satır doğrulandı).

**Tech Stack:** .NET 8, EF Core 8 + Npgsql, React + TS.

## Global Constraints

- Görünen Türkçe metinler ("Çalışanlar" menü etiketi dahil) ve MQL `employee` alanı değişmez.
- Sayfa kimliği `employees` (navigation) değişmez.
- Commit sonu: `Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>`.

---

### Task 1: Backend silme + Leave izinleri

**Files:**
- Delete: `backend/src/EforTakip.Domain/Employees/`, `backend/src/EforTakip.Application/Employees/`, `backend/src/EforTakip.Api/Controllers/v1/EmployeesController.cs`, `backend/src/EforTakip.Persistence/Configurations/EmployeeConfiguration.cs`, `backend/tests/EforTakip.Domain.Tests/Employees/`
- Modify: `IApplicationDbContext.cs`, `EforTakipDbContext.cs`, `DependencyInjection.cs` (Persistence), `tests/.../TestDbContext.cs` — `DbSet<Employee>`/`IRepository<Employee>` kayıtları ve using'ler kalkar
- Modify: `backend/src/EforTakip.Domain/Authorization/Permissions.cs` — `Employee` sınıfı silinir, `Leave` eklenir:

```csharp
public static class Leave
{
    public const string Read = "leave:read";
    public const string Manage = "leave:manage";
}
```

- Modify: `LeavesController.cs` — `Permissions.Employee.Read/Manage` → `Permissions.Leave.Read/Manage`

- [ ] Step 1: git rm + edits yukarıdaki gibi.
- [ ] Step 2: `dotnet build` + `dotnet test` yeşil (Domain testlerinde Employee testleri düşer, kalan suite geçer).
- [ ] Step 3: Commit — `refactor: olu Employee entity'si ve CRUD modulu silindi, Leave izinleri eklendi`

### Task 2: Migration — DropTable Employees

- [ ] Step 1: `ASPNETCORE_ENVIRONMENT=RealDb dotnet ef migrations add DropEmployeesTable --project src/EforTakip.Persistence --startup-project src/EforTakip.Api`
- [ ] Step 2: Üretilen Up yalnızca `DropTable("Employees")` içermeli; Down tabloyu (Id, Name, Email, WorkCalendarId + WorkCalendars FK Restrict + index) geri oluşturmalı — otomatik üretim doğruysa dokunma, değilse düzelt.
- [ ] Step 3: `dotnet ef migrations list` tutarlı; commit — `feat: Employees tablosunu dusuren migration`

### Task 3: Frontend

**Files:**
- Delete: `frontend/src/api/employees.ts`, `frontend/src/hooks/useEmployees.ts`
- Modify: `frontend/src/api/types.ts` — `EmployeeDto` silinir
- Modify: `frontend/src/pages/EmployeesPage.tsx` — `useEmployees` → `useUserRoster` (davranış aynı: istemci tarafı arama, izin modalı)
- Modify: `frontend/src/pages/AdminPage.tsx` — `EmployeesSection` ve `'employees'` sekmesi kaldırılır

- [ ] Step 1: Silme + edits; `npm run build` yeşil.
- [ ] Step 2: Commit — `refactor(frontend): Employees API istemcisi silindi, Calisanlar sayfasi Users'a baglandi`

### Task 4: Doğrulama + kapanış

- [ ] Step 1: `grep -ri "employee" backend/src frontend/src` → yalnızca Türkçe metin, MQL alanı, tarihsel migration'lar.
- [ ] Step 2: Test Mode smoke: login → Çalışanlar sayfası kullanıcıları listeler → izin modalı açılır; Yönetim'de Çalışanlar sekmesi yok.
- [ ] Step 3: Tüm suite + build; finishing-a-development-branch (merge seçenekleri).
