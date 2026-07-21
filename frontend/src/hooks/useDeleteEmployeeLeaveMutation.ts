import { useMutation, useQueryClient } from '@tanstack/react-query';
import { deleteEmployeeLeave } from '../api/employeeLeaves';

export function useDeleteEmployeeLeaveMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => deleteEmployeeLeave(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['employeeLeaves'] });
    },
  });
}
