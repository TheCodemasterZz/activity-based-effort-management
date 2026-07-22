import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  createAttributeMapping,
  deleteAttributeMapping,
  getAttributeMappings,
  updateAttributeMapping,
  type SaveAttributeMappingPayload,
} from '../api/directoryAttributeMappings';

const QUERY_KEY = ['directoryAttributeMappings'];

export function useAttributeMappings() {
  return useQuery({ queryKey: QUERY_KEY, queryFn: getAttributeMappings });
}

export function useCreateAttributeMappingMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: createAttributeMapping,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: QUERY_KEY }),
  });
}

export function useUpdateAttributeMappingMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: SaveAttributeMappingPayload }) =>
      updateAttributeMapping(id, payload),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: QUERY_KEY }),
  });
}

export function useDeleteAttributeMappingMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: deleteAttributeMapping,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: QUERY_KEY }),
  });
}
