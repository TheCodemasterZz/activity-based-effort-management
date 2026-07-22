import { useMutation, useQueryClient } from '@tanstack/react-query';
import { updateProjectHealth } from '../api/projects';
import type { ProjectHealthStatusValue } from '../api/types';

export function useUpdateProjectHealthMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (variables: { id: string; healthStatus: ProjectHealthStatusValue }) =>
      updateProjectHealth(variables.id, variables.healthStatus),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['projects'] });
    },
  });
}
