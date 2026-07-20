import { useState } from 'react';
import type { EmployeeWorkLogDto } from '../../api/types';
import { useDeleteWorkLogMutation } from '../../hooks/useDeleteWorkLogMutation';
import { WorkLogFormModal, type WorkLogFormInitialValues } from './WorkLogFormModal';

interface CellWorkLogsModalProps {
  logs: EmployeeWorkLogDto[];
  date: string;
  resolveEmployee: (id: string) => string;
  resolveProject: (id: string) => string;
  resolveCustomer: (id: string) => string;
  resolveActivity: (id: string) => string;
  addPrefill: WorkLogFormInitialValues;
  onClose: () => void;
}

export function CellWorkLogsModal({
  logs,
  date,
  resolveEmployee,
  resolveProject,
  resolveCustomer,
  resolveActivity,
  addPrefill,
  onClose,
}: CellWorkLogsModalProps) {
  const [editingLog, setEditingLog] = useState<EmployeeWorkLogDto | null>(null);
  const [isAdding, setIsAdding] = useState(false);
  const deleteMutation = useDeleteWorkLogMutation();

  const handleDelete = async (logId: string) => {
    if (!window.confirm('Bu efor kaydını silmek istediğinize emin misiniz?')) return;
    await deleteMutation.mutateAsync(logId);
    if (logs.length <= 1) onClose();
  };

  if (isAdding) {
    return (
      <WorkLogFormModal
        mode="create"
        allowDateRange={false}
        initialValues={{ ...addPrefill, date }}
        onClose={onClose}
      />
    );
  }

  if (editingLog) {
    return (
      <WorkLogFormModal
        mode="edit"
        workLogId={editingLog.id}
        initialValues={{
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
        }}
        onClose={onClose}
        onDeleted={onClose}
      />
    );
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/40 p-4">
      <div className="w-full max-w-4xl rounded-xl bg-white p-6 shadow-xl">
        <div className="mb-4 flex items-center justify-between">
          <h2 className="text-lg font-semibold text-slate-800">{date} Tarihli Kayıtlar</h2>
          <button type="button" onClick={onClose} className="text-slate-400 hover:text-slate-600">
            ✕
          </button>
        </div>

        <div className="max-h-[40rem] space-y-2 overflow-y-auto">
          {logs.map((log) => (
            <div key={log.id} className="rounded-lg border border-slate-200 p-3 text-sm">
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
          ))}
        </div>

        <button
          type="button"
          onClick={() => setIsAdding(true)}
          className="mt-4 w-full rounded-lg border border-dashed border-indigo-300 py-2 text-sm font-semibold text-indigo-600 hover:bg-indigo-50"
        >
          + Ekle
        </button>
      </div>
    </div>
  );
}
