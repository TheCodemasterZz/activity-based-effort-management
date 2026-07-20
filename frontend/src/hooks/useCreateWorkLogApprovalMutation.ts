import { useMutation, useQueryClient } from '@tanstack/react-query';
import { createWorkLogApproval } from '../api/workLogApprovals';

export function useCreateWorkLogApprovalMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: createWorkLogApproval,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['workLogs'] });
      queryClient.invalidateQueries({ queryKey: ['workLogApprovals'] });
    },
  });
}
