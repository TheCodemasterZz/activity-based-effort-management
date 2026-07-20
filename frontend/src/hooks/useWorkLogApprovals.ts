import { useQuery } from '@tanstack/react-query';
import { getWorkLogApprovals } from '../api/workLogApprovals';

export function useWorkLogApprovals() {
  return useQuery({ queryKey: ['workLogApprovals'], queryFn: getWorkLogApprovals });
}
