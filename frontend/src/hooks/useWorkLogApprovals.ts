import { useQuery } from '@tanstack/react-query';
import { getWorkLogApprovals } from '../api/workLogApprovals';
import { WORK_LOG_ENTRY_TYPE, type WorkLogEntryType } from '../api/types';

export function useWorkLogApprovals(entryType: WorkLogEntryType = WORK_LOG_ENTRY_TYPE.Actual) {
  return useQuery({
    queryKey: ['workLogApprovals', entryType],
    queryFn: () => getWorkLogApprovals(entryType),
  });
}
