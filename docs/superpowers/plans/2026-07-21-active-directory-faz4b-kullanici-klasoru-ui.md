# Active Directory Entegrasyonu — Faz 4b: Kullanıcı Klasörü Arayüzü Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Yöneticinin dizinleri, alan eşlemelerini ve senkronize edilen kullanıcıları curl yerine arayüzden yönetebilmesi.

**Architecture:** Ayarlar → Kullanıcı Yönetimi altına iki yeni bölüm eklenir: **Kullanıcı Klasörü** (dizinler ve kullanıcıları) ve **Alan Eşlemeleri** (tüm dizinler için ortak). Uygulamada router olmadığından, Kullanıcı Klasörü bölümü kendi içinde yerel bir görünüm durumu (`liste → form → kullanıcılar → kullanıcı kartı`) ile çalışır. Dizin formu 5 bölüm ve ~20 alan içerdiğinden modal yerine panelin içinde tam genişlikte açılır.

**Tech Stack:** React 19, TypeScript, Tailwind 4, TanStack Query 5 — yeni bağımlılık yok.

## Global Constraints

- Faz 4a kuralları geçerli: yeni bağımlılık yok, Türkçe metinler, mevcut Tailwind/bileşen stiliyle uyum.
- Mutation'lar mevcut desende: `useMutation` + `queryClient.invalidateQueries`.
- Form hataları `ApiError.message` ile gösterilir (mevcut `ProjectFormModal` deseni).
- **Bind şifresi düzenleme formunda boş gelir** — boş bırakılırsa mevcut şifre korunur (backend böyle davranıyor). Alanın altında bu açıkça yazılır.
- `npm run build` ve `npm run lint` temiz geçmelidir.

## Enum karşılıkları (backend ile birebir)

| Enum | Değerler |
|------|----------|
| `DirectorySource` | 0 = Internal, 1 = ActiveDirectory |
| `DirectoryPermission` | 0 = ReadOnly, 1 = ReadOnlyLocalGroups, 2 = ReadWrite |
| `SyncScheduleKind` | 0 = Off, 1 = Hourly, 2 = Daily, 3 = Weekly |

---

## Dosya Yapısı

**API (`frontend/src/api/`):**
- `types.ts` — değişiklik: dizin tipleri
- `directories.ts` — dizin CRUD + sync + bağlantı testi
- `directoryUsers.ts` — kullanıcı listesi/detayı
- `directoryAttributeMappings.ts` — alan eşlemesi CRUD

**Hooks (`frontend/src/hooks/`):**
- `useDirectories.ts` — liste + detay
- `useDirectoryMutations.ts` — oluştur/güncelle/sil/senkronize/test
- `useDirectoryUsers.ts` — liste + detay
- `useAttributeMappings.ts` — liste + oluştur/güncelle/sil

**Bileşenler (`frontend/src/components/admin/directory/`):**
- `DirectoryList.tsx` — dizin tablosu ve aksiyonlar
- `DirectoryForm.tsx` — dizin ekle/düzenle formu (5 bölüm)
- `DirectoryUserList.tsx` — bir dizinin kullanıcıları
- `DirectoryUserCard.tsx` — kullanıcı kartı (tüm attribute'lar)
- `UserDirectorySection.tsx` — görünüm durumunu yöneten kapsayıcı
- `AttributeMappingsSection.tsx` — global alan eşlemeleri

**Sayfa:**
- `pages/AdminPage.tsx` — değişiklik: iki yeni bölüm

---

## Task 1: API katmanı ve tipler

**Files:**
- Modify: `frontend/src/api/types.ts`
- Create: `frontend/src/api/directories.ts`
- Create: `frontend/src/api/directoryUsers.ts`
- Create: `frontend/src/api/directoryAttributeMappings.ts`

**Interfaces:**
- Produces: `DirectoryDto`, `DirectoryUserDto`, `DirectoryUserDetailDto`, `DirectoryUserAttributeValueDto`, `DirectoryAttributeMappingDto`, `DirectorySyncResultDto`, `LdapConnectionTestResult`, `SaveDirectoryPayload`, `SaveAttributeMappingPayload` ve ilgili fonksiyonlar.

- [ ] **Step 1: Tipleri ekle**

`frontend/src/api/types.ts` — dosyanın sonuna ekle:
```typescript
export interface DirectoryDto {
  id: string;
  name: string;
  source: number;
  directoryType: string | null;
  hostname: string | null;
  port: number;
  useSsl: boolean;
  bindUsername: string | null;
  baseDn: string | null;
  additionalUserDn: string | null;
  additionalGroupDn: string | null;
  permission: number;
  userObjectClass: string | null;
  userObjectFilter: string | null;
  usernameAttribute: string | null;
  usernameRdnAttribute: string | null;
  firstNameAttribute: string | null;
  lastNameAttribute: string | null;
  displayNameAttribute: string | null;
  emailAttribute: string | null;
  uniqueIdAttribute: string | null;
  syncSchedule: number;
  isActive: boolean;
  sortOrder: number;
  lastSyncedUtc: string | null;
}

export interface DirectoryUserDto {
  id: string;
  directoryId: string;
  directoryName: string;
  source: number;
  username: string;
  firstName: string | null;
  lastName: string | null;
  displayName: string | null;
  email: string | null;
  isActive: boolean;
  lastSyncedUtc: string | null;
}

export interface DirectoryUserAttributeValueDto {
  systemFieldName: string;
  adAttributeName: string;
  fieldType: string;
  value: string | null;
}

export interface DirectoryUserDetailDto extends DirectoryUserDto {
  attributes: DirectoryUserAttributeValueDto[];
}

export interface DirectoryAttributeMappingDto {
  id: string;
  adAttributeName: string;
  systemFieldName: string;
  fieldType: string;
  isSynced: boolean;
  sortOrder: number;
}

export interface DirectorySyncResultDto {
  directoryId: string;
  directoryName: string;
  added: number;
  updated: number;
  deactivated: number;
  totalFromDirectory: number;
  syncedAtUtc: string;
}

export interface LdapConnectionTestResult {
  success: boolean;
  message: string;
}
```

**Not:** `DirectoryDto.lastSyncedUtc` backend'de Faz 2'de eklendi; DTO'da mevcut.

- [ ] **Step 2: directories API'sini yaz**

`frontend/src/api/directories.ts`:
```typescript
import { apiClient } from './client';
import type {
  DirectoryDto,
  DirectorySyncResultDto,
  LdapConnectionTestResult,
  PagedResult,
} from './types';

export interface SaveDirectoryPayload {
  name: string;
  source: number;
  directoryType?: string | null;
  hostname?: string | null;
  port: number;
  useSsl: boolean;
  bindUsername?: string | null;
  /** Boş bırakılırsa güncellemede mevcut şifre korunur. */
  bindPassword?: string | null;
  baseDn?: string | null;
  additionalUserDn?: string | null;
  additionalGroupDn?: string | null;
  permission: number;
  userObjectClass?: string | null;
  userObjectFilter?: string | null;
  usernameAttribute?: string | null;
  usernameRdnAttribute?: string | null;
  firstNameAttribute?: string | null;
  lastNameAttribute?: string | null;
  displayNameAttribute?: string | null;
  emailAttribute?: string | null;
  uniqueIdAttribute?: string | null;
  syncSchedule: number;
  sortOrder: number;
}

export function getDirectories() {
  return apiClient.get<PagedResult<DirectoryDto>>('/api/v1/directories', { pageSize: 100 });
}

export function getDirectoryById(id: string) {
  return apiClient.get<DirectoryDto>(`/api/v1/directories/${id}`);
}

export function createDirectory(payload: SaveDirectoryPayload) {
  return apiClient.post<void>('/api/v1/directories', payload);
}

export function updateDirectory(id: string, payload: SaveDirectoryPayload) {
  return apiClient.put<void>(`/api/v1/directories/${id}`, { ...payload, id });
}

export function deleteDirectory(id: string) {
  return apiClient.delete<void>(`/api/v1/directories/${id}`);
}

export function syncDirectory(id: string) {
  return apiClient.post<DirectorySyncResultDto>(`/api/v1/directories/${id}/sync`);
}

export function testDirectoryConnection(id: string) {
  return apiClient.post<LdapConnectionTestResult>(`/api/v1/directories/${id}/test-connection`);
}
```

- [ ] **Step 3: directoryUsers ve attributeMappings API'lerini yaz**

`frontend/src/api/directoryUsers.ts`:
```typescript
import { apiClient } from './client';
import type { DirectoryUserDetailDto, DirectoryUserDto, PagedResult } from './types';

export function getDirectoryUsers(options?: {
  directoryId?: string;
  searchTerm?: string;
  onlyActive?: boolean;
  pageSize?: number;
}) {
  return apiClient.get<PagedResult<DirectoryUserDto>>('/api/v1/directoryusers', {
    directoryId: options?.directoryId,
    searchTerm: options?.searchTerm,
    onlyActive: options?.onlyActive,
    pageSize: options?.pageSize ?? 100,
  });
}

export function getDirectoryUserById(id: string) {
  return apiClient.get<DirectoryUserDetailDto>(`/api/v1/directoryusers/${id}`);
}

export interface CreateInternalUserPayload {
  directoryId: string;
  username: string;
  password: string;
  firstName?: string | null;
  lastName?: string | null;
  displayName?: string | null;
  email?: string | null;
}

export function createInternalUser(payload: CreateInternalUserPayload) {
  return apiClient.post<void>('/api/v1/directoryusers/internal', payload);
}
```

`frontend/src/api/directoryAttributeMappings.ts`:
```typescript
import { apiClient } from './client';
import type { DirectoryAttributeMappingDto } from './types';

export interface SaveAttributeMappingPayload {
  adAttributeName: string;
  systemFieldName: string;
  fieldType: string;
  isSynced: boolean;
  sortOrder: number;
}

export function getAttributeMappings() {
  return apiClient.get<DirectoryAttributeMappingDto[]>('/api/v1/directoryattributemappings');
}

export function createAttributeMapping(payload: SaveAttributeMappingPayload) {
  return apiClient.post<{ id: string }>('/api/v1/directoryattributemappings', payload);
}

export function updateAttributeMapping(id: string, payload: SaveAttributeMappingPayload) {
  return apiClient.put<void>(`/api/v1/directoryattributemappings/${id}`, { ...payload, id });
}

export function deleteAttributeMapping(id: string) {
  return apiClient.delete<void>(`/api/v1/directoryattributemappings/${id}`);
}
```

- [ ] **Step 4: Derleme kontrolü**

Run: `cd frontend && npx tsc -b --noEmit 2>&1 | head -20`
Expected: Hata yok.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/api/
git commit -m "feat: add directory api client functions and types"
```

---

## Task 2: Hook'lar

**Files:**
- Create: `frontend/src/hooks/useDirectories.ts`
- Create: `frontend/src/hooks/useDirectoryMutations.ts`
- Create: `frontend/src/hooks/useDirectoryUsers.ts`
- Create: `frontend/src/hooks/useAttributeMappings.ts`

**Interfaces:**
- Produces: `useDirectories()`, `useDirectory(id)`, `useCreateDirectoryMutation()`, `useUpdateDirectoryMutation()`, `useDeleteDirectoryMutation()`, `useSyncDirectoryMutation()`, `useTestDirectoryConnectionMutation()`, `useDirectoryUsers(options)`, `useDirectoryUser(id)`, `useAttributeMappings()`, `useCreateAttributeMappingMutation()`, `useUpdateAttributeMappingMutation()`, `useDeleteAttributeMappingMutation()`

- [ ] **Step 1: Sorgu hook'larını yaz**

`frontend/src/hooks/useDirectories.ts`:
```typescript
import { useQuery } from '@tanstack/react-query';
import { getDirectories, getDirectoryById } from '../api/directories';

export function useDirectories() {
  return useQuery({ queryKey: ['directories'], queryFn: getDirectories });
}

export function useDirectory(id: string | null) {
  return useQuery({
    queryKey: ['directories', id],
    queryFn: () => getDirectoryById(id!),
    enabled: id !== null,
  });
}
```

`frontend/src/hooks/useDirectoryUsers.ts`:
```typescript
import { useQuery } from '@tanstack/react-query';
import { getDirectoryUserById, getDirectoryUsers } from '../api/directoryUsers';

export function useDirectoryUsers(options: { directoryId?: string; searchTerm?: string }) {
  return useQuery({
    queryKey: ['directoryUsers', options.directoryId ?? null, options.searchTerm ?? ''],
    queryFn: () => getDirectoryUsers(options),
  });
}

export function useDirectoryUser(id: string | null) {
  return useQuery({
    queryKey: ['directoryUsers', 'detail', id],
    queryFn: () => getDirectoryUserById(id!),
    enabled: id !== null,
  });
}
```

- [ ] **Step 2: Dizin mutation'larını yaz**

`frontend/src/hooks/useDirectoryMutations.ts`:
```typescript
import { useMutation, useQueryClient } from '@tanstack/react-query';
import {
  createDirectory,
  deleteDirectory,
  syncDirectory,
  testDirectoryConnection,
  updateDirectory,
  type SaveDirectoryPayload,
} from '../api/directories';

export function useCreateDirectoryMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: createDirectory,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['directories'] }),
  });
}

export function useUpdateDirectoryMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: SaveDirectoryPayload }) =>
      updateDirectory(id, payload),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['directories'] }),
  });
}

export function useDeleteDirectoryMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: deleteDirectory,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['directories'] }),
  });
}

export function useSyncDirectoryMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: syncDirectory,
    onSuccess: () => {
      // Senkronizasyon hem kullanıcıları hem dizinin son senkron zamanını değiştirir.
      queryClient.invalidateQueries({ queryKey: ['directories'] });
      queryClient.invalidateQueries({ queryKey: ['directoryUsers'] });
    },
  });
}

export function useTestDirectoryConnectionMutation() {
  return useMutation({ mutationFn: testDirectoryConnection });
}
```

- [ ] **Step 3: Alan eşlemesi hook'larını yaz**

`frontend/src/hooks/useAttributeMappings.ts`:
```typescript
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  createAttributeMapping,
  deleteAttributeMapping,
  getAttributeMappings,
  updateAttributeMapping,
  type SaveAttributeMappingPayload,
} from '../api/directoryAttributeMappings';

const QUERY_KEY = ['directoryAttributeMappings'];

export function useAttributeMappings() {
  return useQuery({ queryKey: QUERY_KEY, queryFn: getAttributeMappings });
}

export function useCreateAttributeMappingMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: createAttributeMapping,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: QUERY_KEY }),
  });
}

export function useUpdateAttributeMappingMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: SaveAttributeMappingPayload }) =>
      updateAttributeMapping(id, payload),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: QUERY_KEY }),
  });
}

export function useDeleteAttributeMappingMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: deleteAttributeMapping,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: QUERY_KEY }),
  });
}
```

- [ ] **Step 4: Derleme kontrolü ve commit**

Run: `cd frontend && npx tsc -b --noEmit 2>&1 | head -20`
Expected: Hata yok.

```bash
git add frontend/src/hooks/
git commit -m "feat: add directory query and mutation hooks"
```

---

## Task 3: Dizin listesi

**Files:**
- Create: `frontend/src/components/admin/directory/DirectoryList.tsx`

**Interfaces:**
- Consumes: `useDirectories`, `useDeleteDirectoryMutation`, `useSyncDirectoryMutation`.
- Produces: `DirectoryList({ onAdd, onEdit, onViewUsers })`

**Davranış:** Dizinler tablo halinde listelenir. Her satırda Kullanıcılar / Senkronize Et / Düzenle / Sil aksiyonları. Senkronizasyon sonucu satır altında özet olarak gösterilir. Internal dizinlerde senkronizasyon aksiyonu gösterilmez (senkronize edilemezler).

- [ ] **Step 1: Bileşeni yaz**

`frontend/src/components/admin/directory/DirectoryList.tsx`:
```tsx
import { useState } from 'react';
import { ApiError } from '../../../api/client';
import { useDirectories } from '../../../hooks/useDirectories';
import {
  useDeleteDirectoryMutation,
  useSyncDirectoryMutation,
} from '../../../hooks/useDirectoryMutations';
import type { DirectoryDto } from '../../../api/types';

const SOURCE_LABEL: Record<number, string> = {
  0: 'Internal',
  1: 'Active Directory',
};

const SCHEDULE_LABEL: Record<number, string> = {
  0: 'Kapalı',
  1: 'Saatlik',
  2: 'Günlük',
  3: 'Haftalık',
};

interface DirectoryListProps {
  onAdd: () => void;
  onEdit: (directory: DirectoryDto) => void;
  onViewUsers: (directory: DirectoryDto) => void;
}

function formatLastSynced(value: string | null): string {
  if (!value) return 'Hiç senkronize edilmedi';
  return new Date(value).toLocaleString('tr-TR');
}

export function DirectoryList({ onAdd, onEdit, onViewUsers }: DirectoryListProps) {
  const directories = useDirectories();
  const syncMutation = useSyncDirectoryMutation();
  const deleteMutation = useDeleteDirectoryMutation();
  const [message, setMessage] = useState<{ text: string; isError: boolean } | null>(null);
  // Hangi dizinin senkronize edildiğini tutar; aksi halde tek bir mutation'ın isPending'i
  // tüm satırları aynı anda "senkronize ediliyor" gösterirdi.
  const [syncingId, setSyncingId] = useState<string | null>(null);

  const handleSync = async (directory: DirectoryDto) => {
    setMessage(null);
    setSyncingId(directory.id);
    try {
      const result = await syncMutation.mutateAsync(directory.id);
      setMessage({
        text: `${result.directoryName}: ${result.added} eklendi, ${result.updated} güncellendi, ${result.deactivated} pasife alındı.`,
        isError: false,
      });
    } catch (error) {
      setMessage({
        text: error instanceof ApiError ? error.message : 'Senkronizasyon başarısız oldu.',
        isError: true,
      });
    } finally {
      setSyncingId(null);
    }
  };

  const handleDelete = async (directory: DirectoryDto) => {
    if (!window.confirm(`"${directory.name}" dizinini silmek istediğinize emin misiniz?`)) return;

    setMessage(null);
    try {
      await deleteMutation.mutateAsync(directory.id);
    } catch (error) {
      setMessage({
        text: error instanceof ApiError ? error.message : 'Dizin silinemedi.',
        isError: true,
      });
    }
  };

  const items = directories.data?.items ?? [];

  return (
    <div>
      <div className="mb-4 flex items-center justify-between">
        <p className="text-sm text-slate-500">
          Kullanıcıların çekileceği dizinler. Birden fazla dizin tanımlanabilir.
        </p>
        <button
          type="button"
          onClick={onAdd}
          className="rounded-lg bg-indigo-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-indigo-700"
        >
          Yeni Dizin Ekle
        </button>
      </div>

      {message && (
        <p
          role="status"
          className={
            'mb-4 rounded-md px-3 py-2 text-sm ' +
            (message.isError ? 'bg-rose-50 text-rose-700' : 'bg-emerald-50 text-emerald-700')
          }
        >
          {message.text}
        </p>
      )}

      {directories.isLoading ? (
        <div className="py-8 text-center text-sm text-slate-400">Yükleniyor…</div>
      ) : items.length === 0 ? (
        <div className="rounded-xl border border-dashed border-slate-200 py-12 text-center text-sm text-slate-500">
          Henüz dizin tanımlanmamış.
        </div>
      ) : (
        <table className="w-full text-left text-sm">
          <thead>
            <tr className="border-b border-slate-100 text-xs uppercase tracking-wide text-slate-400">
              <th className="py-2 pr-4 font-medium">Ad</th>
              <th className="py-2 pr-4 font-medium">Tip</th>
              <th className="py-2 pr-4 font-medium">Sunucu</th>
              <th className="py-2 pr-4 font-medium">Zamanlama</th>
              <th className="py-2 pr-4 font-medium">Son Senkron</th>
              <th className="py-2 font-medium">İşlemler</th>
            </tr>
          </thead>
          <tbody>
            {items.map((directory) => (
              <tr key={directory.id} className="border-b border-slate-50 last:border-0">
                <td className="py-2 pr-4 text-slate-700">
                  {directory.name}
                  {!directory.isActive && (
                    <span className="ml-2 rounded-full bg-slate-100 px-2 py-0.5 text-xs text-slate-500">
                      Pasif
                    </span>
                  )}
                </td>
                <td className="py-2 pr-4 text-slate-500">{SOURCE_LABEL[directory.source] ?? '—'}</td>
                <td className="py-2 pr-4 text-slate-500">
                  {directory.hostname ? `${directory.hostname}:${directory.port}` : '—'}
                </td>
                <td className="py-2 pr-4 text-slate-500">
                  {SCHEDULE_LABEL[directory.syncSchedule] ?? '—'}
                </td>
                <td className="py-2 pr-4 text-slate-500">
                  {directory.source === 1 ? formatLastSynced(directory.lastSyncedUtc) : '—'}
                </td>
                <td className="py-2">
                  <div className="flex gap-2 text-xs">
                    <button
                      type="button"
                      onClick={() => onViewUsers(directory)}
                      className="text-indigo-600 hover:underline"
                    >
                      Kullanıcılar
                    </button>
                    {directory.source === 1 && (
                      <button
                        type="button"
                        onClick={() => handleSync(directory)}
                        disabled={syncingId !== null}
                        className="text-indigo-600 hover:underline disabled:text-slate-300"
                      >
                        {syncingId === directory.id ? 'Senkronize ediliyor…' : 'Senkronize Et'}
                      </button>
                    )}
                    <button
                      type="button"
                      onClick={() => onEdit(directory)}
                      className="text-slate-600 hover:underline"
                    >
                      Düzenle
                    </button>
                    <button
                      type="button"
                      onClick={() => handleDelete(directory)}
                      className="text-rose-600 hover:underline"
                    >
                      Sil
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}
```

- [ ] **Step 2: Derleme kontrolü ve commit**

Run: `cd frontend && npx tsc -b --noEmit 2>&1 | head -20`
Expected: Hata yok.

```bash
git add frontend/src/components/admin/directory/DirectoryList.tsx
git commit -m "feat: add directory list with sync and delete actions"
```

---

## Task 4: Dizin formu

**Files:**
- Create: `frontend/src/components/admin/directory/DirectoryForm.tsx`

**Interfaces:**
- Consumes: `useCreateDirectoryMutation`, `useUpdateDirectoryMutation`, `useTestDirectoryConnectionMutation`, `ApiError`.
- Produces: `DirectoryForm({ directory, onClose })` — `directory` null ise ekleme modu.

**Bölümler:** Server Settings · LDAP Schema · LDAP Permissions · User Schema Settings · Sync Schedule. Internal dizinlerde yalnızca ad alanı gösterilir (diğer alanların karşılığı yok).

- [ ] **Step 1: Bileşeni yaz**

`frontend/src/components/admin/directory/DirectoryForm.tsx`:
```tsx
import { useState, type FormEvent, type ReactNode } from 'react';
import { ApiError } from '../../../api/client';
import {
  useCreateDirectoryMutation,
  useTestDirectoryConnectionMutation,
  useUpdateDirectoryMutation,
} from '../../../hooks/useDirectoryMutations';
import type { DirectoryDto } from '../../../api/types';

interface DirectoryFormProps {
  directory: DirectoryDto | null;
  onClose: () => void;
}

function Section({ title, children }: { title: string; children: ReactNode }) {
  return (
    <section className="border-t border-slate-100 pt-5 first:border-0 first:pt-0">
      <h3 className="mb-3 text-sm font-semibold text-slate-800">{title}</h3>
      <div className="grid grid-cols-1 gap-4 md:grid-cols-2">{children}</div>
    </section>
  );
}

function Field({
  label,
  hint,
  children,
}: {
  label: string;
  hint?: string;
  children: ReactNode;
}) {
  return (
    <label className="block">
      <span className="mb-1 block text-sm font-medium text-slate-700">{label}</span>
      {children}
      {hint && <span className="mt-1 block text-xs text-slate-400">{hint}</span>}
    </label>
  );
}

const inputClass =
  'w-full rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500';

export function DirectoryForm({ directory, onClose }: DirectoryFormProps) {
  const isEdit = directory !== null;

  const [name, setName] = useState(directory?.name ?? '');
  const [source, setSource] = useState(directory?.source ?? 1);
  const [directoryType, setDirectoryType] = useState(
    directory?.directoryType ?? 'Microsoft Active Directory',
  );
  const [hostname, setHostname] = useState(directory?.hostname ?? '');
  const [port, setPort] = useState(String(directory?.port ?? 389));
  const [useSsl, setUseSsl] = useState(directory?.useSsl ?? false);
  const [bindUsername, setBindUsername] = useState(directory?.bindUsername ?? '');
  const [bindPassword, setBindPassword] = useState('');
  const [baseDn, setBaseDn] = useState(directory?.baseDn ?? '');
  const [additionalUserDn, setAdditionalUserDn] = useState(directory?.additionalUserDn ?? '');
  const [additionalGroupDn, setAdditionalGroupDn] = useState(directory?.additionalGroupDn ?? '');
  const [permission, setPermission] = useState(directory?.permission ?? 0);
  const [userObjectClass, setUserObjectClass] = useState(directory?.userObjectClass ?? 'user');
  const [userObjectFilter, setUserObjectFilter] = useState(
    directory?.userObjectFilter ?? '(&(objectCategory=Person)(sAMAccountName=*))',
  );
  const [usernameAttribute, setUsernameAttribute] = useState(
    directory?.usernameAttribute ?? 'sAMAccountName',
  );
  const [usernameRdnAttribute, setUsernameRdnAttribute] = useState(
    directory?.usernameRdnAttribute ?? 'cn',
  );
  const [firstNameAttribute, setFirstNameAttribute] = useState(
    directory?.firstNameAttribute ?? 'givenName',
  );
  const [lastNameAttribute, setLastNameAttribute] = useState(directory?.lastNameAttribute ?? 'sn');
  const [displayNameAttribute, setDisplayNameAttribute] = useState(
    directory?.displayNameAttribute ?? 'displayName',
  );
  const [emailAttribute, setEmailAttribute] = useState(directory?.emailAttribute ?? 'mail');
  const [uniqueIdAttribute, setUniqueIdAttribute] = useState(
    directory?.uniqueIdAttribute ?? 'objectGUID',
  );
  const [syncSchedule, setSyncSchedule] = useState(directory?.syncSchedule ?? 0);

  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [testResult, setTestResult] = useState<{ text: string; isError: boolean } | null>(null);

  const createMutation = useCreateDirectoryMutation();
  const updateMutation = useUpdateDirectoryMutation();
  const testMutation = useTestDirectoryConnectionMutation();

  const isPending = createMutation.isPending || updateMutation.isPending;
  const isActiveDirectory = source === 1;

  const buildPayload = () => ({
    name: name.trim(),
    source,
    directoryType: isActiveDirectory ? directoryType.trim() : null,
    hostname: isActiveDirectory ? hostname.trim() : null,
    port: isActiveDirectory ? Number(port) || 0 : 0,
    useSsl: isActiveDirectory ? useSsl : false,
    bindUsername: isActiveDirectory ? bindUsername.trim() : null,
    bindPassword: bindPassword.length > 0 ? bindPassword : null,
    baseDn: isActiveDirectory ? baseDn.trim() : null,
    additionalUserDn: isActiveDirectory ? additionalUserDn.trim() || null : null,
    additionalGroupDn: isActiveDirectory ? additionalGroupDn.trim() || null : null,
    permission,
    userObjectClass: isActiveDirectory ? userObjectClass.trim() : null,
    userObjectFilter: isActiveDirectory ? userObjectFilter.trim() : null,
    usernameAttribute: isActiveDirectory ? usernameAttribute.trim() : null,
    usernameRdnAttribute: isActiveDirectory ? usernameRdnAttribute.trim() : null,
    firstNameAttribute: isActiveDirectory ? firstNameAttribute.trim() : null,
    lastNameAttribute: isActiveDirectory ? lastNameAttribute.trim() : null,
    displayNameAttribute: isActiveDirectory ? displayNameAttribute.trim() : null,
    emailAttribute: isActiveDirectory ? emailAttribute.trim() : null,
    uniqueIdAttribute: isActiveDirectory ? uniqueIdAttribute.trim() : null,
    syncSchedule: isActiveDirectory ? syncSchedule : 0,
    sortOrder: directory?.sortOrder ?? 0,
  });

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault();
    setErrorMessage(null);

    try {
      if (isEdit && directory) {
        await updateMutation.mutateAsync({ id: directory.id, payload: buildPayload() });
      } else {
        await createMutation.mutateAsync(buildPayload());
      }
      onClose();
    } catch (error) {
      setErrorMessage(error instanceof ApiError ? error.message : 'Dizin kaydedilemedi.');
    }
  };

  const handleTestConnection = async () => {
    if (!directory) return;
    setTestResult(null);
    try {
      const result = await testMutation.mutateAsync(directory.id);
      setTestResult({ text: result.message, isError: !result.success });
    } catch (error) {
      setTestResult({
        text: error instanceof ApiError ? error.message : 'Bağlantı test edilemedi.',
        isError: true,
      });
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-5">
      <div className="flex items-center justify-between">
        <h2 className="text-base font-semibold text-slate-800">
          {isEdit ? 'Dizini Düzenle' : 'Yeni Dizin'}
        </h2>
        <button type="button" onClick={onClose} className="text-sm text-slate-500 hover:text-slate-700">
          ← Listeye dön
        </button>
      </div>

      <Section title="Sunucu Ayarları">
        <Field label="Ad">
          <input value={name} onChange={(e) => setName(e.target.value)} className={inputClass} />
        </Field>
        <Field label="Dizin Kaynağı">
          <select
            value={source}
            onChange={(e) => setSource(Number(e.target.value))}
            disabled={isEdit}
            className={inputClass}
          >
            <option value={1}>Active Directory</option>
            <option value={0}>Internal</option>
          </select>
        </Field>

        {isActiveDirectory && (
          <>
            <Field label="Dizin Tipi">
              <input
                value={directoryType}
                onChange={(e) => setDirectoryType(e.target.value)}
                className={inputClass}
              />
            </Field>
            <Field label="Sunucu Adresi" hint="LDAP sunucusunun adresi. Örnek: kizilay.local">
              <input
                value={hostname}
                onChange={(e) => setHostname(e.target.value)}
                className={inputClass}
              />
            </Field>
            <Field label="Port">
              <input
                type="number"
                value={port}
                onChange={(e) => setPort(e.target.value)}
                className={inputClass}
              />
            </Field>
            <Field label="SSL Kullan" hint="Üretimde şifrenin ağda düz metin gitmemesi için açık olmalıdır.">
              <label className="flex items-center gap-2 pt-2">
                <input
                  type="checkbox"
                  checked={useSsl}
                  onChange={(e) => setUseSsl(e.target.checked)}
                  className="h-4 w-4"
                />
                <span className="text-sm text-slate-600">SSL (LDAPS)</span>
              </label>
            </Field>
            <Field label="Bağlantı Kullanıcısı" hint="Örnek: servis_hesabi@kizilay.org.tr">
              <input
                value={bindUsername}
                onChange={(e) => setBindUsername(e.target.value)}
                className={inputClass}
              />
            </Field>
            <Field
              label="Bağlantı Şifresi"
              hint={isEdit ? 'Boş bırakılırsa mevcut şifre korunur.' : undefined}
            >
              <input
                type="password"
                value={bindPassword}
                onChange={(e) => setBindPassword(e.target.value)}
                autoComplete="new-password"
                className={inputClass}
              />
            </Field>
          </>
        )}
      </Section>

      {isActiveDirectory && (
        <>
          <Section title="LDAP Şeması">
            <Field label="Base DN" hint="Kullanıcı ve grupların arandığı kök düğüm.">
              <input value={baseDn} onChange={(e) => setBaseDn(e.target.value)} className={inputClass} />
            </Field>
            <Field label="Ek Kullanıcı DN" hint="Kullanıcı aramasını daraltmak için Base DN'in önüne eklenir.">
              <input
                value={additionalUserDn}
                onChange={(e) => setAdditionalUserDn(e.target.value)}
                className={inputClass}
              />
            </Field>
            <Field label="Ek Grup DN" hint="Grup aramasını daraltmak için Base DN'in önüne eklenir.">
              <input
                value={additionalGroupDn}
                onChange={(e) => setAdditionalGroupDn(e.target.value)}
                className={inputClass}
              />
            </Field>
          </Section>

          <section className="border-t border-slate-100 pt-5">
            <h3 className="mb-3 text-sm font-semibold text-slate-800">Dizin İzinleri</h3>
            <div className="space-y-2">
              {[
                { value: 0, label: 'Salt Okunur', hint: 'Kullanıcılar dizinden okunur, sistemde değiştirilemez.' },
                {
                  value: 1,
                  label: 'Salt Okunur, Yerel Gruplarla',
                  hint: 'Dizinden okunur; sistem içindeki gruplara eklenebilir.',
                },
                {
                  value: 2,
                  label: 'Okuma/Yazma',
                  hint: 'Sistemdeki değişiklikler dizine de yazılır. Bağlantı kullanıcısının yetkisi olmalıdır.',
                },
              ].map((option) => (
                <label key={option.value} className="flex items-start gap-2">
                  <input
                    type="radio"
                    name="permission"
                    checked={permission === option.value}
                    onChange={() => setPermission(option.value)}
                    className="mt-1 h-4 w-4"
                  />
                  <span>
                    <span className="block text-sm text-slate-700">{option.label}</span>
                    <span className="block text-xs text-slate-400">{option.hint}</span>
                  </span>
                </label>
              ))}
            </div>
          </section>

          <Section title="Kullanıcı Şeması Ayarları">
            <Field label="Kullanıcı Nesne Sınıfı">
              <input
                value={userObjectClass}
                onChange={(e) => setUserObjectClass(e.target.value)}
                className={inputClass}
              />
            </Field>
            <Field label="Kullanıcı Nesne Filtresi">
              <input
                value={userObjectFilter}
                onChange={(e) => setUserObjectFilter(e.target.value)}
                className={inputClass}
              />
            </Field>
            <Field label="Kullanıcı Adı Attribute">
              <input
                value={usernameAttribute}
                onChange={(e) => setUsernameAttribute(e.target.value)}
                className={inputClass}
              />
            </Field>
            <Field label="Kullanıcı Adı RDN Attribute">
              <input
                value={usernameRdnAttribute}
                onChange={(e) => setUsernameRdnAttribute(e.target.value)}
                className={inputClass}
              />
            </Field>
            <Field label="Ad Attribute">
              <input
                value={firstNameAttribute}
                onChange={(e) => setFirstNameAttribute(e.target.value)}
                className={inputClass}
              />
            </Field>
            <Field label="Soyad Attribute">
              <input
                value={lastNameAttribute}
                onChange={(e) => setLastNameAttribute(e.target.value)}
                className={inputClass}
              />
            </Field>
            <Field label="Görünen Ad Attribute">
              <input
                value={displayNameAttribute}
                onChange={(e) => setDisplayNameAttribute(e.target.value)}
                className={inputClass}
              />
            </Field>
            <Field label="E-posta Attribute">
              <input
                value={emailAttribute}
                onChange={(e) => setEmailAttribute(e.target.value)}
                className={inputClass}
              />
            </Field>
            <Field
              label="Benzersiz Kimlik Attribute"
              hint="Kullanıcı adı değişse bile kimliğin korunmasını sağlar."
            >
              <input
                value={uniqueIdAttribute}
                onChange={(e) => setUniqueIdAttribute(e.target.value)}
                className={inputClass}
              />
            </Field>
          </Section>

          <Section title="Senkronizasyon Zamanlaması">
            <Field label="Otomatik Senkronizasyon">
              <select
                value={syncSchedule}
                onChange={(e) => setSyncSchedule(Number(e.target.value))}
                className={inputClass}
              >
                <option value={0}>Kapalı</option>
                <option value={1}>Saatlik</option>
                <option value={2}>Günlük</option>
                <option value={3}>Haftalık</option>
              </select>
            </Field>
          </Section>
        </>
      )}

      {errorMessage && (
        <p role="alert" className="rounded-md bg-rose-50 px-3 py-2 text-sm text-rose-700">
          {errorMessage}
        </p>
      )}

      {testResult && (
        <p
          role="status"
          className={
            'rounded-md px-3 py-2 text-sm ' +
            (testResult.isError ? 'bg-rose-50 text-rose-700' : 'bg-emerald-50 text-emerald-700')
          }
        >
          {testResult.text}
        </p>
      )}

      <div className="flex items-center justify-between border-t border-slate-100 pt-4">
        <div>
          {isEdit && isActiveDirectory && (
            <button
              type="button"
              onClick={handleTestConnection}
              disabled={testMutation.isPending}
              className="rounded-lg border border-slate-200 px-4 py-2 text-sm font-medium text-slate-600 hover:bg-slate-50 disabled:text-slate-300"
            >
              {testMutation.isPending ? 'Test ediliyor…' : 'Bağlantıyı Test Et'}
            </button>
          )}
        </div>
        <div className="flex gap-2">
          <button
            type="button"
            onClick={onClose}
            className="rounded-lg border border-slate-200 px-4 py-2 text-sm font-medium text-slate-600 hover:bg-slate-50"
          >
            Vazgeç
          </button>
          <button
            type="submit"
            disabled={isPending || name.trim().length === 0}
            className="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-700 disabled:bg-slate-300"
          >
            {isPending ? 'Kaydediliyor…' : 'Kaydet'}
          </button>
        </div>
      </div>
    </form>
  );
}
```

**Not:** "Bağlantıyı Test Et" yalnızca düzenleme modunda görünür — test backend'de kayıtlı dizin kimliğiyle çalışır, henüz kaydedilmemiş bir dizin test edilemez.

- [ ] **Step 2: Derleme kontrolü ve commit**

Run: `cd frontend && npx tsc -b --noEmit 2>&1 | head -20`
Expected: Hata yok.

```bash
git add frontend/src/components/admin/directory/DirectoryForm.tsx
git commit -m "feat: add directory create/edit form"
```

---

## Task 5: Kullanıcı listesi ve kullanıcı kartı

**Files:**
- Create: `frontend/src/components/admin/directory/DirectoryUserList.tsx`
- Create: `frontend/src/components/admin/directory/DirectoryUserCard.tsx`

**Interfaces:**
- Consumes: `useDirectoryUsers`, `useDirectoryUser`.
- Produces: `DirectoryUserList({ directory, onBack, onSelectUser })`, `DirectoryUserCard({ userId, onBack })`

- [ ] **Step 1: Kullanıcı listesini yaz**

`frontend/src/components/admin/directory/DirectoryUserList.tsx`:
```tsx
import { useState } from 'react';
import { useDirectoryUsers } from '../../../hooks/useDirectoryUsers';
import type { DirectoryDto } from '../../../api/types';

interface DirectoryUserListProps {
  directory: DirectoryDto;
  onBack: () => void;
  onSelectUser: (userId: string) => void;
}

export function DirectoryUserList({ directory, onBack, onSelectUser }: DirectoryUserListProps) {
  const [searchTerm, setSearchTerm] = useState('');
  const users = useDirectoryUsers({ directoryId: directory.id, searchTerm });
  const items = users.data?.items ?? [];

  return (
    <div>
      <div className="mb-4 flex items-center justify-between">
        <h2 className="text-base font-semibold text-slate-800">{directory.name} — Kullanıcılar</h2>
        <button type="button" onClick={onBack} className="text-sm text-slate-500 hover:text-slate-700">
          ← Listeye dön
        </button>
      </div>

      <input
        value={searchTerm}
        onChange={(e) => setSearchTerm(e.target.value)}
        placeholder="Kullanıcı adı, görünen ad veya e-posta ara"
        className="mb-4 w-full max-w-sm rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500"
      />

      {users.isLoading ? (
        <div className="py-8 text-center text-sm text-slate-400">Yükleniyor…</div>
      ) : items.length === 0 ? (
        <div className="rounded-xl border border-dashed border-slate-200 py-12 text-center text-sm text-slate-500">
          {searchTerm ? 'Aramayla eşleşen kullanıcı yok.' : 'Bu dizinde henüz kullanıcı yok.'}
        </div>
      ) : (
        <table className="w-full text-left text-sm">
          <thead>
            <tr className="border-b border-slate-100 text-xs uppercase tracking-wide text-slate-400">
              <th className="py-2 pr-4 font-medium">Kullanıcı Adı</th>
              <th className="py-2 pr-4 font-medium">Görünen Ad</th>
              <th className="py-2 pr-4 font-medium">E-posta</th>
              <th className="py-2 font-medium">Durum</th>
            </tr>
          </thead>
          <tbody>
            {items.map((user) => (
              <tr
                key={user.id}
                onClick={() => onSelectUser(user.id)}
                className="cursor-pointer border-b border-slate-50 last:border-0 hover:bg-slate-50"
              >
                <td className="py-2 pr-4 text-indigo-600">{user.username}</td>
                <td className="py-2 pr-4 text-slate-700">{user.displayName ?? '—'}</td>
                <td className="py-2 pr-4 text-slate-500">{user.email ?? '—'}</td>
                <td className="py-2">
                  <span
                    className={
                      'rounded-full px-2 py-0.5 text-xs font-medium ' +
                      (user.isActive
                        ? 'bg-emerald-50 text-emerald-700'
                        : 'bg-slate-100 text-slate-500')
                    }
                  >
                    {user.isActive ? 'Aktif' : 'Pasif'}
                  </span>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}
```

- [ ] **Step 2: Kullanıcı kartını yaz**

`frontend/src/components/admin/directory/DirectoryUserCard.tsx`:
```tsx
import { useDirectoryUser } from '../../../hooks/useDirectoryUsers';

interface DirectoryUserCardProps {
  userId: string;
  onBack: () => void;
}

const SOURCE_LABEL: Record<number, string> = {
  0: 'Internal',
  1: 'Active Directory',
};

function InfoRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex border-b border-slate-50 py-2 last:border-0">
      <div className="w-48 shrink-0 text-sm text-slate-500">{label}</div>
      <div className="text-sm text-slate-700">{value}</div>
    </div>
  );
}

export function DirectoryUserCard({ userId, onBack }: DirectoryUserCardProps) {
  const { data: user, isLoading } = useDirectoryUser(userId);

  return (
    <div>
      <div className="mb-4 flex items-center justify-between">
        <h2 className="text-base font-semibold text-slate-800">Kullanıcı Kartı</h2>
        <button type="button" onClick={onBack} className="text-sm text-slate-500 hover:text-slate-700">
          ← Kullanıcılara dön
        </button>
      </div>

      {isLoading || !user ? (
        <div className="py-8 text-center text-sm text-slate-400">Yükleniyor…</div>
      ) : (
        <div className="space-y-6">
          <div>
            <div className="flex items-center gap-2">
              <h3 className="text-base font-semibold text-slate-800">
                {user.displayName ?? user.username}
              </h3>
              <span
                className={
                  'rounded-full px-2 py-0.5 text-xs font-medium ' +
                  (user.isActive ? 'bg-emerald-50 text-emerald-700' : 'bg-slate-100 text-slate-500')
                }
              >
                {user.isActive ? 'Aktif' : 'Pasif'}
              </span>
            </div>
            <p className="mt-0.5 text-sm text-slate-500">{user.username}</p>
          </div>

          <div>
            <h4 className="mb-1 text-xs font-semibold uppercase tracking-wide text-slate-400">
              Hesap
            </h4>
            <InfoRow label="Dizin" value={user.directoryName} />
            <InfoRow label="Kaynak" value={SOURCE_LABEL[user.source] ?? '—'} />
            <InfoRow label="Ad" value={user.firstName ?? '—'} />
            <InfoRow label="Soyad" value={user.lastName ?? '—'} />
            <InfoRow label="E-posta" value={user.email ?? '—'} />
            <InfoRow
              label="Son Senkron"
              value={user.lastSyncedUtc ? new Date(user.lastSyncedUtc).toLocaleString('tr-TR') : '—'}
            />
          </div>

          <div>
            <h4 className="mb-1 text-xs font-semibold uppercase tracking-wide text-slate-400">
              Dizin Alanları
            </h4>
            {user.attributes.length === 0 ? (
              <p className="py-2 text-sm text-slate-400">
                Senkronize edilmiş alan yok. Alan Eşlemeleri bölümünden alan tanımlayıp dizini
                yeniden senkronize edin.
              </p>
            ) : (
              user.attributes.map((attribute) => (
                <InfoRow
                  key={attribute.adAttributeName}
                  label={attribute.systemFieldName}
                  value={attribute.value ?? '—'}
                />
              ))
            )}
          </div>
        </div>
      )}
    </div>
  );
}
```

- [ ] **Step 3: Derleme kontrolü ve commit**

Run: `cd frontend && npx tsc -b --noEmit 2>&1 | head -20`
Expected: Hata yok.

```bash
git add frontend/src/components/admin/directory/DirectoryUserList.tsx frontend/src/components/admin/directory/DirectoryUserCard.tsx
git commit -m "feat: add directory user list and user card"
```

---

## Task 6: Alan eşlemeleri bölümü

**Files:**
- Create: `frontend/src/components/admin/directory/AttributeMappingsSection.tsx`

**Interfaces:**
- Consumes: `useAttributeMappings` ve mutation'ları.

**Davranış:** Satır içi düzenlenebilir tablo. Yeni satır formu üstte; her satırda senkronizasyon açma/kapama ve silme. Eşlemeler tüm dizinler için ortaktır — bölüm başında bu belirtilir.

- [ ] **Step 1: Bileşeni yaz**

`frontend/src/components/admin/directory/AttributeMappingsSection.tsx`:
```tsx
import { useState, type FormEvent } from 'react';
import { ApiError } from '../../../api/client';
import {
  useAttributeMappings,
  useCreateAttributeMappingMutation,
  useDeleteAttributeMappingMutation,
  useUpdateAttributeMappingMutation,
} from '../../../hooks/useAttributeMappings';
import type { DirectoryAttributeMappingDto } from '../../../api/types';

const inputClass =
  'w-full rounded-md border border-slate-300 px-2 py-1.5 text-sm outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500';

export function AttributeMappingsSection() {
  const mappings = useAttributeMappings();
  const createMutation = useCreateAttributeMappingMutation();
  const updateMutation = useUpdateAttributeMappingMutation();
  const deleteMutation = useDeleteAttributeMappingMutation();

  const [adAttributeName, setAdAttributeName] = useState('');
  const [systemFieldName, setSystemFieldName] = useState('');
  const [fieldType, setFieldType] = useState('text');
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const items = mappings.data ?? [];

  const handleCreate = async (event: FormEvent) => {
    event.preventDefault();
    setErrorMessage(null);

    try {
      await createMutation.mutateAsync({
        adAttributeName: adAttributeName.trim(),
        systemFieldName: systemFieldName.trim(),
        fieldType,
        isSynced: true,
        sortOrder: items.length,
      });
      setAdAttributeName('');
      setSystemFieldName('');
      setFieldType('text');
    } catch (error) {
      setErrorMessage(error instanceof ApiError ? error.message : 'Alan eşlemesi eklenemedi.');
    }
  };

  const handleToggleSynced = async (mapping: DirectoryAttributeMappingDto) => {
    setErrorMessage(null);
    try {
      await updateMutation.mutateAsync({
        id: mapping.id,
        payload: {
          adAttributeName: mapping.adAttributeName,
          systemFieldName: mapping.systemFieldName,
          fieldType: mapping.fieldType,
          isSynced: !mapping.isSynced,
          sortOrder: mapping.sortOrder,
        },
      });
    } catch (error) {
      setErrorMessage(error instanceof ApiError ? error.message : 'Alan eşlemesi güncellenemedi.');
    }
  };

  const handleDelete = async (mapping: DirectoryAttributeMappingDto) => {
    if (!window.confirm(`"${mapping.systemFieldName}" eşlemesini silmek istediğinize emin misiniz?`))
      return;

    setErrorMessage(null);
    try {
      await deleteMutation.mutateAsync(mapping.id);
    } catch (error) {
      setErrorMessage(error instanceof ApiError ? error.message : 'Alan eşlemesi silinemedi.');
    }
  };

  const canCreate = adAttributeName.trim().length > 0 && systemFieldName.trim().length > 0;

  return (
    <div>
      <p className="mb-4 text-sm text-slate-500">
        Dizinden çekilecek alanlar. Bu eşlemeler <strong>tüm dizinler</strong> için ortaktır.
        Senkronizasyon kapalı olan alanlar dizinden çekilmez.
      </p>

      <form onSubmit={handleCreate} className="mb-5 flex flex-wrap items-end gap-2">
        <label className="block">
          <span className="mb-1 block text-xs font-medium text-slate-600">Dizindeki Alan</span>
          <input
            value={adAttributeName}
            onChange={(e) => setAdAttributeName(e.target.value)}
            placeholder="company"
            className={inputClass}
          />
        </label>
        <label className="block">
          <span className="mb-1 block text-xs font-medium text-slate-600">Sistemdeki Ad</span>
          <input
            value={systemFieldName}
            onChange={(e) => setSystemFieldName(e.target.value)}
            placeholder="Kurum"
            className={inputClass}
          />
        </label>
        <label className="block">
          <span className="mb-1 block text-xs font-medium text-slate-600">Tip</span>
          <select value={fieldType} onChange={(e) => setFieldType(e.target.value)} className={inputClass}>
            <option value="text">Metin</option>
            <option value="user">Kullanıcı</option>
          </select>
        </label>
        <button
          type="submit"
          disabled={!canCreate || createMutation.isPending}
          className="rounded-lg bg-indigo-600 px-3 py-2 text-sm font-medium text-white hover:bg-indigo-700 disabled:bg-slate-300"
        >
          {createMutation.isPending ? 'Ekleniyor…' : 'Ekle'}
        </button>
      </form>

      {errorMessage && (
        <p role="alert" className="mb-4 rounded-md bg-rose-50 px-3 py-2 text-sm text-rose-700">
          {errorMessage}
        </p>
      )}

      {mappings.isLoading ? (
        <div className="py-8 text-center text-sm text-slate-400">Yükleniyor…</div>
      ) : items.length === 0 ? (
        <div className="rounded-xl border border-dashed border-slate-200 py-12 text-center text-sm text-slate-500">
          Henüz alan eşlemesi tanımlanmamış.
        </div>
      ) : (
        <table className="w-full text-left text-sm">
          <thead>
            <tr className="border-b border-slate-100 text-xs uppercase tracking-wide text-slate-400">
              <th className="py-2 pr-4 font-medium">Dizindeki Alan</th>
              <th className="py-2 pr-4 font-medium">Sistemdeki Ad</th>
              <th className="py-2 pr-4 font-medium">Tip</th>
              <th className="py-2 pr-4 font-medium">Senkronize</th>
              <th className="py-2 font-medium">İşlem</th>
            </tr>
          </thead>
          <tbody>
            {items.map((mapping) => (
              <tr key={mapping.id} className="border-b border-slate-50 last:border-0">
                <td className="py-2 pr-4 font-mono text-xs text-slate-600">
                  {mapping.adAttributeName}
                </td>
                <td className="py-2 pr-4 text-slate-700">{mapping.systemFieldName}</td>
                <td className="py-2 pr-4 text-slate-500">
                  {mapping.fieldType === 'user' ? 'Kullanıcı' : 'Metin'}
                </td>
                <td className="py-2 pr-4">
                  <input
                    type="checkbox"
                    checked={mapping.isSynced}
                    onChange={() => handleToggleSynced(mapping)}
                    className="h-4 w-4"
                  />
                </td>
                <td className="py-2">
                  <button
                    type="button"
                    onClick={() => handleDelete(mapping)}
                    className="text-xs text-rose-600 hover:underline"
                  >
                    Sil
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}
```

- [ ] **Step 2: Derleme kontrolü ve commit**

Run: `cd frontend && npx tsc -b --noEmit 2>&1 | head -20`
Expected: Hata yok.

```bash
git add frontend/src/components/admin/directory/AttributeMappingsSection.tsx
git commit -m "feat: add global attribute mappings section"
```

---

## Task 7: AdminPage entegrasyonu

**Files:**
- Create: `frontend/src/components/admin/directory/UserDirectorySection.tsx`
- Modify: `frontend/src/pages/AdminPage.tsx`

**Interfaces:**
- Produces: `UserDirectorySection` — dört görünüm arasında geçişi yönetir; `AdminPage`'de "Kullanıcı Klasörü" ve "Alan Eşlemeleri" bölümleri.

- [ ] **Step 1: Kapsayıcı bileşeni yaz**

`frontend/src/components/admin/directory/UserDirectorySection.tsx`:
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
    return (
      <DirectoryForm directory={view.directory} onClose={() => setView({ kind: 'list' })} />
    );
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

- [ ] **Step 2: AdminPage'e bölümleri ekle**

`frontend/src/pages/AdminPage.tsx` — import ekle:
```typescript
import { UserDirectorySection } from '../components/admin/directory/UserDirectorySection';
import { AttributeMappingsSection } from '../components/admin/directory/AttributeMappingsSection';
```

`SectionKind` tipini genişlet:
```typescript
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

`ADMIN_TABS` içindeki `users` sekmesinin `sections` dizisini değiştir:
```typescript
        sections: [
          { key: 'employees', label: 'Çalışanlar', kind: 'employees' },
          { key: 'userDirectory', label: 'Kullanıcı Klasörü', kind: 'userDirectory' },
          { key: 'attributeMappings', label: 'Alan Eşlemeleri', kind: 'attributeMappings' },
          { key: 'roles', label: 'Roller ve İzinler', kind: 'placeholder' },
        ],
```

`SectionContent` switch'ine ekle:
```typescript
    case 'userDirectory':
      return <UserDirectorySection />;
    case 'attributeMappings':
      return <AttributeMappingsSection />;
```

- [ ] **Step 3: Derleme ve lint kontrolü**

Run: `cd frontend && npx tsc -b --noEmit 2>&1 | head -20 && npm run lint 2>&1 | grep -c "admin/directory" || echo "0 uyarı"`
Expected: Tip hatası yok; yeni dosyalarda lint uyarısı yok.

- [ ] **Step 4: Commit**

```bash
git add frontend/src/components/admin/directory/UserDirectorySection.tsx frontend/src/pages/AdminPage.tsx
git commit -m "feat: add user directory and attribute mappings admin sections"
```

---

## Task 8: Uçtan uca tarayıcı doğrulaması

**Files:** (kod değişikliği yok — doğrulama görevi)

- [ ] **Step 1: Backend ve frontend'i başlat, giriş yap**

Backend ve Vite çalışır durumda olmalı. Tarayıcıda giriş yap (`admin` / `Admin123!`), sağ üstteki ⚙️ Yönetim ikonuna tıkla, "Kullanıcı Yönetimi" sekmesine geç.
Expected: Sol menüde "Çalışanlar", "Kullanıcı Klasörü", "Alan Eşlemeleri", "Roller ve İzinler" görünür.

- [ ] **Step 2: Alan eşlemesi ekle**

"Alan Eşlemeleri" bölümüne geç, `company` / `Kurum` / Metin ekle.
Expected: Tabloya eklenir, senkronize kutusu işaretli gelir.

- [ ] **Step 3: Dizin oluştur**

"Kullanıcı Klasörü" → "Yeni Dizin Ekle". Ad: `Kızılay AD`, sunucu: `kizilay.local`, port `389`, bağlantı kullanıcısı ve şifre gir, Base DN: `DC=kizilay,DC=local`. Kaydet.
Expected: Listeye eklenir; tip "Active Directory", sunucu `kizilay.local:389`, son senkron "Hiç senkronize edilmedi".

- [ ] **Step 4: Bağlantı testini doğrula**

Oluşturduğun dizinde "Düzenle" → "Bağlantıyı Test Et".
Expected: Gerçek bir AD yoksa kırmızı uyarı ("Sunucuya ulaşılamıyor…"). İç sistem detayı görünmemeli.

- [ ] **Step 5: Şifrenin korunduğunu doğrula**

Düzenleme formunda şifre alanının **boş** geldiğini doğrula, şifreyi boş bırakıp adı değiştirerek kaydet, tekrar düzenlemeyi aç.
Expected: Ad değişmiş; şifre alanı yine boş. (Backend mevcut şifreyi korur.)

- [ ] **Step 6: Senkronizasyon hatasını doğrula**

Listeden "Senkronize Et" tıkla.
Expected: Kırmızı uyarı satırı — "Sunucuya ulaşılamıyor. Adres ve port bilgisini kontrol edin." Uygulama çökmemeli.

- [ ] **Step 7: Internal kullanıcıyı kartta doğrula**

"Internal Users" dizininde "Kullanıcılar" → `admin` satırına tıkla.
Expected: Kullanıcı kartı açılır; Dizin "Internal Users", Kaynak "Internal", durum "Aktif". "Dizin Alanları" bölümünde "Senkronize edilmiş alan yok" mesajı görünür (internal kullanıcının AD attribute'u yoktur).

- [ ] **Step 8: Dizin silmeyi doğrula**

Test dizinini sil (onay kutusunu kabul et).
Expected: Listeden kalkar.

- [ ] **Step 9: Bulguları raporla**

Sapma varsa düzelt ve ilgili görevi tekrar çalıştır.

---

## Faz 4b Tamamlanma Kriteri

- [ ] `cd frontend && npm run build` temiz geçiyor.
- [ ] `cd frontend && npm run lint` yeni uyarı üretmiyor.
- [ ] Ayarlar → Kullanıcı Yönetimi altında "Kullanıcı Klasörü" ve "Alan Eşlemeleri" bölümleri görünüyor.
- [ ] Dizin oluşturma, düzenleme, silme arayüzden çalışıyor.
- [ ] Düzenlemede şifre alanı boş geliyor ve boş bırakıldığında mevcut şifre korunuyor.
- [ ] Bağlantı testi ve senkronizasyon sonuçları (başarı ve hata) arayüzde anlaşılır şekilde gösteriliyor.
- [ ] Kullanıcı listesi ve kullanıcı kartı, senkronize edilen attribute'ları gösteriyor.
- [ ] Alan eşlemesi ekleme, senkronizasyon açma/kapama ve silme çalışıyor.

## Bilinen Sınırlar

- Alan eşlemesinde satır içi **ad/tip düzenleme** yok; yalnızca senkronizasyon açılıp kapatılabilir ve satır silinebilir. Ad değişikliği için sil–yeniden ekle gerekir.
- Dizin sıralaması (`sortOrder`) arayüzden değiştirilemiyor.
- Internal kullanıcı oluşturma arayüzü bu fazda yok — API'si var (`POST /directoryusers/internal`), ekranı ayrı bir iş.
- Dizin aktif/pasif yapma arayüzden yapılamıyor (backend'de `Activate`/`Deactivate` var ama endpoint'i yok).
- Kullanıcı listesi ilk 100 kayıtla sınırlı; sayfalama arayüzü yok. Büyük dizinlerde arama kutusu kullanılmalı.
