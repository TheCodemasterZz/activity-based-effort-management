import { useMemo, useState } from 'react';
import { AsyncSearchSelect } from '../common/AsyncSearchSelect';
import { useEmployeeSearch } from '../../hooks/useEmployees';
import { useProjectSearch } from '../../hooks/useProjects';
import { useSubActivities, useTopLevelActivities } from '../../hooks/useActivities';
import { useHolidays } from '../../hooks/useHolidays';
import { useLogWorkMutation } from '../../hooks/useLogWorkMutation';
import { useUpdateWorkLogMutation } from '../../hooks/useUpdateWorkLogMutation';
import { useDeleteWorkLogMutation } from '../../hooks/useDeleteWorkLogMutation';
import { ApiError } from '../../api/client';
import { getEmployeeById } from '../../api/employees';
import { getWorkCalendarById } from '../../api/workCalendars';
import { findOvertimeDates } from '../../lib/overtimeCheck';
import { formatDuration, parseDuration } from '../../lib/duration';
import { pushSuccessNotification } from '../../lib/notifications';
import { WORK_LOG_ENTRY_TYPE, type WorkLogEntryType } from '../../api/types';

export interface WorkLogFormInitialValues {
  employeeId?: string;
  employeeLabel?: string;
  projectId?: string;
  projectLabel?: string;
  activityL1Id?: string;
  activityL2Id?: string;
  date?: string;
  endDate?: string;
  hours?: number;
  description?: string;
}

export interface WorkLogFormProps {
  mode: 'create' | 'edit';
  workLogId?: string;
  allowDateRange?: boolean;
  initialValues?: WorkLogFormInitialValues;
  /** Gerçekleşen (Log Work, varsayılan) mı yoksa planlanan (Plan Work) bir kayıt mı — sadece
   * create sırasında API'ye gönderilir (kayıt oluştuktan sonra türü değişmez) ve gelecek
   * tarih kısıtını (Planned için serbest) belirler. */
  entryType?: WorkLogEntryType;
  /** Başarılı kayıt/güncelleme sonrası ve "Vazgeç" ile çağrılır. Bağımsız bir modalde (bkz.
   * WorkLogFormModal) modalı kapatır; hücre modalı gibi iki sütunlu bir yerleşimde form yeniden
   * "yeni kayıt" durumuna dönerken üst modal açık kalabilir — anlamı çağıran taraf belirler. */
  onClose: () => void;
  onDeleted?: () => void;
  /** Alt kısımdaki "Vazgeç" butonunun metni — gömülü kullanımda "Yeni Kayıt" gibi bağlama daha
   * uygun bir metin geçirilebilir. */
  cancelLabel?: string;
}

type FieldName = 'employee' | 'project' | 'activityL1' | 'activityL2' | 'date' | 'endDate' | 'hours' | 'description';

function todayIso(): string {
  return new Date().toISOString().slice(0, 10);
}

function formatDateTr(date: string): string {
  return new Date(`${date}T00:00:00`).toLocaleDateString('tr-TR');
}

function localDateKey(d: Date): string {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}

function eachDateKey(startKey: string, endKey: string): string[] {
  const keys: string[] = [];
  let d = new Date(`${startKey}T00:00:00`);
  const end = new Date(`${endKey}T00:00:00`);
  while (d <= end) {
    keys.push(localDateKey(d));
    d = new Date(d.getTime() + 86400000);
  }
  return keys;
}

function isWeekendKey(dateKeyValue: string): boolean {
  const day = new Date(`${dateKeyValue}T00:00:00`).getDay();
  return day === 0 || day === 6;
}

/** Work log ekleme/düzenleme formunun kendisi — kendi overlay/başlığını çizmez, çağıran taraf
 * (bağımsız bir modal ya da hücre modalındaki iki sütunlu yerleşimin bir sütunu) etrafını sarar. */
export function WorkLogForm({
  mode,
  workLogId,
  allowDateRange = true,
  initialValues,
  entryType = WORK_LOG_ENTRY_TYPE.Actual,
  onClose,
  onDeleted,
  cancelLabel = 'Vazgeç',
}: WorkLogFormProps) {
  const isPlanned = entryType === WORK_LOG_ENTRY_TYPE.Planned;
  const [employeeId, setEmployeeId] = useState(initialValues?.employeeId ?? '');
  const [employeeLabel, setEmployeeLabel] = useState(initialValues?.employeeLabel ?? '');
  const [projectId, setProjectId] = useState(initialValues?.projectId ?? '');
  const [projectLabel, setProjectLabel] = useState(initialValues?.projectLabel ?? '');
  const [activityL1Id, setActivityL1Id] = useState(initialValues?.activityL1Id ?? '');
  const [activityL2Id, setActivityL2Id] = useState(initialValues?.activityL2Id ?? '');
  const [description, setDescription] = useState(initialValues?.description ?? '');

  const hasInitialRange = !!(initialValues?.endDate && initialValues.endDate !== initialValues?.date);
  const [isRange, setIsRange] = useState(hasInitialRange);
  const [startDate, setStartDate] = useState(initialValues?.date ?? todayIso());
  const [endDate, setEndDate] = useState(initialValues?.endDate ?? initialValues?.date ?? todayIso());
  const [hoursText, setHoursText] = useState(
    initialValues?.hours !== undefined ? formatDuration(initialValues.hours) : '',
  );
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isCheckingOvertime, setIsCheckingOvertime] = useState(false);
  const [touched, setTouched] = useState<Partial<Record<FieldName, boolean>>>({});

  const markTouched = (field: FieldName) => setTouched((prev) => ({ ...prev, [field]: true }));

  const [employeeQuery, setEmployeeQuery] = useState('');
  const employeeSearch = useEmployeeSearch(employeeQuery);

  const [projectQuery, setProjectQuery] = useState('');
  const projectSearch = useProjectSearch(projectQuery, employeeId || null);

  const topLevelActivities = useTopLevelActivities();
  const subActivities = useSubActivities(activityL1Id || null);
  const holidays = useHolidays();

  const logWorkMutation = useLogWorkMutation();
  const updateMutation = useUpdateWorkLogMutation();
  const deleteMutation = useDeleteWorkLogMutation();

  const isPending =
    logWorkMutation.isPending || updateMutation.isPending || deleteMutation.isPending || isCheckingOvertime;

  const isCreateRange = mode === 'create' && allowDateRange && isRange;
  const parsedHours = parseDuration(hoursText);

  const fieldErrors: Partial<Record<FieldName, string>> = {
    employee: employeeId ? undefined : 'Kişi seçilmeli.',
    project: projectId ? undefined : 'Proje seçilmeli.',
    activityL1: activityL1Id ? undefined : 'Activity L1 seçilmeli.',
    activityL2: activityL2Id ? undefined : 'Activity L2 seçilmeli.',
    date: startDate ? undefined : 'Tarih seçilmeli.',
    endDate: isCreateRange && endDate < startDate ? 'Bitiş tarihi başlangıçtan önce olamaz.' : undefined,
    hours:
      hoursText.trim().length === 0
        ? 'Saat girilmeli.'
        : parsedHours === null
          ? 'Geçersiz süre biçimi (ör. 1h 30m, 2h, 45m).'
          : parsedHours <= 0
            ? 'Saat 0’dan büyük olmalı.'
            : parsedHours > 24
              ? 'Saat 24’ten büyük olamaz.'
              : undefined,
    description: description.trim().length === 0 ? 'Açıklama girilmeli.' : undefined,
  };

  const showError = (field: FieldName) => (touched[field] ? fieldErrors[field] : undefined);

  const canSubmit = Object.values(fieldErrors).every((e) => !e);

  const holidayDateKeys = useMemo(
    () => new Set(holidays.data?.items.map((h) => h.date) ?? []),
    [holidays.data],
  );

  const flaggedDateKeys = useMemo(() => {
    if (!startDate) return [];
    const rangeEnd = isCreateRange ? endDate : startDate;
    if (!rangeEnd || rangeEnd < startDate) return [];
    return eachDateKey(startDate, rangeEnd).filter((d) => isWeekendKey(d) || holidayDateKeys.has(d));
  }, [startDate, endDate, isCreateRange, holidayDateKeys]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMessage(null);
    setTouched({
      employee: true,
      project: true,
      activityL1: true,
      activityL2: true,
      date: true,
      endDate: true,
      hours: true,
      description: true,
    });
    if (!canSubmit) return;

    const hours = parsedHours as number;
    const rangeStart = startDate;
    const rangeEnd = isRange ? endDate : startDate;

    try {
      setIsCheckingOvertime(true);
      const employee = await getEmployeeById(employeeId);
      const calendar = await getWorkCalendarById(employee.workCalendarId);
      const exceededDates = await findOvertimeDates({
        employeeId,
        calendar,
        startDate: rangeStart,
        endDate: rangeEnd,
        hoursPerDay: hours,
        excludeWorkLogId: mode === 'edit' ? workLogId : undefined,
        entryType,
      });
      setIsCheckingOvertime(false);

      if (exceededDates.length > 0) {
        const formatted = exceededDates.map(formatDateTr).join(', ');
        const confirmed = window.confirm(
          `Şu tarih(ler)de çalışanın mesai takvimini aşan bir kayıt giriyorsunuz: ${formatted}. Yine de eklemek istiyor musunuz?`,
        );
        if (!confirmed) return;
      }
    } catch {
      setIsCheckingOvertime(false);
      // Mesai takvimi kontrolü başarısız olsa bile (ör. ağ hatası) kaydın kendisi engellenmez.
    }

    try {
      if (mode === 'edit' && workLogId) {
        await updateMutation.mutateAsync({
          id: workLogId,
          payload: {
            employeeId,
            projectId,
            activityL1Id,
            activityL2Id,
            workDate: startDate,
            hours,
            description: description.trim(),
          },
        });
      } else {
        await logWorkMutation.mutateAsync({
          employeeId,
          projectId,
          activityL1Id,
          activityL2Id,
          startDate,
          endDate: rangeEnd,
          hours,
          description: description.trim(),
          entryType,
        });
      }
      pushSuccessNotification(mode === 'edit' ? 'Kayıt güncellendi.' : 'İşlem başarıyla yapılmıştır.');
      onClose();
    } catch (err) {
      setErrorMessage(err instanceof ApiError ? err.message : 'Beklenmeyen bir hata oluştu.');
    }
  };

  const handleDelete = async () => {
    if (!workLogId) return;
    setErrorMessage(null);
    try {
      await deleteMutation.mutateAsync(workLogId);
      onDeleted?.();
      onClose();
    } catch (err) {
      setErrorMessage(err instanceof ApiError ? err.message : 'Beklenmeyen bir hata oluştu.');
    }
  };

  return (
    <>
      {flaggedDateKeys.length > 0 && (
        <div className="mb-4 rounded-lg bg-red-50 px-3 py-2 text-xs font-medium text-red-700">
          ⚠ {flaggedDateKeys.length === 1
            ? formatDateTr(flaggedDateKeys[0])
            : `${flaggedDateKeys.length} gün`}{' '}
          hafta sonu veya resmi tatile denk geliyor.
        </div>
      )}

      <form className="space-y-3" onSubmit={handleSubmit}>
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
              setProjectId('');
              setProjectLabel('');
              markTouched('employee');
            }}
            placeholder="Kişi ara…"
          />
          {showError('employee') && <p className="mt-1 text-xs text-red-600">{showError('employee')}</p>}
        </div>

        <div>
          <label className="mb-1 block text-xs font-medium text-slate-500">
            Proje <span className="text-red-500">*</span>
          </label>
          <AsyncSearchSelect
            selectedLabel={projectLabel || null}
            onSearch={setProjectQuery}
            options={(projectSearch.data?.items ?? []).map((p) => ({ id: p.id, label: p.name }))}
            isLoading={projectSearch.isLoading}
            onSelect={(option) => {
              setProjectId(option.id);
              setProjectLabel(option.label);
              markTouched('project');
            }}
            placeholder="Proje ara…"
            disabled={!employeeId}
            disabledMessage="Önce kişi seçin"
          />
          {showError('project') && <p className="mt-1 text-xs text-red-600">{showError('project')}</p>}
        </div>

        <div>
          <label className="mb-1 block text-xs font-medium text-slate-500">
            Activity L1 <span className="text-red-500">*</span>
          </label>
          <select
            value={activityL1Id}
            onChange={(e) => {
              setActivityL1Id(e.target.value);
              setActivityL2Id('');
            }}
            onBlur={() => markTouched('activityL1')}
            className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm"
          >
            <option value="">Seçiniz</option>
            {topLevelActivities.data?.items.map((activity) => (
              <option key={activity.id} value={activity.id}>
                {activity.name}
              </option>
            ))}
          </select>
          {showError('activityL1') && <p className="mt-1 text-xs text-red-600">{showError('activityL1')}</p>}
        </div>

        <div>
          <label className="mb-1 block text-xs font-medium text-slate-500">
            Activity L2 <span className="text-red-500">*</span>
          </label>
          <select
            value={activityL2Id}
            onChange={(e) => setActivityL2Id(e.target.value)}
            onBlur={() => markTouched('activityL2')}
            className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm disabled:bg-slate-50"
            disabled={!activityL1Id}
          >
            <option value="">Seçiniz</option>
            {subActivities.data?.items.map((activity) => (
              <option key={activity.id} value={activity.id}>
                {activity.name}
              </option>
            ))}
          </select>
          {showError('activityL2') && <p className="mt-1 text-xs text-red-600">{showError('activityL2')}</p>}
        </div>

        {mode === 'create' && allowDateRange && (
          <label className="flex items-center gap-2 text-xs font-medium text-slate-500">
            <input type="checkbox" checked={isRange} onChange={(e) => setIsRange(e.target.checked)} />
            Tarih aralığı gir
          </label>
        )}

        <div className="grid grid-cols-2 gap-3">
          <div>
            <label className="mb-1 block text-xs font-medium text-slate-500">
              {isRange && mode === 'create' ? 'Başlangıç' : 'Tarih'} <span className="text-red-500">*</span>
            </label>
            <input
              type="date"
              value={startDate}
              max={isPlanned ? undefined : todayIso()}
              onChange={(e) => setStartDate(e.target.value)}
              onBlur={() => markTouched('date')}
              className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm"
            />
            {showError('date') && <p className="mt-1 text-xs text-red-600">{showError('date')}</p>}
          </div>
          {mode === 'create' && allowDateRange && isRange && (
            <div>
              <label className="mb-1 block text-xs font-medium text-slate-500">
                Bitiş <span className="text-red-500">*</span>
              </label>
              <input
                type="date"
                value={endDate}
                min={startDate}
                max={isPlanned ? undefined : todayIso()}
                onChange={(e) => setEndDate(e.target.value)}
                onBlur={() => markTouched('endDate')}
                className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm"
              />
              {showError('endDate') && <p className="mt-1 text-xs text-red-600">{showError('endDate')}</p>}
            </div>
          )}
        </div>

        <div>
          <label className="mb-1 block text-xs font-medium text-slate-500">
            Saat <span className="text-red-500">*</span>
          </label>
          <input
            type="text"
            value={hoursText}
            onChange={(e) => setHoursText(e.target.value)}
            onBlur={() => markTouched('hours')}
            placeholder="ör. 1h 30m, 2h, 45m"
            className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm"
          />
          {showError('hours') && <p className="mt-1 text-xs text-red-600">{showError('hours')}</p>}
        </div>

        <div>
          <label className="mb-1 block text-xs font-medium text-slate-500">
            Açıklama <span className="text-red-500">*</span>
          </label>
          <textarea
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            onBlur={() => markTouched('description')}
            placeholder="Bu efor kaydıyla ilgili kısa bir açıklama girin…"
            rows={3}
            className="w-full resize-none rounded-lg border border-slate-200 px-3 py-2 text-sm"
          />
          {showError('description') && <p className="mt-1 text-xs text-red-600">{showError('description')}</p>}
        </div>

        {errorMessage && <p className="text-sm text-red-600">{errorMessage}</p>}

        <div className="mt-4 flex items-center justify-between">
          {mode === 'edit' ? (
            <button
              type="button"
              onClick={handleDelete}
              disabled={isPending}
              className="rounded-lg border border-red-200 px-4 py-2 text-sm font-medium text-red-600 hover:bg-red-50 disabled:opacity-50"
            >
              Sil
            </button>
          ) : (
            <span />
          )}
          <div className="flex gap-2">
            <button
              type="button"
              onClick={onClose}
              className="rounded-lg border border-slate-200 px-4 py-2 text-sm font-medium text-slate-600 hover:bg-slate-50"
            >
              {cancelLabel}
            </button>
            <button
              type="submit"
              disabled={isPending}
              className="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-700 disabled:cursor-not-allowed disabled:opacity-50"
            >
              {isCheckingOvertime ? 'Kontrol ediliyor…' : isPending ? 'Kaydediliyor…' : 'Kaydet'}
            </button>
          </div>
        </div>
      </form>
    </>
  );
}
