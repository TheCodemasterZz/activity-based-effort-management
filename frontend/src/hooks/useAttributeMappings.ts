import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  createAttributeMapping,
  deleteAttributeMapping,
  getAttributeMappings,
  updateAttributeMapping,
  type SaveAttributeMappingPayload,
} from '../api/directoryAttributeMappings';

const queryKey = (directoryId: string) => ['directoryAttributeMappings', directoryId];

export function useAttributeMappings(directoryId: string) {
  return useQuery({
    queryKey: queryKey(directoryId),
    queryFn: () => getAttributeMappings(directoryId),
  });
}

export function useCreateAttributeMappingMutation(directoryId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: SaveAttributeMappingPayload) => createAttributeMapping(directoryId, payload),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKey(directoryId) }),
  });
}

export function useUpdateAttributeMappingMutation(directoryId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: SaveAttributeMappingPayload }) =>
      updateAttributeMapping(directoryId, id, payload),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKey(directoryId) }),
  });
}

export function useDeleteAttributeMappingMutation(directoryId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => deleteAttributeMapping(directoryId, id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKey(directoryId) }),
  });
}
