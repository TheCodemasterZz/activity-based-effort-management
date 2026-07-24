import { useQuery } from '@tanstack/react-query';
import { getProjectById, getProjects } from '../api/projects';

export function useProjects() {
  return useQuery({ queryKey: ['projects'], queryFn: () => getProjects() });
}

export function useProjectDetail(projectId: string | null) {
  return useQuery({
    queryKey: ['projects', projectId],
    queryFn: () => getProjectById(projectId as string),
    enabled: projectId !== null,
  });
}

/** Yazdıkça arama — seçili çalışana atanmış projeler arasından, küçük sayfa boyutuyla sunucu taraflı arar. */
export function useProjectSearch(nameFilter: string, userId: string | null) {
  return useQuery({
    queryKey: ['projects', 'search', nameFilter, userId],
    queryFn: () => getProjects({ nameFilter, userId: userId ?? undefined, pageSize: 10 }),
    enabled: userId !== null,
  });
}
