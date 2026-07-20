import { useMutation, useQueryClient } from '@tanstack/react-query';
import { updateProject, type SaveProjectPayload } from '../api/projects';

export function useUpdateProjectMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (variables: { id: string; payload: SaveProjectPayload }) =>
      updateProject(variables.id, variables.payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['projects'] });
    },
  });
}
