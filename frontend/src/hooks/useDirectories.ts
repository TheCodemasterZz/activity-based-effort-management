import { useQuery } from '@tanstack/react-query';
import { getDirectories, getDirectoryById, getOrgChart } from '../api/directories';

export function useDirectories() {
  return useQuery({ queryKey: ['directories'], queryFn: getDirectories });
}

export function useDirectory(id: string | null) {
  return useQuery({
    queryKey: ['directories', id],
    queryFn: () => getDirectoryById(id!),
    enabled: id !== null,
  });
}

export function useOrgChart(directoryId: string | null) {
  return useQuery({
    queryKey: ['directories', directoryId, 'org-chart'],
    queryFn: () => getOrgChart(directoryId!),
    enabled: directoryId !== null,
  });
}
