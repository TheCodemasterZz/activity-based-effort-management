import { getWorkLogs } from '../api/workLogs';
import { WORK_LOG_ENTRY_TYPE, type WorkCalendarDetailDto, type WorkLogEntryType } from '../api/types';

export interface OvertimeCheckParams {
  userId: string;
  calendar: WorkCalendarDetailDto;
  startDate: string;
  endDate: string;
  hoursPerDay: number;
  /** Düzenleme modunda, güncellenmekte olan kaydın kendisi toplam saatten hariç tutulur. */
  excludeWorkLogId?: string;
  /** Aşım kontrolü, hangi türdeki mevcut kayıtlara göre yapılacak — Planned formunda Actual
   * kayıtlarla karışmasın diye. */
  entryType?: WorkLogEntryType;
}

function toMinutes(time: string): number {
  const [hours, minutes] = time.split(':').map(Number);
  return hours * 60 + minutes;
}

/** Yerel (local) tarihi "yyyy-MM-dd" olarak biçimlendirir — toISOString() UTC'ye çevirdiği
 * için saat dilimine göre bir gün kayabilir, bu yüzden kullanılmıyor. */
function localDateKey(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

function eachDateInRange(startDate: string, endDate: string): string[] {
  const dates: string[] = [];
  let cursor = new Date(`${startDate}T00:00:00`);
  const end = new Date(`${endDate}T00:00:00`);

  while (cursor <= end) {
    dates.push(localDateKey(cursor));
    cursor = new Date(cursor.getTime() + 24 * 60 * 60 * 1000);
  }

  return dates;
}

/** Girilecek tarih aralığındaki, çalışanın mesai takvimini aşacak günlerin listesini döner (boşsa aşım yok demektir). */
export async function findOvertimeDates(params: OvertimeCheckParams): Promise<string[]> {
  const dates = eachDateInRange(params.startDate, params.endDate);

  const existing = await getWorkLogs({
    userId: params.userId,
    dateFrom: params.startDate,
    dateTo: params.endDate,
    pageSize: 5000,
    entryType: params.entryType ?? WORK_LOG_ENTRY_TYPE.Actual,
  });

  const existingHoursByDate = new Map<string, number>();
  for (const log of existing.items) {
    if (log.id === params.excludeWorkLogId) continue;
    existingHoursByDate.set(log.workDate, (existingHoursByDate.get(log.workDate) ?? 0) + log.hours);
  }

  const exceededDates: string[] = [];

  for (const date of dates) {
    const dayOfWeek = new Date(`${date}T00:00:00`).getDay();
    const dayConfig = params.calendar.days.find((d) => d.dayOfWeek === dayOfWeek);
    const allowedHours =
      dayConfig?.isWorkingDay && dayConfig.startTime && dayConfig.endTime
        ? (toMinutes(dayConfig.endTime) - toMinutes(dayConfig.startTime)) / 60
        : 0;

    const totalHours = (existingHoursByDate.get(date) ?? 0) + params.hoursPerDay;
    if (totalHours > allowedHours) {
      exceededDates.push(date);
    }
  }

  return exceededDates;
}
