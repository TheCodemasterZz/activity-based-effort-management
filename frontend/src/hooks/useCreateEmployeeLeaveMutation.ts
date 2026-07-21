import { useMutation, useQueryClient } from '@tanstack/react-query';
import { createEmployeeLeave } from '../api/employeeLeaves';

export function useCreateEmployeeLeaveMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: createEmployeeLeave,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['employeeLeaves'] });
    },
  });
}
