import { useQuery } from '@tanstack/react-query';
import { getAllProjectIssues, getProjectIssues } from '../api/projectIssues';

export function useProjectIssues(projectId: string | null) {
  return useQuery({
    queryKey: ['projectIssues', projectId],
    queryFn: () => getProjectIssues(projectId as string),
    enabled: projectId !== null,
  });
}

/** ProjectsPage'deki RAG göstergeleri için — tüm projelerin sorunlarını tek seferde çeker. */
export function useAllProjectIssues() {
  return useQuery({ queryKey: ['projectIssues', 'all'], queryFn: getAllProjectIssues });
}
