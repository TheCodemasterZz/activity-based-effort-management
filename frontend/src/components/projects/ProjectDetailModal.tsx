import { useState } from 'react';
import { useProjectDetail } from '../../hooks/useProjects';
import { useProjectTasks } from '../../hooks/useProjectTasks';
import { useDeleteProjectTaskMutation, useUpdateProjectTaskStatusMutation } from '../../hooks/useProjectTaskMutations';
import { computeProjectEvmSummary, spiHealthTier } from '../../lib/projectSpi';
import { PROJECT_TASK_STATUS, type ProjectTaskDto } from '../../api/types';
import { ProjectTaskFormModal } from './ProjectTaskFormModal';

interface ProjectDetailModalProps {
  projectId: string;
  onClose: () => void;
}

const STATUS_LABEL: Record<string, { label: string; className: string }> = {
  Active: { label: 'Aktif', className: 'bg-emerald-50 text-emerald-700' },
  Completed: { label: 'Tamamlandı', className: 'bg-slate-100 text-slate-600' },
  Cancelled: { label: 'İptal Edildi', className: 'bg-red-50 text-red-600' },
};

const HEALTH_LABEL: Record<string, { label: string; className: string }> = {
  OnTrack: { label: 'ON TRACK', className: 'bg-emerald-500 text-white' },
  AtRisk: { label: 'AT RISK', className: 'bg-red-500 text-white' },
  NeedsHelp: { label: 'NEEDS HELP', className: 'bg-amber-500 text-white' },
};

const TASK_STATUS_LABEL: Record<string, { label: string; className: string }> = {
  NotStarted: { label: 'Başlamadı', className: 'bg-slate-100 text-slate-500' },
  InProgress: { label: 'Devam Ediyor', className: 'bg-amber-100 text-amber-700' },
  Done: { label: 'Bitti', className: 'bg-emerald-100 text-emerald-700' },
};

const SPI_TIER_CLASS: Record<string, string> = {
  good: 'text-emerald-600',
  warning: 'text-amber-600',
  critical: 'text-red-600',
  unknown: 'text-slate-400',
};

function formatDateTr(date: string | null): string {
  if (!date) return '—';
  return new Date(`${date}T00:00:00`).toLocaleDateString('tr-TR');
}

function TaskRow({ task, onEdit }: { task: ProjectTaskDto; onEdit: () => void }) {
  const statusMutation = useUpdateProjectTaskStatusMutation();
  const deleteMutation = useDeleteProjectTaskMutation();
  const statusInfo = TASK_STATUS_LABEL[task.status] ?? { label: task.status, className: 'bg-slate-100 text-slate-500' };

  const handleDelete = async () => {
    if (!window.confirm(`"${task.name}" görevini silmek istediğinize emin misiniz?`)) return;
    await deleteMutation.mutateAsync(task.id);
  };

  return (
    <div className="flex items-center justify-between gap-2 rounded-lg border border-slate-100 px-3 py-2 text-sm">
      <div className="min-w-0">
        <div className="flex items-center gap-1.5">
          {task.isMilestone && <span title="Kilometre taşı">◆</span>}
          <span className="truncate font-medium text-slate-700">{task.name}</span>
        </div>
        <div className="text-xs text-slate-400">
          {formatDateTr(task.startDate)} – {formatDateTr(task.endDate)} · {task.estimatedEffortHours}h
        </div>
      </div>
      <div className="flex shrink-0 items-center gap-1.5">
        <select
          value={task.status}
          onChange={(e) => statusMutation.mutate({ id: task.id, status: PROJECT_TASK_STATUS[e.target.value as keyof typeof PROJECT_TASK_STATUS] })}
          className={`rounded-full border-0 px-2 py-0.5 text-xs font-medium ${statusInfo.className}`}
        >
          <option value="NotStarted">Başlamadı</option>
          <option value="InProgress">Devam Ediyor</option>
          <option value="Done">Bitti</option>
        </select>
        <button type="button" onClick={onEdit} className="text-xs text-slate-400 hover:text-slate-600">
          Düzenle
        </button>
        <button type="button" onClick={handleDelete} className="text-xs text-red-400 hover:text-red-600">
          Sil
        </button>
      </div>
    </div>
  );
}

export function ProjectDetailModal({ projectId, onClose }: ProjectDetailModalProps) {
  const { data: project, isLoading } = useProjectDetail(projectId);
  const tasksQuery = useProjectTasks(projectId);
  const [taskModal, setTaskModal] = useState<{ task?: ProjectTaskDto } | null>(null);

  const status = project ? (STATUS_LABEL[project.status] ?? { label: project.status, className: 'bg-slate-100 text-slate-600' }) : null;
  const health = project ? (HEALTH_LABEL[project.healthStatus] ?? { label: project.healthStatus, className: 'bg-slate-400 text-white' }) : null;

  const tasks = tasksQuery.data?.items ?? [];
  const evm = computeProjectEvmSummary(tasks);
  const spiTier = spiHealthTier(evm.spi);

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/40 p-4">
      <div className="max-h-[90vh] w-full max-w-2xl overflow-y-auto rounded-xl bg-white p-6 shadow-xl">
        <div className="mb-4 flex items-center justify-between">
          <h2 className="text-lg font-semibold text-slate-800">Proje Detayı</h2>
          <button type="button" onClick={onClose} className="text-slate-400 hover:text-slate-600">
            ✕
          </button>
        </div>

        {isLoading || !project ? (
          <div className="py-8 text-center text-sm text-slate-400">Yükleniyor…</div>
        ) : (
          <div className="space-y-4">
            <div>
              <div className="flex flex-wrap items-center gap-2">
                <h3 className="text-base font-semibold text-slate-800">{project.name}</h3>
                {status && (
                  <span className={`rounded-full px-2 py-0.5 text-xs font-medium ${status.className}`}>
                    {status.label}
                  </span>
                )}
                {health && (
                  <span className={`rounded px-2 py-0.5 text-[10px] font-bold tracking-wide ${health.className}`}>
                    {health.label}
                  </span>
                )}
              </div>
              {project.description && <p className="mt-1 text-sm text-slate-500">{project.description}</p>}
              <p className="mt-1 text-xs text-slate-400">
                {formatDateTr(project.startDate)} – {formatDateTr(project.endDate)}
              </p>
            </div>

            <div className="grid grid-cols-3 gap-3 rounded-lg bg-slate-50 p-3 text-center">
              <div>
                <div className={`text-lg font-bold ${SPI_TIER_CLASS[spiTier]}`}>{evm.spi ?? '—'}</div>
                <div className="text-[10px] font-medium uppercase tracking-wide text-slate-400">SPI</div>
              </div>
              <div>
                <div className="text-lg font-bold text-slate-700">%{evm.percentComplete}</div>
                <div className="text-[10px] font-medium uppercase tracking-wide text-slate-400">Tamamlanma</div>
              </div>
              <div>
                <div className="text-lg font-bold text-slate-700">
                  {evm.doneTaskCount}/{evm.totalTaskCount}
                </div>
                <div className="text-[10px] font-medium uppercase tracking-wide text-slate-400">Görev</div>
              </div>
            </div>

            <div>
              <div className="mb-1.5 text-xs font-medium text-slate-500">Müşteriler ({project.customers.length})</div>
              <div className="flex flex-wrap gap-1.5">
                {project.customers.length === 0 ? (
                  <span className="text-sm text-slate-400">Atanmış müşteri yok</span>
                ) : (
                  project.customers.map((c) => (
                    <span key={c.id} className="rounded-full bg-slate-100 px-2.5 py-1 text-xs text-slate-600">
                      {c.name}
                    </span>
                  ))
                )}
              </div>
            </div>

            <div>
              <div className="mb-1.5 text-xs font-medium text-slate-500">Çalışanlar ({project.employees.length})</div>
              <div className="flex flex-wrap gap-1.5">
                {project.employees.length === 0 ? (
                  <span className="text-sm text-slate-400">Atanmış çalışan yok</span>
                ) : (
                  project.employees.map((e) => (
                    <span key={e.id} className="rounded-full bg-slate-100 px-2.5 py-1 text-xs text-slate-600">
                      {e.name}
                    </span>
                  ))
                )}
              </div>
            </div>

            <div>
              <div className="mb-1.5 flex items-center justify-between">
                <span className="text-xs font-medium text-slate-500">Görevler ({tasks.length})</span>
                <button
                  type="button"
                  onClick={() => setTaskModal({})}
                  className="text-xs font-semibold text-indigo-600 hover:underline"
                >
                  + Görev Ekle
                </button>
              </div>
              <div className="space-y-1.5">
                {tasksQuery.isLoading ? (
                  <p className="text-sm text-slate-400">Yükleniyor…</p>
                ) : tasks.length === 0 ? (
                  <p className="text-sm text-slate-400">Henüz görev eklenmemiş.</p>
                ) : (
                  tasks.map((task) => (
                    <TaskRow key={task.id} task={task} onEdit={() => setTaskModal({ task })} />
                  ))
                )}
              </div>
            </div>
          </div>
        )}

        <div className="mt-6 flex justify-end">
          <button
            type="button"
            onClick={onClose}
            className="rounded-lg border border-slate-200 px-4 py-2 text-sm font-medium text-slate-600 hover:bg-slate-50"
          >
            Kapat
          </button>
        </div>
      </div>

      {taskModal && (
        <ProjectTaskFormModal projectId={projectId} task={taskModal.task} onClose={() => setTaskModal(null)} />
      )}
    </div>
  );
}
