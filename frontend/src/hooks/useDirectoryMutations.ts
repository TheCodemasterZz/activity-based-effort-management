import { useMutation, useQueryClient } from '@tanstack/react-query';
import {
  createDirectory,
  deleteDirectory,
  syncDirectory,
  testDirectoryConnection,
  updateDirectory,
  type SaveDirectoryPayload,
} from '../api/directories';

export function useCreateDirectoryMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: createDirectory,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['directories'] }),
  });
}

export function useUpdateDirectoryMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: SaveDirectoryPayload }) =>
      updateDirectory(id, payload),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['directories'] }),
  });
}

export function useDeleteDirectoryMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: deleteDirectory,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['directories'] }),
  });
}

export function useSyncDirectoryMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: syncDirectory,
    onSuccess: () => {
      // Senkronizasyon hem kullanıcıları hem dizinin son senkron zamanını değiştirir.
      queryClient.invalidateQueries({ queryKey: ['directories'] });
      queryClient.invalidateQueries({ queryKey: ['users'] });
    },
  });
}

export function useTestDirectoryConnectionMutation() {
  return useMutation({ mutationFn: testDirectoryConnection });
}
