import { useMutation, useQueryClient } from '@tanstack/react-query';
import { deleteWorkLog } from '../api/workLogs';

export function useDeleteWorkLogMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => deleteWorkLog(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['workLogs'] });
    },
  });
}
