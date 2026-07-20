import { useQuery, useQueries } from '@tanstack/react-query';
import { getValueStreamById, getValueStreams } from '../api/valueStreams';

export function useValueStreams() {
  return useQuery({ queryKey: ['valueStreams'], queryFn: () => getValueStreams() });
}

export function useValueStreamStageCount(valueStreamIds: string[]) {
  const results = useQueries({
    queries: valueStreamIds.map((id) => ({
      queryKey: ['valueStreams', id],
      queryFn: () => getValueStreamById(id),
      enabled: valueStreamIds.length > 0,
    })),
  });

  const isLoading = results.some((r) => r.isLoading);
  const total = results.reduce((sum, r) => sum + (r.data?.stages.length ?? 0), 0);
  return { isLoading, total };
}
