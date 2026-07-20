import { useQuery } from '@tanstack/react-query';
import { getHolidays } from '../api/holidays';

export function useHolidays() {
  return useQuery({ queryKey: ['holidays'], queryFn: () => getHolidays() });
}
