import { useMemo, useState } from 'react';
import { WORK_LOG_ENTRY_TYPE, type EmployeeWorkLogDto, type WorkLogEntryType } from '../../api/types';
import { useDeleteWorkLogMutation } from '../../hooks/useDeleteWorkLogMutation';
import { useHolidays } from '../../hooks/useHolidays';
import { useEmployeeLeaves } from '../../hooks/useEmployeeLeaves';
import { WorkLogForm, type WorkLogFormInitialValues } from './WorkLogForm';

function formatTimeShort(time: string): string {
  const [hours, minutes] = time.split(':');
  return `${hours}:${minutes}`;
}

interface CellWorkLogsModalProps {
  logs: EmployeeWorkLogDto[];
  date: string;
  resolveEmployee: (id: string) => string;
  resolveProject: (id: string) => string;
  resolveCustomer: (id: string) => string;
  resolveActivity: (id: string) => string;
  addPrefill: WorkLogFormInitialValues;
  entryType?: WorkLogEntryType;
  onClose: () => void;
}

/** Hücreye tıklanınca açılan modal — Efor Onayı modalıyla aynı iki sütunlu mantık: solda her
 * zaman açık duran ekleme/düzenleme formu, sağda o tarihe ait mevcut kayıtların listesi. Bir
 * kaydın "Düzenle"sine basmak formu o kaydın verileriyle doldurur; kaydet/vazgeç sonrası form
 * yeniden "yeni kayıt" durumuna döner — modalın kendisi kapanmaz, böylece art arda birden fazla
 * küçük kayıt (ör. bir günü 15dk-2h'lik birkaç parçaya bölerek) girmek tek modalde mümkün olur. */
export function CellWorkLogsModal({
  logs,
  date,
  resolveEmployee,
  resolveProject,
  resolveCustomer,
  resolveActivity,
  addPrefill,
  entryType = WORK_LOG_ENTRY_TYPE.Actual,
  onClose,
}: CellWorkLogsModalProps) {
  const [editingLog, setEditingLog] = useState<EmployeeWorkLogDto | null>(null);
  const deleteMutation = useDeleteWorkLogMutation();

  const holidays = useHolidays();
  const employeeLeaves = useEmployeeLeaves();
  const holiday = holidays.data?.items.find((h) => h.date === date) ?? null;

  // Onay formundaki İzin/Tatil paneliyle aynı mantık: bu hücredeki kayıtlara ait çalışanlardan
  // hangileri bu tarihte izinliymiş, listelenir (birden fazla çalışan olabilir — ör. Proje'ye
  // göre gruplanmış bir hücre).
  const leavesForDate = useMemo(() => {
    const distinctEmployeeIds = [...new Set(logs.map((l) => l.employeeId))];
    return (employeeLeaves.data?.items ?? []).filter(
      (l) => distinctEmployeeIds.includes(l.employeeId) && date >= l.startDate && date <= l.endDate,
    );
  }, [logs, employeeLeaves.data, date]);

  const handleDelete = async (logId: string) => {
    if (!window.confirm('Bu efor kaydını silmek istediğinize emin misiniz?')) return;
    await deleteMutation.mutateAsync(logId);
    if (editingLog?.id === logId) setEditingLog(null);
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/40 p-4">
      <div className="w-full max-w-5xl rounded-xl bg-white p-6 shadow-xl">
        <div className="mb-4 flex items-center justify-between">
          <h2 className="text-lg font-semibold text-slate-800">{date} Tarihli Kayıtlar</h2>
          <button type="button" onClick={onClose} className="text-slate-400 hover:text-slate-600">
            ✕
          </button>
        </div>

        {(holiday || leavesForDate.length > 0) && (
          <div className="mb-4 space-y-1.5">
            {holiday && (
              <div className="flex items-center gap-2 rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-xs font-medium text-red-700">
                <span>🔴</span>
                <span>Resmi Tatil: {holiday.name}</span>
              </div>
            )}
            {leavesForDate.map((leave) => (
              <div
                key={leave.id}
                className="flex items-center gap-2 rounded-lg border border-violet-200 bg-violet-50 px-3 py-2 text-xs font-medium text-violet-700"
              >
                <span>🟣</span>
                <span>
                  {resolveEmployee(leave.employeeId)} —{' '}
                  {leave.isFullDay
                    ? 'İzinli (Tam Gün)'
                    : `İzinli: ${formatTimeShort(leave.startTime!)}–${formatTimeShort(leave.endTime!)}`}
                </span>
              </div>
            ))}
          </div>
        )}

        <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
          <div>
            <label className="mb-1 block text-xs font-medium text-slate-500">
              {editingLog ? 'Kaydı Düzenle' : 'Yeni Kayıt Ekle'}
            </label>
            <WorkLogForm
              key={editingLog ? `edit-${editingLog.id}` : 'create'}
              mode={editingLog ? 'edit' : 'create'}
              workLogId={editingLog?.id}
              allowDateRange={false}
              initialValues={
                editingLog
                  ? {
                      employeeId: editingLog.employeeId,
                      employeeLabel: resolveEmployee(editingLog.employeeId),
                      projectId: editingLog.projectId,
                      projectLabel: resolveProject(editingLog.projectId),
                      customerId: editingLog.customerId,
                      customerLabel: resolveCustomer(editingLog.customerId),
                      activityL1Id: editingLog.activityL1Id,
                      activityL2Id: editingLog.activityL2Id,
                      date: editingLog.workDate,
                      hours: editingLog.hours,
                      description: editingLog.description,
                    }
                  : { ...addPrefill, date }
              }
              entryType={entryType}
              cancelLabel={editingLog ? 'Vazgeç' : 'Temizle'}
              onClose={() => setEditingLog(null)}
              onDeleted={() => setEditingLog(null)}
            />
          </div>

          <div>
            <label className="mb-1 block text-xs font-medium text-slate-500">
              Bu Tarihteki Kayıtlar ({logs.length})
            </label>
            <div className="max-h-[34rem] space-y-2 overflow-y-auto rounded-lg border border-slate-200 p-2">
              {logs.length === 0 ? (
                <div className="p-4 text-center text-xs text-slate-400">Bu tarihte kayıt yok.</div>
              ) : (
                logs.map((log) => (
                  <div
                    key={log.id}
                    className={
                      'rounded-lg border p-3 text-sm ' +
                      (editingLog?.id === log.id ? 'border-indigo-300 bg-indigo-50/50' : 'border-slate-200')
                    }
                  >
                    <div className="flex items-start justify-between">
                      <div>
                        <div className="flex items-center gap-1.5">
                          <div className="font-medium text-slate-800">{resolveProject(log.projectId)}</div>
                          {log.isApproved && (
                            <span className="rounded-full bg-emerald-50 px-1.5 py-0.5 text-[10px] font-semibold text-emerald-700">
                              🔒 Onaylı
                            </span>
                          )}
                        </div>
                        <div className="text-xs text-slate-500">{resolveCustomer(log.customerId)}</div>
                        <div className="text-xs text-slate-500">
                          {resolveActivity(log.activityL1Id)} / {resolveActivity(log.activityL2Id)}
                        </div>
                        <div className="text-xs text-slate-400">{resolveEmployee(log.employeeId)}</div>
                        <div className="mt-1 text-xs text-slate-500">{log.description}</div>
                      </div>
                      <div className="text-right">
                        <div className="font-semibold text-slate-800">{log.hours}h</div>
                      </div>
                    </div>
                    <div className="mt-2 flex justify-end gap-2">
                      {log.isApproved ? (
                        <span className="px-2 py-1 text-xs text-slate-400">Onaylandığı için düzenlenemez/silinemez</span>
                      ) : (
                        <>
                          <button
                            type="button"
                            onClick={() => setEditingLog(log)}
                            className="rounded border border-slate-200 px-2 py-1 text-xs font-medium text-slate-600 hover:bg-slate-50"
                          >
                            Düzenle
                          </button>
                          <button
                            type="button"
                            onClick={() => handleDelete(log.id)}
                            disabled={deleteMutation.isPending}
                            className="rounded border border-red-200 px-2 py-1 text-xs font-medium text-red-600 hover:bg-red-50 disabled:opacity-50"
                          >
                            Sil
                          </button>
                        </>
                      )}
                    </div>
                  </div>
                ))
              )}
            </div>
            {logs.length > 0 && (
              <div className="mt-1.5 text-xs text-slate-500">
                Toplam: {logs.reduce((sum, l) => sum + l.hours, 0).toFixed(2)}h
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
