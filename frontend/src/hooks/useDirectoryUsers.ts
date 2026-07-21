import { useQuery } from '@tanstack/react-query';
import { getDirectoryUserById, getDirectoryUsers } from '../api/directoryUsers';

export function useDirectoryUsers(options: { directoryId?: string; searchTerm?: string }) {
  return useQuery({
    queryKey: ['directoryUsers', options.directoryId ?? null, options.searchTerm ?? ''],
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
