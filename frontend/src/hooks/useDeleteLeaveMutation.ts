import { useMutation, useQueryClient } from '@tanstack/react-query';
import { deleteLeave } from '../api/leaves';

export function useDeleteLeaveMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => deleteLeave(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['leaves'] });
    },
  });
}
