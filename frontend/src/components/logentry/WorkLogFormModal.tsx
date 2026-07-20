import { useState } from 'react';
import { AsyncSearchSelect } from '../common/AsyncSearchSelect';
import { useEmployeeSearch } from '../../hooks/useEmployees';
import { useProjectSearch } from '../../hooks/useProjects';
import { useCustomerSearch } from '../../hooks/useCustomers';
import { useSubActivities, useTopLevelActivities } from '../../hooks/useActivities';
import { useLogWorkMutation } from '../../hooks/useLogWorkMutation';
import { useUpdateWorkLogMutation } from '../../hooks/useUpdateWorkLogMutation';
import { useDeleteWorkLogMutation } from '../../hooks/useDeleteWorkLogMutation';
import { ApiError } from '../../api/client';
import { getEmployeeById } from '../../api/employees';
import { getWorkCalendarById } from '../../api/workCalendars';
import { findOvertimeDates } from '../../lib/overtimeCheck';
import { formatDuration, parseDuration } from '../../lib/duration';

export interface WorkLogFormInitialValues {
  employeeId?: string;
  employeeLabel?: string;
  projectId?: string;
  projectLabel?: string;
  customerId?: string;
  customerLabel?: string;
  activityL1Id?: string;
  activityL2Id?: string;
  date?: string;
  endDate?: string;
  hours?: number;
  description?: string;
}

interface WorkLogFormModalProps {
  mode: 'create' | 'edit';
  workLogId?: string;
  allowDateRange?: boolean;
  initialValues?: WorkLogFormInitialValues;
  onClose: () => void;
  onDeleted?: () => void;
}

function todayIso(): string {
  return new Date().toISOString().slice(0, 10);
}

function formatDateTr(date: string): string {
  return new Date(`${date}T00:00:00`).toLocaleDateString('tr-TR');
}

export function WorkLogFormModal({
  mode,
  workLogId,
  allowDateRange = true,
  initialValues,
  onClose,
  onDeleted,
}: WorkLogFormModalProps) {
  const [employeeId, setEmployeeId] = useState(initialValues?.employeeId ?? '');
  const [employeeLabel, setEmployeeLabel] = useState(initialValues?.employeeLabel ?? '');
  const [projectId, setProjectId] = useState(initialValues?.projectId ?? '');
  const [projectLabel, setProjectLabel] = useState(initialValues?.projectLabel ?? '');
  const [customerId, setCustomerId] = useState(initialValues?.customerId ?? '');
  const [customerLabel, setCustomerLabel] = useState(initialValues?.customerLabel ?? '');
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

  const [employeeQuery, setEmployeeQuery] = useState('');
  const employeeSearch = useEmployeeSearch(employeeQuery);

  const [projectQuery, setProjectQuery] = useState('');
  const projectSearch = useProjectSearch(projectQuery, employeeId || null);

  const [customerQuery, setCustomerQuery] = useState('');
  const customerSearch = useCustomerSearch(customerQuery, projectId || null);

  const topLevelActivities = useTopLevelActivities();
  const subActivities = useSubActivities(activityL1Id || null);

  const logWorkMutation = useLogWorkMutation();
  const updateMutation = useUpdateWorkLogMutation();
  const deleteMutation = useDeleteWorkLogMutation();

  const isPending =
    logWorkMutation.isPending || updateMutation.isPending || deleteMutation.isPending || isCheckingOvertime;

  const canSubmit =
    employeeId &&
    projectId &&
    customerId &&
    activityL1Id &&
    activityL2Id &&
    description.trim().length > 0 &&
    startDate &&
    (!isRange || endDate >= startDate) &&
    parseDuration(hoursText) !== null &&
    (parseDuration(hoursText) as number) > 0;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMessage(null);

    const hours = parseDuration(hoursText);
    if (hours === null || hours <= 0) {
      setErrorMessage('Saat alanı geçersiz. Örnek: "1h 30m", "2h" veya "45m".');
      return;
    }

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
            customerId,
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
          customerId,
          activityL1Id,
          activityL2Id,
          startDate,
          endDate: rangeEnd,
          hours,
          description: description.trim(),
        });
      }
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
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/40 p-4">
      <div className="w-full max-w-md rounded-xl bg-white p-6 shadow-xl">
        <div className="mb-4 flex items-center justify-between">
          <h2 className="text-lg font-semibold text-slate-800">
            {mode === 'edit' ? 'Work Log Düzenle' : 'Work Log Ekle'}
          </h2>
          <button type="button" onClick={onClose} className="text-slate-400 hover:text-slate-600">
            ✕
          </button>
        </div>

        <form className="space-y-3" onSubmit={handleSubmit}>
          <div>
            <label className="mb-1 block text-xs font-medium text-slate-500">Kişi</label>
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
                setCustomerId('');
                setCustomerLabel('');
              }}
              placeholder="Kişi ara…"
            />
          </div>

          <div>
            <label className="mb-1 block text-xs font-medium text-slate-500">Proje</label>
            <AsyncSearchSelect
              selectedLabel={projectLabel || null}
              onSearch={setProjectQuery}
              options={(projectSearch.data?.items ?? []).map((p) => ({ id: p.id, label: p.name }))}
              isLoading={projectSearch.isLoading}
              onSelect={(option) => {
                setProjectId(option.id);
                setProjectLabel(option.label);
                setCustomerId('');
                setCustomerLabel('');
              }}
              placeholder="Proje ara…"
              disabled={!employeeId}
              disabledMessage="Önce kişi seçin"
            />
          </div>

          <div>
            <label className="mb-1 block text-xs font-medium text-slate-500">Müşteri</label>
            <AsyncSearchSelect
              selectedLabel={customerLabel || null}
              onSearch={setCustomerQuery}
              options={(customerSearch.data?.items ?? []).map((c) => ({ id: c.id, label: c.name }))}
              isLoading={customerSearch.isLoading}
              onSelect={(option) => {
                setCustomerId(option.id);
                setCustomerLabel(option.label);
              }}
              placeholder="Müşteri ara…"
              disabled={!projectId}
              disabledMessage="Önce proje seçin"
            />
          </div>

          <div>
            <label className="mb-1 block text-xs font-medium text-slate-500">Activity L1</label>
            <select
              value={activityL1Id}
              onChange={(e) => {
                setActivityL1Id(e.target.value);
                setActivityL2Id('');
              }}
              className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm"
              required
            >
              <option value="">Seçiniz</option>
              {topLevelActivities.data?.items.map((activity) => (
                <option key={activity.id} value={activity.id}>
                  {activity.name}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label className="mb-1 block text-xs font-medium text-slate-500">Activity L2</label>
            <select
              value={activityL2Id}
              onChange={(e) => setActivityL2Id(e.target.value)}
              className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm disabled:bg-slate-50"
              disabled={!activityL1Id}
              required
            >
              <option value="">Seçiniz</option>
              {subActivities.data?.items.map((activity) => (
                <option key={activity.id} value={activity.id}>
                  {activity.name}
                </option>
              ))}
            </select>
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
                {isRange && mode === 'create' ? 'Başlangıç' : 'Tarih'}
              </label>
              <input
                type="date"
                value={startDate}
                max={todayIso()}
                onChange={(e) => setStartDate(e.target.value)}
                className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm"
                required
              />
            </div>
            {mode === 'create' && allowDateRange && isRange && (
              <div>
                <label className="mb-1 block text-xs font-medium text-slate-500">Bitiş</label>
                <input
                  type="date"
                  value={endDate}
                  min={startDate}
                  max={todayIso()}
                  onChange={(e) => setEndDate(e.target.value)}
                  className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm"
                  required
                />
              </div>
            )}
          </div>

          <div>
            <label className="mb-1 block text-xs font-medium text-slate-500">Saat</label>
            <input
              type="text"
              value={hoursText}
              onChange={(e) => setHoursText(e.target.value)}
              placeholder="ör. 1h 30m, 2h, 45m"
              className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm"
              required
            />
          </div>

          <div>
            <label className="mb-1 block text-xs font-medium text-slate-500">Açıklama</label>
            <textarea
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Bu efor kaydıyla ilgili kısa bir açıklama girin…"
              rows={3}
              className="w-full resize-none rounded-lg border border-slate-200 px-3 py-2 text-sm"
              required
            />
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
                Vazgeç
              </button>
              <button
                type="submit"
                disabled={!canSubmit || isPending}
                className="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-700 disabled:cursor-not-allowed disabled:opacity-50"
              >
                {isCheckingOvertime ? 'Kontrol ediliyor…' : isPending ? 'Kaydediliyor…' : 'Kaydet'}
              </button>
            </div>
          </div>
        </form>
      </div>
    </div>
  );
}
