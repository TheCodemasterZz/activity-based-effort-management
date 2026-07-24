import { useMutation, useQueryClient } from '@tanstack/react-query';
import { createLeave } from '../api/leaves';

export function useCreateLeaveMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: createLeave,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['leaves'] });
    },
  });
}
