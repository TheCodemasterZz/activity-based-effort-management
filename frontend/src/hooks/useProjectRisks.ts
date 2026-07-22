import { useQuery } from '@tanstack/react-query';
import { getAllProjectRisks, getProjectRisks } from '../api/projectRisks';

export function useProjectRisks(projectId: string | null) {
  return useQuery({
    queryKey: ['projectRisks', projectId],
    queryFn: () => getProjectRisks(projectId as string),
    enabled: projectId !== null,
  });
}

/** ProjectsPage'deki RAG göstergeleri için — tüm projelerin risklerini tek seferde çeker. */
export function useAllProjectRisks() {
  return useQuery({ queryKey: ['projectRisks', 'all'], queryFn: getAllProjectRisks });
}
