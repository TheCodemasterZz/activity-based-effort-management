import { useMutation, useQuery } from '@tanstack/react-query';
import {
  getUserById,
  getUsers,
  resetInternalUserPassword,
} from '../api/users';

export function useUsers(options: {
  directoryId?: string;
  searchTerm?: string;
  pageNumber?: number;
  pageSize?: number;
}) {
  return useQuery({
    queryKey: [
      'users',
      options.directoryId ?? null,
      options.searchTerm ?? '',
      options.pageNumber ?? 1,
      options.pageSize ?? 25,
    ],
    queryFn: () => getUsers(options),
  });
}

export function useUser(id: string | null) {
  return useQuery({
    queryKey: ['users', 'detail', id],
    queryFn: () => getUserById(id!),
    enabled: id !== null,
  });
}

export function useResetInternalUserPasswordMutation() {
  return useMutation({
    mutationFn: ({ userId, newPassword }: { userId: string; newPassword: string }) =>
      resetInternalUserPassword(userId, newPassword),
  });
}
