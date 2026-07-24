import { useQuery } from '@tanstack/react-query';
import { getUserById, getUsers } from '../api/users';
import type { PagedResult, UserDto } from '../api/types';
import { userDisplayName } from '../lib/userDisplayName';

/** Work log / plan / izin / proje ekranlarının kişi kaynağı — eski Employees API'nin
 * (Faz 4'te silinecek) yerine Users API. Eski EmployeeDto şekline (id/name/workCalendarId)
 * eşlenir ki tüketici bileşenler alan adlarını değiştirmek zorunda kalmasın; tek fark
 * workCalendarId'nin null olabilmesi (Faz 2 kararı: senkronda default takvim atanmaz). */
export interface UserRosterEntry {
  id: string;
  name: string;
  email: string | null;
  workCalendarId: string | null;
}

function toRosterEntry(user: UserDto): UserRosterEntry {
  return {
    id: user.id,
    name: userDisplayName(user),
    email: user.email,
    workCalendarId: user.workCalendarId,
  };
}

function mapPage(page: PagedResult<UserDto>): PagedResult<UserRosterEntry> {
  return { ...page, items: page.items.map(toRosterEntry) };
}

/** Tüm aktif kullanıcılar, tek sayfada (kişi listeleri/çözümleyiciler için). */
export function useUserRoster() {
  return useQuery({
    queryKey: ['users', 'roster'],
    queryFn: () => getUsers({ onlyActive: true, pageSize: 1000 }),
    select: mapPage,
  });
}

/** Yazdıkça arama — küçük sayfa boyutuyla sunucu taraflı arar. */
export function useUserSearch(nameFilter: string) {
  return useQuery({
    queryKey: ['users', 'roster-search', nameFilter],
    queryFn: () => getUsers({ searchTerm: nameFilter || undefined, onlyActive: true, pageSize: 10 }),
    select: mapPage,
  });
}

export async function getUserRosterEntry(id: string): Promise<UserRosterEntry> {
  return toRosterEntry(await getUserById(id));
}

export function useUserById(id: string | null) {
  return useQuery({
    queryKey: ['users', 'roster-detail', id],
    queryFn: () => getUserRosterEntry(id as string),
    enabled: id !== null,
  });
}
