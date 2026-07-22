import { useQuery } from '@tanstack/react-query';
import { getAllProjectTasks, getProjectTasks } from '../api/projectTasks';

export function useProjectTasks(projectId: string | null) {
  return useQuery({
    queryKey: ['projectTasks', projectId],
    queryFn: () => getProjectTasks(projectId as string),
    enabled: projectId !== null,
  });
}

/** ProjectsPage'deki kart-grid için — tüm projelerin görevlerini tek seferde çeker. */
export function useAllProjectTasks() {
  return useQuery({ queryKey: ['projectTasks', 'all'], queryFn: getAllProjectTasks });
}
