import { useMemo, useState } from 'react';
import type { ProjectTaskDto } from '../../../api/types';

const STATUS_ROW_CLASS: Record<string, string> = {
  NotStarted: 'border-l-4 border-slate-300',
  InProgress: 'border-l-4 border-amber-400',
  Done: 'border-l-4 border-emerald-500',
};

function formatDateTr(date: string): string {
  return new Date(`${date}T00:00:00`).toLocaleDateString('tr-TR');
}

interface ScheduleTabProps {
  tasks: ProjectTaskDto[];
  resolveEmployee: (id: string | null) => string;
}

/** Görevlerin ParentTaskId'ye göre basit 2 seviyeli WBS ağacı (Schedule sekmesi) — gerçek bir
 * Gantt/CPM motoru değil, sadece hiyerarşi + bağımlılık + baseline-vs-güncel karşılaştırması
 * gösterir. Kilometre taşları (IsMilestone) da aynı listede ◆ ile işaretlenir. */
export function ScheduleTab({ tasks, resolveEmployee }: ScheduleTabProps) {
  const [collapsed, setCollapsed] = useState<Set<string>>(new Set());

  const byParent = useMemo(() => {
    const map = new Map<string, ProjectTaskDto[]>();
    for (const task of tasks) {
      const key = task.parentTaskId ?? '__root__';
      const list = map.get(key) ?? [];
      list.push(task);
      map.set(key, list);
    }
    return map;
  }, [tasks]);

  const byId = useMemo(() => new Map(tasks.map((t) => [t.id, t])), [tasks]);
  const roots = byParent.get('__root__') ?? [];

  const toggle = (id: string) =>
    setCollapsed((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });

  const renderRow = (task: ProjectTaskDto, depth: number): React.ReactNode => {
    const children = byParent.get(task.id) ?? [];
    const hasChildren = children.length > 0;
    const isCollapsed = collapsed.has(task.id);
    const dependsOn = task.dependsOnTaskId ? byId.get(task.dependsOnTaskId) : null;
    const baselineDiffers = task.estimatedEffortHours !== task.baselineEffortHours || task.endDate !== task.baselineEndDate;

    return (
      <div key={task.id}>
        <div
          className={`flex items-center justify-between gap-2 rounded-r-lg py-2 pl-2 pr-3 text-sm hover:bg-slate-50 ${STATUS_ROW_CLASS[task.status] ?? ''}`}
          style={{ marginLeft: `${depth * 1.5}rem` }}
        >
          <div className="min-w-0">
            <div className="flex items-center gap-1.5">
              {hasChildren && (
                <button type="button" onClick={() => toggle(task.id)} className="w-3 text-slate-400">
                  {isCollapsed ? '▶' : '▼'}
                </button>
              )}
              {task.isMilestone && <span className="text-indigo-500">◆</span>}
              <span className="truncate font-medium text-slate-700">{task.name}</span>
            </div>
            <div className="text-xs text-slate-400">
              {formatDateTr(task.startDate)} – {formatDateTr(task.endDate)} · {task.estimatedEffortHours}h ·{' '}
              {resolveEmployee(task.assignedEmployeeId)}
              {dependsOn && <span> · ← "{dependsOn.name}"e bağımlı</span>}
            </div>
            {baselineDiffers && (
              <div className="text-[11px] text-amber-600">
                Baseline: {task.baselineEffortHours}h, {formatDateTr(task.baselineEndDate)} (güncelden farklı)
              </div>
            )}
          </div>
        </div>
        {hasChildren && !isCollapsed && children.map((child) => renderRow(child, depth + 1))}
      </div>
    );
  };

  return (
    <div className="rounded-xl border border-slate-200 bg-white p-4">
      <div className="mb-3 text-sm font-semibold text-slate-700">Zaman Çizelgesi / WBS ({tasks.length} görev)</div>
      {roots.length === 0 ? (
        <p className="text-sm text-slate-400">Henüz görev eklenmemiş.</p>
      ) : (
        <div className="space-y-1">{roots.map((task) => renderRow(task, 0))}</div>
      )}
    </div>
  );
}
