import { useMutation, useQuery } from '@tanstack/react-query';
import {
  getDirectoryUserById,
  getDirectoryUsers,
  resetInternalUserPassword,
} from '../api/directoryUsers';

export function useDirectoryUsers(options: {
  directoryId?: string;
  searchTerm?: string;
  pageNumber?: number;
  pageSize?: number;
}) {
  return useQuery({
    queryKey: [
      'directoryUsers',
      options.directoryId ?? null,
      options.searchTerm ?? '',
      options.pageNumber ?? 1,
      options.pageSize ?? 25,
    ],
    queryFn: () => getDirectoryUsers(options),
  });
}

export function useDirectoryUser(id: string | null) {
  return useQuery({
    queryKey: ['directoryUsers', 'detail', id],
    queryFn: () => getDirectoryUserById(id!),
    enabled: id !== null,
  });
}

export function useResetInternalUserPasswordMutation() {
  return useMutation({
    mutationFn: ({ userId, newPassword }: { userId: string; newPassword: string }) =>
      resetInternalUserPassword(userId, newPassword),
  });
}
