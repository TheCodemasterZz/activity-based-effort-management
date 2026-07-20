import { apiClient } from './client';
import type { ActivityDto, PagedResult } from './types';

export function getActivities(options?: {
  parentActivityId?: string;
  onlyTopLevel?: boolean;
  pageSize?: number;
  pageNumber?: number;
}) {
  return apiClient.get<PagedResult<ActivityDto>>('/api/v1/activities', {
    parentActivityId: options?.parentActivityId,
    onlyTopLevel: options?.onlyTopLevel,
    pageSize: options?.pageSize ?? 100,
    pageNumber: options?.pageNumber ?? 1,
  });
}

/** Sunucu sayfa başına en fazla 100 kayıt döndürdüğü için (ör. Software Delivery kataloğu
 * 145 aktivite içeriyor), id->ad çözümleme haritaları için tüm sayfaları birleştirir. */
export async function getAllActivitiesAcrossPages(): Promise<PagedResult<ActivityDto>> {
  const first = await getActivities({ pageSize: 100, pageNumber: 1 });
  if (first.totalPages <= 1) return first;

  const restPages = await Promise.all(
    Array.from({ length: first.totalPages - 1 }, (_, i) => getActivities({ pageSize: 100, pageNumber: i + 2 })),
  );

  return { ...first, items: [...first.items, ...restPages.flatMap((page) => page.items)] };
}
