import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  assignWorkCalendar,
  bulkAssignWorkCalendar,
  getUserById,
  getUsers,
  resetInternalUserPassword,
} from '../api/users';

export function useUsers(options: {
  directoryId?: string;
  searchTerm?: string;
  onlyMissingWorkCalendar?: boolean;
  pageNumber?: number;
  pageSize?: number;
}) {
  return useQuery({
    queryKey: [
      'users',
      options.directoryId ?? null,
      options.searchTerm ?? '',
      options.onlyMissingWorkCalendar ?? false,
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

export function useAssignWorkCalendarMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ userId, workCalendarId }: { userId: string; workCalendarId: string }) =>
      assignWorkCalendar(userId, workCalendarId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
    },
  });
}

export function useBulkAssignWorkCalendarMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ userIds, workCalendarId }: { userIds: string[]; workCalendarId: string }) =>
      bulkAssignWorkCalendar(userIds, workCalendarId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
    },
  });
}
