import { useMutation, useQueryClient } from '@tanstack/react-query';
import { logWork } from '../api/workLogs';

export function useLogWorkMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: logWork,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['workLogs'] });
    },
  });
}
