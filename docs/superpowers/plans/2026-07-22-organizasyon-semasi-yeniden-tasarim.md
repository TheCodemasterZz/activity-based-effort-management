# Organizasyon Şeması Yeniden Tasarım Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Organizasyon Şeması'nı Kullanıcı Klasörü akışından çıkarıp Ayarlar sayfasının sol
menüsünde kendi başına bir bölüm yapmak, ve mevcut yatay kutu-ağacı düzenini yatay taşma
sorununu ortadan kaldıran dikey/girintili, daraltılabilir bir liste ile değiştirmek.

**Architecture:** `AdminPage.tsx`'in sol menüsüne yeni bir `orgChart` bölümü eklenir. Yeni
`OrgChartSection.tsx` bileşeni dizin seçimini ve seçili kullanıcı için modal state'ini yönetir;
`OrgChart.tsx` yeniden yazılarak saf bir sunum bileşenine dönüşür (kendi `directoryId` prop'una
göre `useOrgChart` çağırır, dikey/girintili ağaç render eder). Kullanıcı kartı artık yeni bir
`DirectoryUserCardModal.tsx` içinde açılır; bunun için `DirectoryUserCard`'ın `onBack` prop'u
opsiyonel hale getirilir. Backend'de hiçbir değişiklik yoktur.

**Tech Stack:** React 19, TypeScript, Tailwind 4, TanStack Query 5 (mevcut `useOrgChart`,
`useDirectories` hook'ları aynen kullanılır).

## Global Constraints

- Backend'de hiçbir dosya değişmez — `GET /api/v1/directories/{id}/org-chart` ve
  `OrgChartNodeDto`/`OrgChartResultDto` aynen korunur.
- Proje genelinde component-level otomatik test altyapısı yoktur; doğrulama `npx tsc --noEmit`
  (tip kontrolü) ve tarayıcıda manuel uçtan uca kontrolle yapılır — bu, projenin önceki tüm
  fazlarında izlenen yöntemdir.
- Var olan Tailwind sınıf/renk konvansiyonlarına uyulur (`slate`/`indigo` paleti, `rounded-md`,
  `text-sm` gibi mevcut bileşenlerde kullanılan örüntüler).
- Her adımdan sonra derleme/tip kontrolü kırılmamalı — orta adımlarda geçici olarak "kullanılmayan
  ama var olan" dosyalar olabilir, ama hiçbir commit noktasında `tsc` hata vermemeli.

---

### Task 1: `DirectoryUserCard`'ın `onBack` prop'unu opsiyonel yap

**Files:**
- Modify: `frontend/src/components/admin/directory/DirectoryUserCard.tsx:8-12` (prop tipi),
  `frontend/src/components/admin/directory/DirectoryUserCard.tsx:141-150` (JSX)

**Interfaces:**
- Produces: `DirectoryUserCardProps.onBack?: () => void` (artık opsiyonel) — Task 4 (modal) bu
  prop'u hiç vermeden `DirectoryUserCard`'ı kullanacak.

- [ ] **Step 1: `onBack` prop tipini opsiyonel yap**

`frontend/src/components/admin/directory/DirectoryUserCard.tsx` dosyasında satır 8-12'yi bul:

```tsx
interface DirectoryUserCardProps {
  userId: string;
  onBack: () => void;
  onSelectUser?: (userId: string) => void;
}
```

Şununla değiştir:

```tsx
interface DirectoryUserCardProps {
  userId: string;
  onBack?: () => void;
  onSelectUser?: (userId: string) => void;
}
```

- [ ] **Step 2: Geri butonunu `onBack` varsa göster**

Aynı dosyada satır 141-150'yi bul:

```tsx
      <div className="mb-4 flex items-center justify-between">
        <h2 className="text-base font-semibold text-slate-800">Kullanıcı Kartı</h2>
        <button
          type="button"
          onClick={onBack}
          className="text-sm text-slate-500 hover:text-slate-700"
        >
          ← Kullanıcılara dön
        </button>
      </div>
```

Şununla değiştir:

```tsx
      <div className="mb-4 flex items-center justify-between">
        <h2 className="text-base font-semibold text-slate-800">Kullanıcı Kartı</h2>
        {onBack && (
          <button
            type="button"
            onClick={onBack}
            className="text-sm text-slate-500 hover:text-slate-700"
          >
            ← Kullanıcılara dön
          </button>
        )}
      </div>
```

- [ ] **Step 3: Tip kontrolü**

Run: `cd frontend && npx tsc --noEmit`
Expected: Hata yok (mevcut tüm `DirectoryUserCard` kullanımları zaten `onBack` veriyor, bu değişiklik geriye dönük uyumlu).

- [ ] **Step 4: Commit**

```bash
git add frontend/src/components/admin/directory/DirectoryUserCard.tsx
git commit -m "feat: make DirectoryUserCard onBack prop optional for modal usage"
```

---

### Task 2: Organizasyon Şeması'nın eski giriş noktalarını Kullanıcı Klasörü akışından kaldır

Bu adımdan sonra `OrgChart.tsx` geçici olarak hiçbir yerden import edilmeyecek (Task 3'te yeniden
yazılıp Task 5'te yeni yerinden kullanılacak) — bu ara durumda derleme hatası oluşmaz çünkü
kullanılmayan bir dosyanın var olması TypeScript'i bozmaz.

**Files:**
- Modify: `frontend/src/components/admin/directory/UserDirectorySection.tsx` (tamamen)
- Modify: `frontend/src/components/admin/directory/DirectoryUserList.tsx:5-19,30-48`

**Interfaces:**
- Consumes: Yok (yalnızca eski navigasyon kaldırılıyor).
- Produces: `DirectoryUserListProps` artık `onViewOrgChart` içermiyor — Task 5 sonrası
  "Organizasyon Şeması" girişi yalnızca sol menüden olacak.

- [ ] **Step 1: `UserDirectorySection.tsx`'i org-chart öncesi haline döndür**

`frontend/src/components/admin/directory/UserDirectorySection.tsx` dosyasının tamamını şununla değiştir:

```tsx
import { useState } from 'react';
import { DirectoryForm } from './DirectoryForm';
import { DirectoryList } from './DirectoryList';
import { DirectoryUserCard } from './DirectoryUserCard';
import { DirectoryUserList } from './DirectoryUserList';
import type { DirectoryDto } from '../../../api/types';

type View =
  | { kind: 'list' }
  | { kind: 'form'; directory: DirectoryDto | null }
  | { kind: 'users'; directory: DirectoryDto }
  | { kind: 'userDetail'; directory: DirectoryDto; userId: string };

/** Uygulamada router yok; bu bölümün alt ekranları yerel görünüm durumuyla yönetilir. */
export function UserDirectorySection() {
  const [view, setView] = useState<View>({ kind: 'list' });

  if (view.kind === 'form') {
    return <DirectoryForm directory={view.directory} onClose={() => setView({ kind: 'list' })} />;
  }

  if (view.kind === 'users') {
    return (
      <DirectoryUserList
        directory={view.directory}
        onBack={() => setView({ kind: 'list' })}
        onSelectUser={(userId) =>
          setView({ kind: 'userDetail', directory: view.directory, userId })
        }
      />
    );
  }

  if (view.kind === 'userDetail') {
    return (
      <DirectoryUserCard
        userId={view.userId}
        onBack={() => setView({ kind: 'users', directory: view.directory })}
        onSelectUser={(userId) => setView({ kind: 'userDetail', directory: view.directory, userId })}
      />
    );
  }

  return (
    <DirectoryList
      onAdd={() => setView({ kind: 'form', directory: null })}
      onEdit={(directory) => setView({ kind: 'form', directory })}
      onViewUsers={(directory) => setView({ kind: 'users', directory })}
    />
  );
}
```

- [ ] **Step 2: `DirectoryUserList.tsx`'ten "Organizasyon Şeması" linkini kaldır**

`frontend/src/components/admin/directory/DirectoryUserList.tsx` dosyasında satır 5-19'u bul:

```tsx
interface DirectoryUserListProps {
  directory: DirectoryDto;
  onBack: () => void;
  onSelectUser: (userId: string) => void;
  onViewOrgChart: () => void;
}

const PAGE_SIZE_OPTIONS = [25, 50, 100];

export function DirectoryUserList({
  directory,
  onBack,
  onSelectUser,
  onViewOrgChart,
}: DirectoryUserListProps) {
```

Şununla değiştir:

```tsx
interface DirectoryUserListProps {
  directory: DirectoryDto;
  onBack: () => void;
  onSelectUser: (userId: string) => void;
}

const PAGE_SIZE_OPTIONS = [25, 50, 100];

export function DirectoryUserList({ directory, onBack, onSelectUser }: DirectoryUserListProps) {
```

Aynı dosyada satır 30-48'i bul:

```tsx
        <h2 className="text-base font-semibold text-slate-800">{directory.name} — Kullanıcılar</h2>
        <div className="flex items-center gap-4">
          <button
            type="button"
            onClick={onViewOrgChart}
            className="text-sm text-indigo-600 hover:underline"
          >
            Organizasyon Şeması
          </button>
          <button
            type="button"
            onClick={onBack}
            className="text-sm text-slate-500 hover:text-slate-700"
          >
            ← Listeye dön
          </button>
        </div>
      </div>
```

Şununla değiştir:

```tsx
        <h2 className="text-base font-semibold text-slate-800">{directory.name} — Kullanıcılar</h2>
        <button
          type="button"
          onClick={onBack}
          className="text-sm text-slate-500 hover:text-slate-700"
        >
          ← Listeye dön
        </button>
      </div>
```

- [ ] **Step 3: Tip kontrolü**

Run: `cd frontend && npx tsc --noEmit`
Expected: Hata yok.

- [ ] **Step 4: Commit**

```bash
git add frontend/src/components/admin/directory/UserDirectorySection.tsx frontend/src/components/admin/directory/DirectoryUserList.tsx
git commit -m "refactor: remove org chart entry point from user directory flow"
```

---

### Task 3: `OrgChart.tsx`'i dikey/girintili, daraltılabilir ağaca yeniden yaz

**Files:**
- Modify: `frontend/src/components/admin/directory/OrgChart.tsx` (tamamen)

**Interfaces:**
- Consumes: `useOrgChart` (`frontend/src/hooks/useDirectories.ts`, imza değişmedi:
  `useOrgChart(directoryId: string | null)`), `OrgChartNodeDto` (`frontend/src/api/types.ts`,
  değişmedi: `{ id, username, displayName, managerId, photoBase64 }`).
- Produces: `OrgChart` bileşeni artık `{ directoryId: string; onSelectUser: (userId: string) => void }`
  prop'ları alır (eski `{ directory: DirectoryDto; onBack: () => void; onSelectUser }` yerine).
  Task 5 (`OrgChartSection.tsx`) bu yeni imzayı kullanacak.

- [ ] **Step 1: Dosyanın tamamını yeni implementasyonla değiştir**

`frontend/src/components/admin/directory/OrgChart.tsx` dosyasının tamamını şununla değiştir:

```tsx
import { useMemo, useState } from 'react';
import { useOrgChart } from '../../../hooks/useDirectories';
import type { OrgChartNodeDto } from '../../../api/types';

interface OrgChartProps {
  directoryId: string;
  onSelectUser: (userId: string) => void;
}

interface TreeNode extends OrgChartNodeDto {
  children: TreeNode[];
}

/** managerId null olan veya yöneticisi bu dizinin senkron kapsamında olmayan kullanıcılar köktür. */
function buildForest(nodes: OrgChartNodeDto[]): TreeNode[] {
  const byId = new Map<string, TreeNode>(nodes.map((n) => [n.id, { ...n, children: [] }]));
  const roots: TreeNode[] = [];

  for (const node of byId.values()) {
    const manager = node.managerId ? byId.get(node.managerId) : undefined;
    if (manager && manager.id !== node.id) {
      manager.children.push(node);
    } else {
      roots.push(node);
    }
  }

  return roots;
}

function Avatar({ node }: { node: OrgChartNodeDto }) {
  if (node.photoBase64) {
    return (
      <img
        src={`data:image/jpeg;base64,${node.photoBase64}`}
        alt=""
        className="h-8 w-8 shrink-0 rounded-full object-cover ring-1 ring-slate-200"
      />
    );
  }
  return (
    <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-slate-100 text-xs font-semibold text-slate-400 ring-1 ring-slate-200">
      {node.displayName.charAt(0).toUpperCase()}
    </div>
  );
}

function OrgChartRow({
  node,
  depth,
  collapsed,
  onToggleCollapse,
  onSelectUser,
}: {
  node: TreeNode;
  depth: number;
  collapsed: Set<string>;
  onToggleCollapse: (nodeId: string) => void;
  onSelectUser: (userId: string) => void;
}) {
  const hasChildren = node.children.length > 0;
  const isCollapsed = collapsed.has(node.id);

  return (
    <li>
      <div
        className={
          'flex items-center gap-2 py-1.5' + (depth > 0 ? ' border-l border-slate-200' : '')
        }
        style={{ paddingLeft: `${depth * 1.5}rem` }}
      >
        {hasChildren ? (
          <button
            type="button"
            onClick={() => onToggleCollapse(node.id)}
            className="flex h-5 w-5 shrink-0 items-center justify-center text-slate-400 hover:text-slate-600"
            aria-label={isCollapsed ? 'Dalı genişlet' : 'Dalı daralt'}
          >
            {isCollapsed ? '▸' : '▾'}
          </button>
        ) : (
          <span className="w-5 shrink-0" />
        )}

        <button
          type="button"
          onClick={() => onSelectUser(node.id)}
          className="flex min-w-0 flex-1 items-center gap-2 rounded-md px-2 py-1 text-left hover:bg-slate-50"
        >
          <Avatar node={node} />
          <span className="min-w-0">
            <span className="block truncate text-sm font-medium text-slate-800">
              {node.displayName}
            </span>
            <span className="block truncate text-xs text-slate-400">{node.username}</span>
          </span>
        </button>
      </div>

      {hasChildren && !isCollapsed && (
        <ul>
          {node.children.map((child) => (
            <OrgChartRow
              key={child.id}
              node={child}
              depth={depth + 1}
              collapsed={collapsed}
              onToggleCollapse={onToggleCollapse}
              onSelectUser={onSelectUser}
            />
          ))}
        </ul>
      )}
    </li>
  );
}

export function OrgChart({ directoryId, onSelectUser }: OrgChartProps) {
  const orgChart = useOrgChart(directoryId);
  const forest = useMemo(() => buildForest(orgChart.data?.nodes ?? []), [orgChart.data]);
  const [collapsed, setCollapsed] = useState<Set<string>>(new Set());

  const toggleCollapse = (nodeId: string) => {
    setCollapsed((prev) => {
      const next = new Set(prev);
      if (next.has(nodeId)) {
        next.delete(nodeId);
      } else {
        next.add(nodeId);
      }
      return next;
    });
  };

  if (orgChart.isLoading) {
    return <div className="py-8 text-center text-sm text-slate-400">Yükleniyor…</div>;
  }

  if (!orgChart.data?.hasManagerMapping) {
    return (
      <div className="rounded-xl border border-dashed border-slate-200 py-12 text-center text-sm text-slate-500">
        Organizasyon şeması çıkarılamıyor. Alan Eşlemeleri bölümünden "Kullanıcı" tipinde bir
        Yönetici alanı tanımlayıp dizini yeniden senkronize edin.
      </div>
    );
  }

  if (forest.length === 0) {
    return (
      <div className="rounded-xl border border-dashed border-slate-200 py-12 text-center text-sm text-slate-500">
        Bu dizinde henüz kullanıcı yok.
      </div>
    );
  }

  return (
    <div>
      <ul>
        {forest.map((root) => (
          <OrgChartRow
            key={root.id}
            node={root}
            depth={0}
            collapsed={collapsed}
            onToggleCollapse={toggleCollapse}
            onSelectUser={onSelectUser}
          />
        ))}
      </ul>

      {forest.length > 1 && (
        <p className="mt-4 text-xs text-slate-400">
          Birden fazla kök görünüyor — bazı yöneticiler bu dizinin senkronizasyon filtresi dışında
          kalmış olabilir.
        </p>
      )}
    </div>
  );
}
```

- [ ] **Step 2: Tip kontrolü**

Run: `cd frontend && npx tsc --noEmit`
Expected: Hata yok. (`OrgChart.tsx` şu an hiçbir yerden import edilmiyor — bu normal, Task 5'te bağlanacak.)

- [ ] **Step 3: Commit**

```bash
git add frontend/src/components/admin/directory/OrgChart.tsx
git commit -m "refactor: rewrite org chart as vertical collapsible tree"
```

---

### Task 4: `DirectoryUserCardModal.tsx` oluştur

**Files:**
- Create: `frontend/src/components/admin/directory/DirectoryUserCardModal.tsx`

**Interfaces:**
- Consumes: `DirectoryUserCard` (`frontend/src/components/admin/directory/DirectoryUserCard.tsx`,
  Task 1 sonrası `onBack` opsiyonel).
- Produces: `DirectoryUserCardModal` bileşeni, prop'lar:
  `{ userId: string; onClose: () => void; onSelectUser: (userId: string) => void }`. Task 5
  (`OrgChartSection.tsx`) bunu kullanacak.

- [ ] **Step 1: Dosyayı oluştur**

`frontend/src/components/admin/directory/DirectoryUserCardModal.tsx`:

```tsx
import { DirectoryUserCard } from './DirectoryUserCard';

interface DirectoryUserCardModalProps {
  userId: string;
  onClose: () => void;
  onSelectUser: (userId: string) => void;
}

/**
 * Organizasyon şemasındaki bir düğüme tıklanınca kullanıcı kartını sayfadan ayrılmadan gösterir.
 * Modal içindeki "Yönetici" referansına tıklanınca `onSelectUser` ile içerik değişir, modal kapanmaz.
 */
export function DirectoryUserCardModal({ userId, onClose, onSelectUser }: DirectoryUserCardModalProps) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/40 p-4">
      <div className="max-h-[90vh] w-full max-w-lg overflow-y-auto rounded-xl bg-white p-6 shadow-xl">
        <div className="mb-2 flex justify-end">
          <button
            type="button"
            onClick={onClose}
            className="text-slate-400 hover:text-slate-600"
            aria-label="Kapat"
          >
            ✕
          </button>
        </div>
        <DirectoryUserCard userId={userId} onSelectUser={onSelectUser} />
      </div>
    </div>
  );
}
```

- [ ] **Step 2: Tip kontrolü**

Run: `cd frontend && npx tsc --noEmit`
Expected: Hata yok.

- [ ] **Step 3: Commit**

```bash
git add frontend/src/components/admin/directory/DirectoryUserCardModal.tsx
git commit -m "feat: add DirectoryUserCardModal for org chart node clicks"
```

---

### Task 5: `OrgChartSection.tsx` oluştur (dizin seçici + ağaç + modal bağlantısı)

**Files:**
- Create: `frontend/src/components/admin/directory/OrgChartSection.tsx`

**Interfaces:**
- Consumes: `useDirectories` (`frontend/src/hooks/useDirectories.ts`, döner
  `UseQueryResult<PagedResult<DirectoryDto>>`, `data.items: DirectoryDto[]`, her biri
  `source: number` alanına sahip — `1` Active Directory demek), `OrgChart` (Task 3,
  `{ directoryId, onSelectUser }`), `DirectoryUserCardModal` (Task 4).
- Produces: `OrgChartSection` bileşeni, prop almaz. Task 6 (`AdminPage.tsx`) bunu render edecek.

- [ ] **Step 1: Dosyayı oluştur**

`frontend/src/components/admin/directory/OrgChartSection.tsx`:

```tsx
import { useState } from 'react';
import { useDirectories } from '../../../hooks/useDirectories';
import { OrgChart } from './OrgChart';
import { DirectoryUserCardModal } from './DirectoryUserCardModal';

const ACTIVE_DIRECTORY_SOURCE = 1;

export function OrgChartSection() {
  const directories = useDirectories();
  const adDirectories = (directories.data?.items ?? []).filter(
    (d) => d.source === ACTIVE_DIRECTORY_SOURCE,
  );
  const [selectedDirectoryId, setSelectedDirectoryId] = useState<string | null>(null);
  const [selectedUserId, setSelectedUserId] = useState<string | null>(null);
  const effectiveDirectoryId = selectedDirectoryId ?? adDirectories[0]?.id ?? null;

  if (directories.isLoading) {
    return <div className="py-8 text-center text-sm text-slate-400">Yükleniyor…</div>;
  }

  if (adDirectories.length === 0) {
    return (
      <div className="rounded-xl border border-dashed border-slate-200 py-12 text-center text-sm text-slate-500">
        Önce Kullanıcı Klasörü'nden bir Active Directory dizini tanımlayın.
      </div>
    );
  }

  return (
    <div>
      <label className="mb-4 block max-w-sm">
        <span className="mb-1 block text-xs font-medium text-slate-600">Dizin</span>
        <select
          value={effectiveDirectoryId ?? ''}
          onChange={(e) => setSelectedDirectoryId(e.target.value)}
          className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500"
        >
          {adDirectories.map((directory) => (
            <option key={directory.id} value={directory.id}>
              {directory.name}
            </option>
          ))}
        </select>
      </label>

      {effectiveDirectoryId && (
        <OrgChart
          key={effectiveDirectoryId}
          directoryId={effectiveDirectoryId}
          onSelectUser={setSelectedUserId}
        />
      )}

      {selectedUserId && (
        <DirectoryUserCardModal
          userId={selectedUserId}
          onClose={() => setSelectedUserId(null)}
          onSelectUser={setSelectedUserId}
        />
      )}
    </div>
  );
}
```

- [ ] **Step 2: Tip kontrolü**

Run: `cd frontend && npx tsc --noEmit`
Expected: Hata yok. (`OrgChartSection` şu an hiçbir yerden import edilmiyor — normal, Task 6'da bağlanacak.)

- [ ] **Step 3: Commit**

```bash
git add frontend/src/components/admin/directory/OrgChartSection.tsx
git commit -m "feat: add OrgChartSection with directory picker and user modal"
```

---

### Task 6: `OrgChartSection`'ı Ayarlar sayfasının sol menüsüne bağla

**Files:**
- Modify: `frontend/src/pages/AdminPage.tsx:1-19` (import + `SectionKind`),
  `frontend/src/pages/AdminPage.tsx:52-66` (`users` tab bölümleri),
  `frontend/src/pages/AdminPage.tsx:257-278` (`SectionContent`)

**Interfaces:**
- Consumes: `OrgChartSection` (Task 5, prop almaz).

- [ ] **Step 1: Import ekle**

`frontend/src/pages/AdminPage.tsx` dosyasının en üstündeki import bloğuna, mevcut
`AttributeMappingsSection` importundan hemen sonra ekle:

```tsx
import { AttributeMappingsSection } from '../components/admin/directory/AttributeMappingsSection';
import { OrgChartSection } from '../components/admin/directory/OrgChartSection';
```

- [ ] **Step 2: `SectionKind` union'a `'orgChart'` ekle**

Dosyada şunu bul:

```ts
type SectionKind =
  | 'employees'
  | 'notifications'
  | 'valueStreams'
  | 'activities'
  | 'holidays'
  | 'workCalendars'
  | 'userDirectory'
  | 'attributeMappings'
  | 'placeholder';
```

Şununla değiştir:

```ts
type SectionKind =
  | 'employees'
  | 'notifications'
  | 'valueStreams'
  | 'activities'
  | 'holidays'
  | 'workCalendars'
  | 'userDirectory'
  | 'attributeMappings'
  | 'orgChart'
  | 'placeholder';
```

- [ ] **Step 3: Sol menüye bölüm ekle**

`users` tab'ının `KULLANICI YÖNETİMİ` grubunda şunu bul:

```ts
      {
        header: 'KULLANICI YÖNETİMİ',
        sections: [
          { key: 'employees', label: 'Çalışanlar', kind: 'employees' },
          { key: 'userDirectory', label: 'Kullanıcı Klasörü', kind: 'userDirectory' },
          { key: 'attributeMappings', label: 'Alan Eşlemeleri', kind: 'attributeMappings' },
          { key: 'roles', label: 'Roller ve İzinler', kind: 'placeholder' },
        ],
      },
```

Şununla değiştir:

```ts
      {
        header: 'KULLANICI YÖNETİMİ',
        sections: [
          { key: 'employees', label: 'Çalışanlar', kind: 'employees' },
          { key: 'userDirectory', label: 'Kullanıcı Klasörü', kind: 'userDirectory' },
          { key: 'attributeMappings', label: 'Alan Eşlemeleri', kind: 'attributeMappings' },
          { key: 'orgChart', label: 'Organizasyon Şeması', kind: 'orgChart' },
          { key: 'roles', label: 'Roller ve İzinler', kind: 'placeholder' },
        ],
      },
```

- [ ] **Step 4: `SectionContent`'e case ekle**

Şunu bul:

```tsx
    case 'attributeMappings':
      return <AttributeMappingsSection />;
    case 'placeholder':
      return <Placeholder label={section.label} />;
```

Şununla değiştir:

```tsx
    case 'attributeMappings':
      return <AttributeMappingsSection />;
    case 'orgChart':
      return <OrgChartSection />;
    case 'placeholder':
      return <Placeholder label={section.label} />;
```

- [ ] **Step 5: Tip kontrolü**

Run: `cd frontend && npx tsc --noEmit`
Expected: Hata yok.

- [ ] **Step 6: Commit**

```bash
git add frontend/src/pages/AdminPage.tsx
git commit -m "feat: surface org chart as its own admin sidebar section"
```

---

### Task 7: Uçtan uca doğrulama

**Files:** Yok (yalnızca doğrulama).

- [ ] **Step 1: Backend'i başlat**

Run: `cd backend/src/EforTakip.Api && dotnet run`
Expected: `Now listening on: http://localhost:5298`

- [ ] **Step 2: Frontend'i başlat**

Run: `cd frontend && npm run dev`
Expected: `Local: http://localhost:5173/`

- [ ] **Step 3: Tarayıcıda admin` / `Admin123!` ile giriş yap, Ayarlar (⚙️) → Kullanıcı Yönetimi'ne git**

Expected: Sol menüde "Çalışanlar", "Kullanıcı Klasörü", "Alan Eşlemeleri", **"Organizasyon
Şeması"**, "Roller ve İzinler" sırasıyla görünür.

- [ ] **Step 4: "Organizasyon Şeması"na tıkla**

Expected: Üstte bir "Dizin" açılır listesi, altında seçili dizinin (varsa) ağacı görünür. AD
dizini yoksa "Önce Kullanıcı Klasörü'nden bir Active Directory dizini tanımlayın." mesajı çıkar.

- [ ] **Step 5: Bir AD dizini seçili durumdayken ağacı incele**

Expected: Kutular değil, girintili/dikey bir liste — sayfa yana doğru KAYMIYOR (tarayıcı
penceresini daraltıp genişleterek de kontrol et). Alt çalışanı olan satırlarda ▾ oku var,
tıklanınca dal daralıyor (▸ oluyor) ve tekrar tıklanınca açılıyor.

- [ ] **Step 6: Bir satıra (ok hariç) tıkla**

Expected: Ortada bir modal açılır, kullanıcı kartı (fotoğraf/avatar, hesap bilgileri, dizin
alanları) görünür. Kartta "Yönetici" gibi bir referans satırı varsa tıklanınca modal içeriği o
kişiye güncellenir, modal kapanmaz. Sağ üstteki ✕ ile modal kapanır, şema görünür kalır.

- [ ] **Step 7: Dizin seçiciden başka bir AD dizini seç (birden fazla varsa)**

Expected: Ağaç, seçilen yeni dizinin verisiyle yeniden yüklenir.

- [ ] **Step 8: Kullanıcı Klasörü akışını kontrol et (regresyon)**

`Kullanıcı Klasörü → [Dizin] → Kullanıcılar`a git.

Expected: Artık "Organizasyon Şeması" linki YOK (kaldırıldı). Bir kullanıcıya tıklayınca kart
sayfa içinde (modal değil, eskisi gibi) açılıyor ve "← Kullanıcılara dön" butonu çalışıyor.

- [ ] **Step 9: Backend ve frontend süreçlerini durdur**

Run (PowerShell): `Get-Process -Name 'EforTakip.Api' -ErrorAction SilentlyContinue | Stop-Process -Force`

Frontend dev server'ı çalıştırdığın terminalde Ctrl+C ile durdur.
