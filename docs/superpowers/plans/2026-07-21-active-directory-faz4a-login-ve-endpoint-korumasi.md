# Active Directory Entegrasyonu — Faz 4a: Login Ekranı ve Endpoint Koruması Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Kullanıcıların tek bir login ekranından giriş yapmasını sağlamak, token'ı saklayıp her API isteğine eklemek ve backend endpoint'lerini kimlik doğrulama arkasına almak.

**Architecture:** Uygulamada router yok — sayfa geçişi `useState` ile yapılıyor. Bu nedenle login bir route değil, `App.tsx` seviyesinde bir **kapı (gate)**: oturum yoksa `LoginPage`, varsa uygulama render edilir. Oturum durumu, projede mevcut `lib/notifications.ts` ile aynı desende (modül seviyesi store + listener + hook) tutulur; yeni bir state kütüphanesi veya Context eklenmez.

**Tech Stack:** React 19, TypeScript, Tailwind 4, TanStack Query 5, Vite 8, oxlint; backend tarafında ASP.NET Core 8 authorization.

## Global Constraints

- **Yeni bağımlılık eklenmez.** Router, state kütüphanesi, form kütüphanesi yok — mevcut araçlarla çözülür.
- Mevcut kod stiliyle uyum: fonksiyon bileşenleri, `export function X()`, Tailwind sınıfları, Türkçe kullanıcı metinleri, açıklama yorumları yalnızca kodun anlatamadığı kararlar için.
- **Token asla loglanmaz veya URL'e konmaz.**
- Backend'de `/auth/login` ve `/health` anonim kalır; **diğer tüm endpoint'ler kimlik doğrulama ister**.
- Frontend'de tip hatası bırakılmaz — `npm run build` (tsc -b) temiz geçmelidir.

## Kapsam Kararı: Token localStorage'da tutulur

`localStorage` XSS durumunda okunabilir; `httpOnly` cookie daha güvenlidir. Ancak cookie'ye geçmek CSRF koruması, cookie ayarları ve CORS credential yapılandırması gerektirir — bu fazın hedefini aşar. Uygulama henüz üretime alınmadığı için `localStorage` ile başlanır ve bu sınır dokümante edilir. `sessionStorage` tercih edilmez: sekme kapanınca oturum düşer, kullanıcı deneyimi bozulur.

---

## Dosya Yapısı

**Frontend (`frontend/src/`):**
- `lib/auth.ts` — oturum store'u (token saklama, süre kontrolü, listener)
- `hooks/useAuthSession.ts` — store'u React'e bağlayan hook
- `api/auth.ts` — login isteği
- `api/types.ts` — değişiklik: `LoginResultDto`
- `api/client.ts` — değişiklik: Authorization header + 401 yönetimi
- `pages/LoginPage.tsx` — giriş ekranı
- `App.tsx` — değişiklik: oturum kapısı
- `components/layout/ProfileMenu.tsx` — değişiklik: gerçek kullanıcı + çıkış
- `components/layout/Header.tsx` — değişiklik: ProfileMenu'ye kullanıcı geçirme (gerekirse)

**Backend (`backend/src/EforTakip.Api/`):**
- `Extensions/ApiServiceCollectionExtensions.cs` — değişiklik: fallback authorization policy
- `Controllers/v1/AuthController.cs` — değişiklik: `[AllowAnonymous]`
- `Program.cs` — değişiklik: health check anonim

---

## Task 1: Oturum store'u ve hook

**Files:**
- Create: `frontend/src/lib/auth.ts`
- Create: `frontend/src/hooks/useAuthSession.ts`

**Interfaces:**
- Produces:
  - `AuthSession { token: string; expiresAtUtc: string; userId: string; username: string; displayName: string | null; source: number }`
  - `getSession(): AuthSession | null` — süresi dolmuşsa null döner ve depoyu temizler
  - `setSession(session: AuthSession): void`
  - `clearSession(): void`
  - `subscribeToAuth(listener: () => void): () => void`
  - `useAuthSession(): AuthSession | null`

- [ ] **Step 1: Oturum store'unu yaz**

`frontend/src/lib/auth.ts`:
```typescript
export interface AuthSession {
  token: string;
  expiresAtUtc: string;
  userId: string;
  username: string;
  displayName: string | null;
  source: number;
}

const STORAGE_KEY = 'mesainame.auth';

type Listener = () => void;
const listeners = new Set<Listener>();

let cachedSession: AuthSession | null | undefined;

function emit() {
  listeners.forEach((listener) => listener());
}

function readFromStorage(): AuthSession | null {
  const raw = localStorage.getItem(STORAGE_KEY);
  if (!raw) return null;

  try {
    return JSON.parse(raw) as AuthSession;
  } catch {
    // Bozuk kayıt oturumsuz sayılır; kullanıcı yeniden giriş yapar.
    localStorage.removeItem(STORAGE_KEY);
    return null;
  }
}

function isExpired(session: AuthSession): boolean {
  return new Date(session.expiresAtUtc).getTime() <= Date.now();
}

export function getSession(): AuthSession | null {
  if (cachedSession === undefined) {
    cachedSession = readFromStorage();
  }

  if (cachedSession && isExpired(cachedSession)) {
    clearSession();
    return null;
  }

  return cachedSession;
}

export function setSession(session: AuthSession): void {
  cachedSession = session;
  localStorage.setItem(STORAGE_KEY, JSON.stringify(session));
  emit();
}

export function clearSession(): void {
  cachedSession = null;
  localStorage.removeItem(STORAGE_KEY);
  emit();
}

export function subscribeToAuth(listener: Listener): () => void {
  listeners.add(listener);
  return () => listeners.delete(listener);
}
```

- [ ] **Step 2: Hook'u yaz**

`frontend/src/hooks/useAuthSession.ts`:
```typescript
import { useSyncExternalStore } from 'react';
import { getSession, subscribeToAuth, type AuthSession } from '../lib/auth';

export function useAuthSession(): AuthSession | null {
  return useSyncExternalStore(subscribeToAuth, getSession, () => null);
}
```

- [ ] **Step 3: Derleme kontrolü**

Run: `cd frontend && npx tsc -b --noEmit 2>&1 | head -20`
Expected: Hata yok. (Henüz kullanılmayan export'lar hata üretmez.)

- [ ] **Step 4: Commit**

```bash
git add frontend/src/lib/auth.ts frontend/src/hooks/useAuthSession.ts
git commit -m "feat: add auth session store and hook"
```

---

## Task 2: API client'a token ve 401 yönetimi

**Files:**
- Modify: `frontend/src/api/types.ts`
- Modify: `frontend/src/api/client.ts`
- Create: `frontend/src/api/auth.ts`

**Interfaces:**
- Consumes: `getSession`, `clearSession` (Task 1).
- Produces: `LoginResultDto` tipi; `login(username, password)` fonksiyonu; her istekte `Authorization: Bearer <token>` header'ı.

**Davranış:** Sunucu 401 dönerse oturum temizlenir — token süresi dolmuş veya geçersizdir, kullanıcı login ekranına düşer. Login isteğinin kendisi 401 dönerse zaten oturum yoktur, temizleme zararsızdır.

- [ ] **Step 1: LoginResultDto tipini ekle**

`frontend/src/api/types.ts` — dosyanın sonuna ekle:
```typescript
export interface LoginResultDto {
  token: string;
  expiresAtUtc: string;
  userId: string;
  username: string;
  displayName: string | null;
  source: number;
}
```

- [ ] **Step 2: client.ts'e token ve 401 yönetimi ekle**

`frontend/src/api/client.ts` — using satırına ekle:
```typescript
import { clearSession, getSession } from '../lib/auth';
```

`request` fonksiyonunu değiştir:
```typescript
async function request<T>(
  method: string,
  path: string,
  options?: { query?: Record<string, QueryValue>; body?: unknown },
): Promise<T> {
  const headers: Record<string, string> = {};

  if (options?.body !== undefined) {
    headers['Content-Type'] = 'application/json';
  }

  const session = getSession();
  if (session) {
    headers.Authorization = `Bearer ${session.token}`;
  }

  const response = await fetch(buildUrl(path, options?.query), {
    method,
    headers: Object.keys(headers).length > 0 ? headers : undefined,
    body: options?.body !== undefined ? JSON.stringify(options.body) : undefined,
  });

  if (!response.ok) {
    // 401: token süresi dolmuş veya geçersiz — oturumu düşür, kullanıcı login ekranına gitsin.
    if (response.status === 401) {
      clearSession();
    }

    let problem: ProblemDetails | null = null;
    try {
      problem = await response.json();
    } catch {
      problem = null;
    }
    throw new ApiError(response.status, problem);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  // Bazı 201 Created yanıtları (ör. ProjectsController.Create) gövdesiz döner — .json() boş
  // gövdede "Unexpected end of JSON input" fırlatır, önce metni okuyup boşsa undefined dönüyoruz.
  const text = await response.text();
  return (text ? JSON.parse(text) : undefined) as T;
}
```

- [ ] **Step 3: auth API fonksiyonunu yaz**

`frontend/src/api/auth.ts`:
```typescript
import { apiClient } from './client';
import type { LoginResultDto } from './types';

export function login(username: string, password: string) {
  return apiClient.post<LoginResultDto>('/api/v1/auth/login', { username, password });
}
```

- [ ] **Step 4: Derleme kontrolü**

Run: `cd frontend && npx tsc -b --noEmit 2>&1 | head -20`
Expected: Hata yok.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/api/
git commit -m "feat: attach bearer token to api requests and clear session on 401"
```

---

## Task 3: Login sayfası

**Files:**
- Create: `frontend/src/pages/LoginPage.tsx`

**Interfaces:**
- Consumes: `login` (Task 2), `setSession` (Task 1), `ApiError`.
- Produces: `LoginPage` bileşeni — başarılı girişte oturumu kurar, App kapısı otomatik olarak uygulamayı gösterir.

**Davranış:** Gönderim sırasında buton devre dışı ve "Giriş yapılıyor…" gösterir. Hata sunucudan gelen mesajla gösterilir (401'de "Kullanıcı adı veya şifre hatalı."). Kullanıcı adı ve şifre boşken buton pasiftir.

- [ ] **Step 1: LoginPage'i yaz**

`frontend/src/pages/LoginPage.tsx`:
```tsx
import { useState, type FormEvent } from 'react';
import { login } from '../api/auth';
import { ApiError } from '../api/client';
import { setSession } from '../lib/auth';

export function LoginPage() {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const canSubmit = username.trim().length > 0 && password.length > 0 && !isSubmitting;

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault();
    if (!canSubmit) return;

    setIsSubmitting(true);
    setErrorMessage(null);

    try {
      const result = await login(username.trim(), password);
      setSession(result);
    } catch (error) {
      setErrorMessage(
        error instanceof ApiError
          ? error.message
          : 'Giriş yapılamadı. Lütfen daha sonra tekrar deneyin.',
      );
      setIsSubmitting(false);
    }
  };

  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-50 px-4">
      <div className="w-full max-w-sm">
        <div className="mb-6 text-center">
          <h1 className="text-2xl font-semibold text-slate-800">Mesainame</h1>
          <p className="mt-1 text-sm text-slate-500">Devam etmek için giriş yapın</p>
        </div>

        <form
          onSubmit={handleSubmit}
          className="rounded-xl border border-slate-200 bg-white p-6 shadow-sm"
        >
          <label className="block">
            <span className="mb-1 block text-sm font-medium text-slate-700">Kullanıcı Adı</span>
            <input
              type="text"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              autoComplete="username"
              autoFocus
              className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500"
            />
          </label>

          <label className="mt-4 block">
            <span className="mb-1 block text-sm font-medium text-slate-700">Şifre</span>
            <input
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              autoComplete="current-password"
              className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500"
            />
          </label>

          {errorMessage && (
            <p role="alert" className="mt-4 rounded-md bg-rose-50 px-3 py-2 text-sm text-rose-700">
              {errorMessage}
            </p>
          )}

          <button
            type="submit"
            disabled={!canSubmit}
            className="mt-6 w-full rounded-md bg-indigo-600 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-700 disabled:cursor-not-allowed disabled:bg-slate-300"
          >
            {isSubmitting ? 'Giriş yapılıyor…' : 'Giriş Yap'}
          </button>
        </form>

        <p className="mt-4 text-center text-xs text-slate-400">
          Kurum hesabınızla veya size verilen kullanıcı bilgileriyle giriş yapabilirsiniz.
        </p>
      </div>
    </div>
  );
}
```

- [ ] **Step 2: Derleme kontrolü**

Run: `cd frontend && npx tsc -b --noEmit 2>&1 | head -20`
Expected: Hata yok.

- [ ] **Step 3: Commit**

```bash
git add frontend/src/pages/LoginPage.tsx
git commit -m "feat: add login page"
```

---

## Task 4: App'e oturum kapısı

**Files:**
- Modify: `frontend/src/App.tsx`

**Interfaces:**
- Consumes: `useAuthSession` (Task 1), `LoginPage` (Task 3).

**Davranış:** Oturum yoksa — widget modu dahil — `LoginPage` gösterilir. Widget de API çağırdığı için token'sız çalışamaz.

- [ ] **Step 1: App.tsx'i güncelle**

`frontend/src/App.tsx` — import ekle:
```typescript
import { LoginPage } from './pages/LoginPage';
import { useAuthSession } from './hooks/useAuthSession';
```

`App` fonksiyonunu değiştir:
```tsx
function App() {
  const [activePage, setActivePage] = useState<AppPage>('workLog');
  const session = useAuthSession();

  return (
    <QueryClientProvider client={queryClient}>
      <NotificationHost />
      {/* Oturum yoksa widget modu dahil hiçbir şey gösterilmez — widget de API'ye token ile gider. */}
      {!session ? (
        <LoginPage />
      ) : isWidgetMode() ? (
        <WidgetLogWorkPage />
      ) : (
        <RootLayout activePage={activePage} onNavigate={setActivePage}>
          {activePage === 'home' && <HomePage />}
          {activePage === 'planWork' && <PlanWorkPage />}
          {activePage === 'workLog' && <ReportPage />}
          {activePage === 'capacityManagement' && <CapacityManagementPage />}
          {activePage === 'projects' && <ProjectsPage />}
          {activePage === 'employees' && <EmployeesPage />}
          {activePage === 'widgets' && <WidgetsPage />}
          {activePage === 'admin' && <AdminPage />}
        </RootLayout>
      )}
    </QueryClientProvider>
  );
}
```

- [ ] **Step 2: Derleme kontrolü**

Run: `cd frontend && npx tsc -b --noEmit 2>&1 | head -20`
Expected: Hata yok.

- [ ] **Step 3: Commit**

```bash
git add frontend/src/App.tsx
git commit -m "feat: gate application behind login"
```

---

## Task 5: ProfileMenu'de gerçek kullanıcı ve çıkış

**Files:**
- Modify: `frontend/src/components/layout/ProfileMenu.tsx`

**Interfaces:**
- Consumes: `useAuthSession`, `clearSession`; TanStack Query `useQueryClient`.

**Davranış:** Menüde giriş yapan kullanıcının görünen adı ve kullanıcı adı yer alır; baş harfleri avatar olarak gösterilir. "Çıkış" oturumu kapatır ve **önbelleği temizler** — aksi halde bir sonraki kullanıcı öncekinin verisini görebilir. İşlevi olmayan "Profili Düzenle"/"Dil Değiştir" öğeleri kaldırılır; var olmayan özellikleri ima etmemeleri gerekir.

- [ ] **Step 1: ProfileMenu'yü yeniden yaz**

`frontend/src/components/layout/ProfileMenu.tsx`:
```tsx
import { useEffect, useRef, useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { useAuthSession } from '../../hooks/useAuthSession';
import { clearSession } from '../../lib/auth';

/** Görünen addan baş harfleri üretir: "Serkan Gültepe" → "SG". */
function toInitials(name: string): string {
  const parts = name.trim().split(/\s+/).filter(Boolean);
  if (parts.length === 0) return '?';
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
  return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
}

export function ProfileMenu() {
  const [isOpen, setIsOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);
  const session = useAuthSession();
  const queryClient = useQueryClient();

  useEffect(() => {
    if (!isOpen) return;

    const handleClickOutside = (event: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [isOpen]);

  if (!session) return null;

  const displayName = session.displayName?.trim() || session.username;

  const handleLogout = () => {
    setIsOpen(false);
    // Önbellek temizlenmezse sonraki kullanıcı öncekinin verisini görebilir.
    queryClient.clear();
    clearSession();
  };

  return (
    <div ref={containerRef} className="relative">
      <button type="button" onClick={() => setIsOpen((v) => !v)} className="flex items-center gap-2">
        <div className="flex h-8 w-8 items-center justify-center rounded-full bg-indigo-100 text-xs font-bold text-indigo-700">
          {toInitials(displayName)}
        </div>
        <div className="text-left">
          <div className="text-sm font-medium text-slate-800">{displayName}</div>
          <div className="text-xs text-slate-400">{session.username}</div>
        </div>
        <span className="text-slate-300">▾</span>
      </button>

      {isOpen && (
        <div className="absolute right-0 z-40 mt-2 w-48 rounded-lg border border-slate-200 bg-white p-1 shadow-lg">
          <button
            type="button"
            onClick={handleLogout}
            className="block w-full rounded-md px-3 py-2 text-left text-sm text-slate-600 hover:bg-slate-50"
          >
            Çıkış
          </button>
        </div>
      )}
    </div>
  );
}
```

- [ ] **Step 2: Derleme ve lint kontrolü**

Run: `cd frontend && npx tsc -b --noEmit 2>&1 | head -20 && npm run lint 2>&1 | tail -5`
Expected: Tip hatası yok; lint temiz.

- [ ] **Step 3: Commit**

```bash
git add frontend/src/components/layout/ProfileMenu.tsx
git commit -m "feat: show signed-in user and enable logout"
```

---

## Task 6: İlk kullanıcı (bootstrap) — koruma açılmadan önce zorunlu

**Files:**
- Create: `backend/src/EforTakip.Persistence/Seed/BootstrapAdminSeeder.cs`
- Modify: `backend/src/EforTakip.Api/Program.cs`
- Modify: `backend/src/EforTakip.Api/appsettings.Development.json`

**Interfaces:**
- Consumes: `EforTakipDbContext`, `IPasswordHasher`, `Directory.CreateInternal`, `DirectoryUser.CreateInternal`.
- Produces: `BootstrapAdminSeeder.SeedAsync(EforTakipDbContext db, IPasswordHasher passwordHasher, string? username, string? password, ILogger logger, CancellationToken ct)`

**Neden gerekli:** Task 7 korumayı açtıktan sonra `POST /directoryusers/internal` de token ister. Sistemde hiç kullanıcı yoksa token alınamaz, kullanıcı da oluşturulamaz — kimse giriş yapamaz. Bu yüzden sistemde hiç kullanıcı yokken açılışta bir yönetici hesabı oluşturulur.

**Davranış:** Yalnızca `DirectoryUsers` tablosu **boşken** çalışır (mevcut kuruluma dokunmaz). Kullanıcı adı/şifre konfigürasyondan gelir; tanımlı değilse hesap oluşturulmaz ve uyarı loglanır. **Şifre loglanmaz.**

- [ ] **Step 1: Seeder'ı yaz**

`backend/src/EforTakip.Persistence/Seed/BootstrapAdminSeeder.cs`:
```csharp
using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Directories;
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

        var admin = DirectoryUser.CreateInternal(
            directory.Id, username, null, null, username, null, passwordHasher.Hash(password));

        db.DirectoryUsers.Add(admin);
        await db.SaveChangesAsync(cancellationToken);

        // Şifre bilinçli olarak loglanmaz.
        logger.LogInformation("İlk yönetici hesabı oluşturuldu: {Username}", admin.Username);
    }
}
```

- [ ] **Step 2: Program.cs'te seeder'ı çağır**

`backend/src/EforTakip.Api/Program.cs` — mevcut test-mode seed bloğunun **altına**, ortamdan bağımsız çalışacak şekilde ekle:
```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EforTakipDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
        .CreateLogger(nameof(BootstrapAdminSeeder));

    await BootstrapAdminSeeder.SeedAsync(
        db,
        passwordHasher,
        builder.Configuration["Bootstrap:AdminUsername"],
        builder.Configuration["Bootstrap:AdminPassword"],
        logger,
        CancellationToken.None);
}
```

using ekle:
```csharp
using EforTakip.Application.Common.Interfaces;
```

- [ ] **Step 3: Geliştirme konfigürasyonuna bootstrap değerlerini ekle**

`backend/src/EforTakip.Api/appsettings.Development.json` — kök nesneye ekle:
```json
  "Bootstrap": {
    "AdminUsername": "admin",
    "AdminPassword": "Admin123!"
  }
```

**Not:** Yalnızca geliştirme içindir. Üretimde `Bootstrap__AdminUsername` / `Bootstrap__AdminPassword` environment variable'ları ile verilir ve ilk giriş sonrası şifre değiştirilmelidir.

- [ ] **Step 4: Derle ve bootstrap'i doğrula**

Run: `dotnet build backend/EforTakip.sln`
Expected: Build succeeded.

API'yi başlat ve logda hesabın oluştuğunu doğrula:
```bash
grep -i "İlk yönetici hesabı" logs/log-*.txt | head -1
```
Expected: "İlk yönetici hesabı oluşturuldu: admin"

Şifrenin loglanmadığını doğrula:
```bash
grep -c "Admin123!" logs/log-*.txt
```
Expected: `0`

API'yi durdur.

- [ ] **Step 5: Commit**

```bash
git add backend/src/EforTakip.Persistence/Seed/BootstrapAdminSeeder.cs backend/src/EforTakip.Api/
git commit -m "feat: seed bootstrap admin account when no users exist"
```

---

## Task 7: Backend endpoint korumasını aç

**Files:**
- Modify: `backend/src/EforTakip.Api/Extensions/ApiServiceCollectionExtensions.cs`
- Modify: `backend/src/EforTakip.Api/Controllers/v1/AuthController.cs`
- Modify: `backend/src/EforTakip.Api/Program.cs`

**Interfaces:**
- Produces: Tüm endpoint'ler varsayılan olarak kimlik doğrulama ister; `/auth/login` ve `/health` anonim kalır.

**Yaklaşım:** Her controller'a tek tek `[Authorize]` eklemek yerine **fallback policy** kullanılır — böylece ileride eklenen bir controller yanlışlıkla korumasız kalmaz (güvenli varsayılan).

- [ ] **Step 1: Fallback authorization policy'yi ekle**

`ApiServiceCollectionExtensions.cs` — `services.AddAuthorization();` satırını değiştir:
```csharp
        // Fallback policy: [AllowAnonymous] ile işaretlenmemiş her endpoint kimlik doğrulama ister.
        // Yeni eklenen bir controller'ın yanlışlıkla korumasız kalmasını engeller.
        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });
```

using ekle:
```csharp
using Microsoft.AspNetCore.Authorization;
```

- [ ] **Step 2: AuthController'ı anonim yap**

`AuthController.cs` — using ekle:
```csharp
using Microsoft.AspNetCore.Authorization;
```

`Login` metodunun üstüne ekle:
```csharp
    [AllowAnonymous]
```

- [ ] **Step 3: Health check'i anonim yap**

`Program.cs` — `app.MapHealthChecks("/health");` satırını değiştir:
```csharp
app.MapHealthChecks("/health").AllowAnonymous();
```

- [ ] **Step 4: Derle ve testleri çalıştır**

Run:
```bash
dotnet build backend/EforTakip.sln
dotnet test backend/tests/EforTakip.Domain.Tests/EforTakip.Domain.Tests.csproj
dotnet test backend/tests/EforTakip.Application.Tests/EforTakip.Application.Tests.csproj
```
Expected: Build succeeded; Domain PASS; Application'da yalnızca bilinen 2 `LogWorkCommandHandlerTests` hatası kalır.

- [ ] **Step 5: Korumayı canlı doğrula**

API'yi başlat, sonra:

1. Token'sız istek reddedilmeli:
```bash
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5298/api/v1/employees
```
Expected: `401`

2. Health anonim çalışmalı:
```bash
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5298/health
```
Expected: `200`

3. Login anonim çalışmalı ve token vermeli (hesap Task 6'daki bootstrap ile oluşmuş olmalı):
```bash
TOKEN=$(curl -s -X POST http://localhost:5298/api/v1/auth/login -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"Admin123!"}' | grep -o '"token":"[^"]*"' | cut -d'"' -f4)
echo "token uzunlugu: ${#TOKEN}"
```
Expected: Uzunluk 0'dan büyük.

4. Token ile istek geçmeli:
```bash
curl -s -o /dev/null -w "%{http_code}\n" -H "Authorization: Bearer $TOKEN" http://localhost:5298/api/v1/employees
```
Expected: `200`

5. Geçersiz token reddedilmeli:
```bash
curl -s -o /dev/null -w "%{http_code}\n" -H "Authorization: Bearer gecersiz.token.degeri" http://localhost:5298/api/v1/employees
```
Expected: `401`

API'yi durdur.

- [ ] **Step 6: Commit**

```bash
git add backend/src/EforTakip.Api/
git commit -m "feat: require authentication for all endpoints except login and health"
```

---

## Task 8: Uçtan uca tarayıcı doğrulaması

**Files:** (kod değişikliği yok — doğrulama görevi)

- [ ] **Step 1: Backend ve frontend'i başlat**

Backend:
```bash
dotnet run --project backend/src/EforTakip.Api --urls http://localhost:5298
```

Frontend (ayrı terminal):
```bash
cd frontend && npm run dev
```

- [ ] **Step 2: Giriş yapılmamışken login ekranının çıktığını doğrula**

Tarayıcıda `http://localhost:5173` aç.
Expected: "Mesainame / Devam etmek için giriş yapın" başlıklı login formu görünür; uygulama içeriği (menü, sayfalar) görünmez.

- [ ] **Step 3: Yanlış şifreyle hatayı doğrula**

Login formuna `admin` / `yanlis` gir, Giriş Yap'a bas.
Expected: Form üzerinde "Kullanıcı adı veya şifre hatalı." mesajı görünür; sayfa değişmez.

- [ ] **Step 4: Doğru şifreyle girişi doğrula**

`admin` / `Admin123!` gir (Task 6'daki bootstrap hesabı).
Expected: Uygulama açılır, sağ üstte `admin` görünür.

- [ ] **Step 5: Büyük harfli kullanıcı adıyla girişi doğrula**

Çıkış yapıp `ADMIN` / `Admin123!` ile gir.
Expected: Giriş başarılı — kullanıcı adları büyük/küçük harf duyarsızdır (Faz 3'te düzeltilen Türkçe `I` sorununun regresyon kontrolü).

- [ ] **Step 6: Sayfa yenilendiğinde oturumun korunduğunu doğrula**

Sayfayı yenile (F5).
Expected: Login ekranı gelmez, uygulama açık kalır.

- [ ] **Step 7: Çıkışı doğrula**

Profil menüsünü aç, "Çıkış"a bas.
Expected: Login ekranına dönülür. Tekrar yenilendiğinde de login ekranı gelir.

- [ ] **Step 8: Bozuk token ile oturumun düştüğünü doğrula**

Giriş yap, sonra tarayıcı konsolunda:
```javascript
const s = JSON.parse(localStorage.getItem('mesainame.auth'));
s.token = 'bozuk.token.degeri';
localStorage.setItem('mesainame.auth', JSON.stringify(s));
location.reload();
```
Expected: Sayfa yüklenince API 401 döner, oturum temizlenir ve login ekranı görünür.

- [ ] **Step 9: Bulguları raporla**

Beklenenden sapma varsa düzelt ve ilgili görevi tekrar çalıştır.

---

## Faz 4a Tamamlanma Kriteri

- [ ] `cd frontend && npm run build` temiz geçiyor (tsc + vite build).
- [ ] `cd frontend && npm run lint` temiz.
- [ ] `dotnet build backend/EforTakip.sln` başarılı; testlerde yalnızca bilinen 2 hata kalıyor.
- [ ] Token'sız API isteği 401, geçersiz token 401, geçerli token 200 dönüyor.
- [ ] `/health` ve `/auth/login` token'sız çalışıyor.
- [ ] Giriş yapılmamışken uygulama içeriği hiç render edilmiyor (widget modu dahil).
- [ ] Giriş yapan kullanıcının adı header'da görünüyor; çıkış oturumu ve önbelleği temizliyor.
- [ ] Sayfa yenilendiğinde oturum korunuyor; token bozulduğunda oturum düşüyor.
- [ ] Sistemde kullanıcı yokken açılışta bootstrap yönetici hesabı oluşuyor; şifresi loglanmıyor.

## Bilinen Sınırlar

- **Token `localStorage`'da tutuluyor** — XSS durumunda okunabilir. `httpOnly` cookie'ye geçiş CSRF koruması ve CORS credential yapılandırması gerektirir; ayrı bir iş olarak ele alınmalıdır.
- Token yenileme yok — süre dolunca kullanıcı yeniden giriş yapar.
- Rol/yetki bazlı kısıtlama yok: giriş yapan her kullanıcı tüm endpoint'lere erişebilir. Yönetim ekranlarının yalnızca yöneticilere açılması ayrı bir iştir (spec'te "Roller ve İzinler" bölümü zaten "yakında" olarak işaretli).
- Widget modu da giriş ister; widget'ın gömüleceği senaryoda ayrı bir erişim yöntemi gerekirse yeniden değerlendirilmelidir.
- Bootstrap yönetici şifresi konfigürasyondan gelir ve ilk giriş sonrası **elle değiştirilemez** — şifre değiştirme komutu henüz yok. Üretime geçmeden önce eklenmelidir.

## Sonraki Faz

- **Faz 4b — Kullanıcı Klasörü UI:** dizin listesi ve formu (Server Settings, LDAP Schema, LDAP Permissions, User Schema Settings, Sync Schedule), global Alan Eşlemeleri bölümü, dizin kullanıcı listesi ve tüm attribute'ları gösteren Kullanıcı Kartı, "Senkronize Et" ve "Bağlantıyı Test Et" aksiyonları.
