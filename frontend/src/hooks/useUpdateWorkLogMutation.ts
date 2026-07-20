import { useMutation, useQueryClient } from '@tanstack/react-query';
import { updateWorkLog, type UpdateWorkLogPayload } from '../api/workLogs';

export function useUpdateWorkLogMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (variables: { id: string; payload: UpdateWorkLogPayload }) =>
      updateWorkLog(variables.id, variables.payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['workLogs'] });
    },
  });
}
