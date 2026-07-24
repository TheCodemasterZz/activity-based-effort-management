import { useMemo, useState } from 'react';
import { useDeleteProjectTaskMutation, useUpdateProjectTaskStatusMutation } from '../../../hooks/useProjectTaskMutations';
import { PROJECT_TASK_STATUS, type ProjectTaskDto } from '../../../api/types';

const TASK_STATUS_LABEL: Record<string, { label: string; className: string }> = {
  NotStarted: { label: 'Başlamadı', className: 'bg-slate-100 text-slate-500' },
  InProgress: { label: 'Devam Ediyor', className: 'bg-amber-100 text-amber-700' },
  Done: { label: 'Bitti', className: 'bg-emerald-100 text-emerald-700' },
};

function formatDateTr(date: string): string {
  return new Date(`${date}T00:00:00`).toLocaleDateString('tr-TR');
}

function TaskRow({ task, resolveUser, onEdit }: { task: ProjectTaskDto; resolveUser: (id: string | null) => string; onEdit: () => void }) {
  const statusMutation = useUpdateProjectTaskStatusMutation();
  const deleteMutation = useDeleteProjectTaskMutation();
  const statusInfo = TASK_STATUS_LABEL[task.status] ?? { label: task.status, className: 'bg-slate-100 text-slate-500' };

  const handleDelete = async () => {
    if (!window.confirm(`"${task.name}" görevini silmek istediğinize emin misiniz?`)) return;
    await deleteMutation.mutateAsync(task.id);
  };

  return (
    <div className="flex items-center justify-between gap-2 rounded-lg border border-slate-100 px-3 py-2.5 text-sm hover:bg-slate-50">
      <div className="min-w-0">
        <div className="flex items-center gap-1.5">
          {task.isMilestone && <span title="Kilometre taşı" className="text-indigo-500">◆</span>}
          <span className="truncate font-medium text-slate-700">{task.name}</span>
        </div>
        <div className="text-xs text-slate-400">
          {formatDateTr(task.startDate)} – {formatDateTr(task.endDate)} · {task.estimatedEffortHours}h ·{' '}
          <span className="text-slate-500">{resolveUser(task.assignedUserId)}</span>
        </div>
      </div>
      <div className="flex shrink-0 items-center gap-1.5">
        <select
          value={task.status}
          onChange={(e) =>
            statusMutation.mutate({ id: task.id, status: PROJECT_TASK_STATUS[e.target.value as keyof typeof PROJECT_TASK_STATUS] })
          }
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

interface TasksTabProps {
  tasks: ProjectTaskDto[];
  resolveUser: (id: string | null) => string;
  onAddTask: () => void;
  onEditTask: (task: ProjectTaskDto) => void;
}

/** Görevlerin düz (WBS hiyerarşisi olmadan) listesi — Schedule sekmesinin ağaç görünümünün
 * aksine, burada odak kişi ataması ve iş yükü dağılımı üzerinde. */
export function TasksTab({ tasks, resolveUser, onAddTask, onEditTask }: TasksTabProps) {
  const [filterEmployeeId, setFilterEmployeeId] = useState<string | 'all'>('all');

  const workload = useMemo(() => {
    const map = new Map<string, number>();
    for (const task of tasks) {
      if (!task.assignedUserId) continue;
      map.set(task.assignedUserId, (map.get(task.assignedUserId) ?? 0) + task.estimatedEffortHours);
    }
    return Array.from(map.entries())
      .map(([userId, hours]) => ({ userId, hours }))
      .sort((a, b) => b.hours - a.hours);
  }, [tasks]);

  const filteredTasks = filterEmployeeId === 'all' ? tasks : tasks.filter((t) => t.assignedUserId === filterEmployeeId);

  return (
    <div className="grid grid-cols-1 gap-4 lg:grid-cols-3">
      <div className="rounded-xl border border-slate-200 bg-white p-4 lg:col-span-2">
        <div className="mb-3 flex items-center justify-between">
          <span className="text-sm font-semibold text-slate-700">Görevler ({filteredTasks.length})</span>
          <button type="button" onClick={onAddTask} className="text-xs font-semibold text-indigo-600 hover:underline">
            + Görev Ekle
          </button>
        </div>
        <div className="space-y-1.5">
          {filteredTasks.length === 0 ? (
            <p className="text-sm text-slate-400">Görev bulunamadı.</p>
          ) : (
            filteredTasks.map((task) => (
              <TaskRow key={task.id} task={task} resolveUser={resolveUser} onEdit={() => onEditTask(task)} />
            ))
          )}
        </div>
      </div>

      <div className="rounded-xl border border-slate-200 bg-white p-4">
        <div className="mb-2 text-sm font-semibold text-slate-700">İş Yükü (kişi başına saat)</div>
        {workload.length === 0 ? (
          <p className="text-sm text-slate-400">Henüz atanmış görev yok.</p>
        ) : (
          <div className="space-y-2">
            <button
              type="button"
              onClick={() => setFilterEmployeeId('all')}
              className={
                'block w-full rounded-md px-2 py-1 text-left text-xs font-medium ' +
                (filterEmployeeId === 'all' ? 'bg-indigo-50 text-indigo-700' : 'text-slate-500 hover:bg-slate-50')
              }
            >
              Tümü
            </button>
            {workload.map(({ userId, hours }) => {
              const maxHours = workload[0]?.hours || 1;
              return (
                <button
                  type="button"
                  key={userId}
                  onClick={() => setFilterEmployeeId(userId)}
                  className={
                    'block w-full rounded-md px-2 py-1.5 text-left ' +
                    (filterEmployeeId === userId ? 'bg-indigo-50' : 'hover:bg-slate-50')
                  }
                >
                  <div className="flex items-center justify-between text-xs">
                    <span className="truncate font-medium text-slate-700">{resolveUser(userId)}</span>
                    <span className="shrink-0 text-slate-400">{hours.toFixed(0)}h</span>
                  </div>
                  <div className="mt-1 h-1.5 w-full overflow-hidden rounded-full bg-slate-100">
                    <div className="h-full bg-indigo-400" style={{ width: `${(hours / maxHours) * 100}%` }} />
                  </div>
                </button>
              );
            })}
          </div>
        )}
      </div>
    </div>
  );
}
