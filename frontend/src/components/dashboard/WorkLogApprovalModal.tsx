import { useMemo, useState } from 'react';
import { addWeeks, eachDayOfInterval, endOfWeek, format, startOfWeek } from 'date-fns';
import { tr } from 'date-fns/locale/tr';
import { AsyncSearchSelect } from '../common/AsyncSearchSelect';
import { useEmployeeSearch } from '../../hooks/useEmployees';
import { useEmployeeById } from '../../hooks/useEmployeeById';
import { useEmployeeWorkLogs } from '../../hooks/useWorkLogs';
import { useEmployeeLeaves } from '../../hooks/useEmployeeLeaves';
import { useHolidays } from '../../hooks/useHolidays';
import { useWorkCalendar } from '../../hooks/useWorkCalendar';
import { useCreateWorkLogApprovalMutation } from '../../hooks/useCreateWorkLogApprovalMutation';
import { pushSuccessNotification } from '../../lib/notifications';
import { WORK_LOG_ENTRY_TYPE, type WorkLogEntryType } from '../../api/types';

interface WorkLogApprovalModalProps {
  onClose: () => void;
  resolveProject: (id: string) => string;
  resolveCustomer: (id: string) => string;
  resolveActivity: (id: string) => string;
  /** Gerçekleşen (Log Work, varsayılan) mı yoksa planlanan (Plan Work) haftayı mı onaylıyoruz.
   * Planned onaylarda "sonraki hafta" engeli kalkar — planlama zaten geleceğe dönüktür. */
  entryType?: WorkLogEntryType;
}

function dateKey(d: Date): string {
  return format(d, 'yyyy-MM-dd');
}

function formatDateTr(dateIso: string): string {
  return new Date(`${dateIso}T00:00:00`).toLocaleDateString('tr-TR', { day: '2-digit', month: 'short' });
}

function toMinutes(time: string): number {
  const [hours, minutes] = time.split(':').map(Number);
  return hours * 60 + minutes;
}

function formatTimeShort(time: string): string {
  const [hours, minutes] = time.split(':');
  return `${hours}:${minutes}`;
}

interface WeekDayInfo {
  dateKey: string;
  weekdayLabel: string;
  dayNum: string;
  holidayName: string | null;
  leave: { isFullDay: boolean; startTime: string | null; endTime: string | null; hours: number } | null;
}

function formatHoursLabel(hours: number): string {
  return `${hours % 1 === 0 ? hours : hours.toFixed(1)}h`;
}

/** Onay sadece tam hafta (Pazartesi–Pazar) bazında verilebilir — bu yüzden serbest tarih girişi
 * yerine "Önceki/Bu/Sonraki hafta" seçimi kullanılıyor; bir haftanın yalnızca bir kısmının
 * onaylanması (ör. Pazartesi onaylı, Salı onaysız, Çarşamba onaylı) böylece hiç mümkün olmuyor. */
export function WorkLogApprovalModal({
  onClose,
  resolveProject,
  resolveCustomer,
  resolveActivity,
  entryType = WORK_LOG_ENTRY_TYPE.Actual,
}: WorkLogApprovalModalProps) {
  const isPlanned = entryType === WORK_LOG_ENTRY_TYPE.Planned;
  const [employeeId, setEmployeeId] = useState('');
  const [employeeLabel, setEmployeeLabel] = useState('');
  const [employeeQuery, setEmployeeQuery] = useState('');
  const employeeSearch = useEmployeeSearch(employeeQuery);

  const [weekOffset, setWeekOffset] = useState(0);
  const [description, setDescription] = useState('');
  const [descriptionTouched, setDescriptionTouched] = useState(false);

  const approveMutation = useCreateWorkLogApprovalMutation();

  const week = useMemo(() => {
    const base = addWeeks(new Date(), weekOffset);
    const start = startOfWeek(base, { weekStartsOn: 1 });
    const end = endOfWeek(base, { weekStartsOn: 1 });
    return {
      startKey: dateKey(start),
      endKey: dateKey(end),
      label: `${format(start, 'd MMM', { locale: tr })} – ${format(end, 'd MMM yyyy', { locale: tr })}`,
    };
  }, [weekOffset]);

  const previewLogs = useEmployeeWorkLogs(employeeId || null, week.startKey, week.endKey, entryType);
  const previewItems = previewLogs.data?.items ?? [];
  const previewTotalHours = previewItems.reduce((sum, l) => sum + l.hours, 0);
  const previewApprovedCount = previewItems.filter((l) => l.isApproved).length;

  const employee = useEmployeeById(employeeId || null);
  const calendar = useWorkCalendar(employee.data?.workCalendarId ?? null);
  const holidays = useHolidays();
  const weekLeaves = useEmployeeLeaves(
    employeeId ? { employeeId, dateFrom: week.startKey, dateTo: week.endKey } : undefined,
    { enabled: !!employeeId },
  );

  const weekDays = useMemo<WeekDayInfo[]>(() => {
    const start = new Date(`${week.startKey}T00:00:00`);
    const end = new Date(`${week.endKey}T00:00:00`);
    return eachDayOfInterval({ start, end }).map((d) => {
      const key = dateKey(d);
      const holiday = holidays.data?.items.find((h) => h.date === key) ?? null;
      // employeeId eşleşmesi burada da ayrıca kontrol edilir: employeeId boşken react-query
      // aynı ("employeeLeaves", null, null, null) cache anahtarını ReportPage'in filtresiz genel
      // izin sorgusuyla paylaşabiliyor — bu da kişi seçilmeden önce başka çalışanların izin
      // günlerinin yanlışlıkla gösterilmesine yol açardı.
      const leave = employeeId
        ? (weekLeaves.data?.items.find((l) => l.employeeId === employeeId && key >= l.startDate && key <= l.endDate) ?? null)
        : null;
      const calendarDay = calendar.data?.days.find((c) => c.dayOfWeek === d.getDay());
      // Tam günlük izinde, o günün çalışan takvimine göre beklenen mesai saati kadar izin
      // kullanılmış sayılır; kısmi izinde ise fiilen girilen saat aralığı esas alınır.
      const dayExpectedHours =
        calendarDay?.isWorkingDay && calendarDay.startTime && calendarDay.endTime
          ? (toMinutes(calendarDay.endTime) - toMinutes(calendarDay.startTime)) / 60
          : 0;
      const leaveHours = leave
        ? leave.isFullDay
          ? dayExpectedHours
          : leave.startTime && leave.endTime
            ? (toMinutes(leave.endTime) - toMinutes(leave.startTime)) / 60
            : 0
        : 0;

      return {
        dateKey: key,
        weekdayLabel: format(d, 'EEEEEE', { locale: tr }).toUpperCase(),
        dayNum: format(d, 'd'),
        holidayName: holiday?.name ?? null,
        leave: leave
          ? { isFullDay: leave.isFullDay, startTime: leave.startTime, endTime: leave.endTime, hours: leaveHours }
          : null,
      };
    });
  }, [week.startKey, week.endKey, employeeId, holidays.data, weekLeaves.data, calendar.data]);

  const weekHolidayCount = weekDays.filter((d) => d.holidayName).length;
  const weekLeaveCount = weekDays.filter((d) => d.leave).length;

  const descriptionError = description.trim().length === 0 ? 'Onay açıklaması zorunludur.' : undefined;
  const canSubmit = !!employeeId && !descriptionError;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setDescriptionTouched(true);
    if (!canSubmit) return;

    if (previewItems.length === 0) {
      const proceedEmpty = window.confirm(
        `${employeeLabel} için ${week.label} haftasında hiç efor kaydı yok. Yine de bu haftayı onaylamak istiyor musunuz?`,
      );
      if (!proceedEmpty) return;
    } else {
      const confirmed = window.confirm(
        `${employeeLabel} için ${week.label} haftasını (${previewTotalHours.toFixed(1)}h, ${previewItems.length} kayıt) onaylamak istediğinize emin misiniz? Onaylandıktan sonra bu kayıtlar değiştirilemez/silinemez.`,
      );
      if (!confirmed) return;
    }

    try {
      await approveMutation.mutateAsync({
        employeeId,
        periodType: 1,
        periodStart: week.startKey,
        periodEnd: week.endKey,
        description: description.trim(),
        entryType,
      });
      pushSuccessNotification(`${employeeLabel} için ${week.label} haftası onaylandı.`);
      onClose();
    } catch {
      // Hata zaten global mutationCache.onError üzerinden sağ üstte bildirim olarak gösteriliyor.
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/40 p-4">
      <div className="max-h-[90vh] w-full max-w-4xl overflow-y-auto rounded-xl bg-white p-6 shadow-xl">
        <div className="mb-4 flex items-center justify-between">
          <h2 className="text-lg font-semibold text-slate-800">{isPlanned ? 'Plan Onayı' : 'Efor Onayı'}</h2>
          <button type="button" onClick={onClose} className="text-slate-400 hover:text-slate-600">
            ✕
          </button>
        </div>

        <form onSubmit={handleSubmit}>
          <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
            <div className="space-y-3">
              <div>
                <label className="mb-1 block text-xs font-medium text-slate-500">
                  Kişi <span className="text-red-500">*</span>
                </label>
                <AsyncSearchSelect
                  selectedLabel={employeeLabel || null}
                  onSearch={setEmployeeQuery}
                  options={(employeeSearch.data?.items ?? []).map((e) => ({ id: e.id, label: e.name }))}
                  isLoading={employeeSearch.isLoading}
                  onSelect={(option) => {
                    setEmployeeId(option.id);
                    setEmployeeLabel(option.label);
                  }}
                  placeholder="Kişi ara…"
                />
              </div>

              <div>
                <label className="mb-1 block text-xs font-medium text-slate-500">Hafta</label>
                <div className="flex items-center gap-1.5">
                  <button
                    type="button"
                    onClick={() => setWeekOffset((prev) => prev - 1)}
                    className="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg border border-slate-200 text-slate-500 hover:bg-slate-50"
                    aria-label="Önceki hafta"
                  >
                    ‹
                  </button>
                  <div className="flex-1 rounded-lg bg-slate-50 px-3 py-1.5 text-center text-sm font-medium text-slate-700">
                    {week.label}
                  </div>
                  <button
                    type="button"
                    onClick={() => setWeekOffset((prev) => prev + 1)}
                    disabled={!isPlanned && weekOffset >= 0}
                    className="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg border border-slate-200 text-slate-500 hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-40 disabled:hover:bg-transparent"
                    aria-label="Sonraki hafta"
                    title={
                      !isPlanned && weekOffset >= 0
                        ? 'Henüz gerçekleşmemiş bir haftaya efor onayı verilemez.'
                        : undefined
                    }
                  >
                    ›
                  </button>
                </div>
                <button
                  type="button"
                  onClick={() => setWeekOffset(0)}
                  className={
                    'mt-1 text-xs font-medium text-indigo-600 hover:underline ' +
                    (weekOffset === 0 ? 'invisible' : '')
                  }
                  tabIndex={weekOffset === 0 ? -1 : undefined}
                >
                  Bu haftaya dön
                </button>
              </div>

              <div>
                <label className="mb-1 block text-xs font-medium text-slate-500">İzin ve Resmi Tatil Bilgisi</label>
                <div className="grid grid-cols-7 gap-1">
                  {weekDays.map((d) => {
                    const statusLabel = d.holidayName ? 'Tatil' : d.leave ? formatHoursLabel(d.leave.hours) : ' ';

                    return (
                      <div
                        key={d.dateKey}
                        title={
                          d.holidayName
                            ? d.holidayName
                            : d.leave
                              ? d.leave.isFullDay
                                ? `İzinli (Tam Gün) · ${formatHoursLabel(d.leave.hours)}`
                                : `İzinli: ${formatTimeShort(d.leave.startTime!)}–${formatTimeShort(d.leave.endTime!)} · ${formatHoursLabel(d.leave.hours)}`
                              : undefined
                        }
                        className={
                          'flex h-[3.1rem] flex-col justify-start rounded-md border px-1 py-1 text-center text-[11px] leading-tight ' +
                          (d.holidayName
                            ? 'border-red-300 bg-red-50 text-red-700'
                            : d.leave
                              ? 'border-violet-300 bg-violet-50 text-violet-700'
                              : 'border-slate-200 text-slate-400')
                        }
                      >
                        <div className="font-semibold">{d.weekdayLabel}</div>
                        <div>{d.dayNum}</div>
                        <div className="mt-0.5">{statusLabel}</div>
                      </div>
                    );
                  })}
                </div>
                <p className="mt-1.5 h-4 text-xs text-slate-400">
                  {!employeeId
                    ? 'Kayıtları görmek için önce bir kişi seçin.'
                    : weekHolidayCount === 0 && weekLeaveCount === 0
                      ? 'Bu hafta resmi tatil veya izin yok.'
                      : [
                          weekHolidayCount > 0 ? `${weekHolidayCount} gün resmi tatil` : null,
                          weekLeaveCount > 0 ? `${weekLeaveCount} gün izinli` : null,
                        ]
                          .filter(Boolean)
                          .join(' · ')}
                </p>
              </div>

              <div>
                <label className="mb-1 block text-xs font-medium text-slate-500">
                  Onay Açıklaması <span className="text-red-500">*</span>
                </label>
                <textarea
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  onBlur={() => setDescriptionTouched(true)}
                  placeholder="Bu onayla ilgili bir not girin…"
                  rows={3}
                  className="w-full resize-none rounded-lg border border-slate-200 px-3 py-2 text-sm"
                />
                {descriptionTouched && descriptionError && (
                  <p className="mt-1 text-xs text-red-600">{descriptionError}</p>
                )}
              </div>

              <p className="text-xs text-slate-400">
                Seçilen kişinin bu haftadaki (Pazartesi–Pazar) tüm efor kayıtları toplu olarak onaylanır ve bundan
                sonra değiştirilemez/silinemez. Onay her zaman tam bir haftayı kapsar.
              </p>

            </div>

            <div>
              <label className="mb-1 block text-xs font-medium text-slate-500">
                Bu Haftanın Kayıtları {employeeId && previewLogs.isLoading ? '(yükleniyor…)' : employeeId ? `(${previewItems.length})` : ''}
              </label>
              <div className="max-h-[26rem] overflow-y-auto rounded-lg border border-slate-200">
                {!employeeId ? (
                  <div className="p-4 text-center text-xs text-slate-400">
                    Kayıtları görmek için önce bir kişi seçin.
                  </div>
                ) : previewItems.length === 0 && !previewLogs.isLoading ? (
                  <div className="p-4 text-center text-xs text-slate-400">Bu haftada kayıt yok.</div>
                ) : (
                  <ul className="divide-y divide-slate-100">
                    {previewItems.map((log) => (
                      <li key={log.id} className="flex items-center justify-between gap-2 px-3 py-2.5 text-xs">
                        <div className="min-w-0">
                          <div className="truncate font-medium text-slate-700">{resolveProject(log.projectId)}</div>
                          <div className="truncate text-slate-400">
                            {formatDateTr(log.workDate)} · {resolveCustomer(log.customerId)} ·{' '}
                            {resolveActivity(log.activityL2Id)}
                          </div>
                        </div>
                        <div className="shrink-0 text-right">
                          <span className="font-semibold text-slate-700">{log.hours}h</span>
                          {log.isApproved && <div className="text-teal-600">🔒 onaylı</div>}
                        </div>
                      </li>
                    ))}
                  </ul>
                )}
              </div>
              {previewItems.length > 0 && (
                <div className="mt-1.5 flex justify-between text-xs text-slate-500">
                  <span>Toplam: {previewTotalHours.toFixed(1)}h</span>
                  {previewApprovedCount > 0 && (
                    <span className="text-teal-600">{previewApprovedCount} kayıt zaten onaylı</span>
                  )}
                </div>
              )}
            </div>
          </div>

          <div className="mt-6 flex justify-end gap-2">
            <button
              type="button"
              onClick={onClose}
              className="rounded-lg border border-slate-200 px-4 py-2 text-sm font-medium text-slate-600 hover:bg-slate-50"
            >
              Kapat
            </button>
            <button
              type="submit"
              disabled={!canSubmit || approveMutation.isPending}
              className="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-700 disabled:cursor-not-allowed disabled:opacity-50"
            >
              {approveMutation.isPending ? 'Onaylanıyor…' : 'Onayla'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
