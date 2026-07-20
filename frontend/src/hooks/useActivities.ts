import { useQuery } from '@tanstack/react-query';
import { getActivities, getAllActivitiesAcrossPages } from '../api/activities';

export function useTopLevelActivities() {
  return useQuery({
    queryKey: ['activities', 'topLevel'],
    queryFn: () => getActivities({ onlyTopLevel: true }),
  });
}

export function useSubActivities(parentActivityId: string | null) {
  return useQuery({
    queryKey: ['activities', 'children', parentActivityId],
    queryFn: () => getActivities({ parentActivityId: parentActivityId as string }),
    enabled: parentActivityId !== null,
  });
}

export function useAllActivities() {
  return useQuery({ queryKey: ['activities', 'all'], queryFn: () => getAllActivitiesAcrossPages() });
}
