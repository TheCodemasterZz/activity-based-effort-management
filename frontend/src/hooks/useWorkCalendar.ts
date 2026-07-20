import { useQuery } from '@tanstack/react-query';
import { getWorkCalendarById } from '../api/workCalendars';

export function useWorkCalendar(id: string | null) {
  return useQuery({
    queryKey: ['workCalendars', id],
    queryFn: () => getWorkCalendarById(id as string),
    enabled: id !== null,
  });
}
