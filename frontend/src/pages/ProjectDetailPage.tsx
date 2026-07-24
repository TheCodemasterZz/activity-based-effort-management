import { useMemo, useState } from 'react';
import { useProjectDetail } from '../hooks/useProjects';
import { useProjectTasks } from '../hooks/useProjectTasks';
import { useProjectRisks } from '../hooks/useProjectRisks';
import { useProjectIssues } from '../hooks/useProjectIssues';
import { useWorkLogs } from '../hooks/useWorkLogs';
import { useEmployees } from '../hooks/useEmployees';
import { computeProjectEvmSummary } from '../lib/projectSpi';
import { ProjectDetailTabs, type ProjectDetailTabKey } from '../components/projects/ProjectDetailTabs';
import { OverviewTab } from '../components/projects/tabs/OverviewTab';
import { ScheduleTab } from '../components/projects/tabs/ScheduleTab';
import { TasksTab } from '../components/projects/tabs/TasksTab';
import { TimesheetTab } from '../components/projects/tabs/TimesheetTab';
import { RisksTab } from '../components/projects/tabs/RisksTab';
import { IssuesTab } from '../components/projects/tabs/IssuesTab';
import { PlaceholderTab } from '../components/projects/tabs/PlaceholderTab';
import { ProjectTaskFormModal } from '../components/projects/ProjectTaskFormModal';
import { RiskFormModal } from '../components/projects/RiskFormModal';
import { IssueFormModal } from '../components/projects/IssueFormModal';
import { WORK_LOG_ENTRY_TYPE, type ProjectIssueDto, type ProjectRiskDto, type ProjectTaskDto } from '../api/types';
import type { ChartPoint } from '../lib/trend';

interface ProjectDetailPageProps {
  projectId: string;
  onBack: () => void;
}

const STATUS_LABEL: Record<string, { label: string; className: string }> = {
  Active: { label: 'Aktif', className: 'bg-emerald-50 text-emerald-700' },
  Completed: { label: 'Tamamlandı', className: 'bg-slate-100 text-slate-600' },
  Cancelled: { label: 'İptal Edildi', className: 'bg-red-50 text-red-600' },
};

function dateKeyDaysAgo(days: number): string {
  const d = new Date();
  d.setDate(d.getDate() - days);
  return d.toISOString().slice(0, 10);
}

function todayKey(): string {
  return new Date().toISOString().slice(0, 10);
}

const PLACEHOLDER_LABEL: Partial<Record<ProjectDetailTabKey, string>> = {
  resources: 'Resources',
  budget: 'Budget/Financials',
  changes: 'Changes',
  approvals: 'Approvals',
  documents: 'Documents',
  stakeholders: 'Stakeholders',
  status: 'Status/Reports',
};

/** Clarity PPM benzeri kurumsal ölçekte proje detay sayfası — kart listesinden ayrı, tam sayfa
 * bir görünüm (modal değil). Üstte sabit kimlik/sağlık şeridi, altında yatay bir sekme şeridi
 * (Faz 1: Overview/Schedule/Tasks/Timesheet/Risks/Issues gerçek içerikli, kalan 7 sekme
 * "yakında" placeholder'ı — bkz. plan dosyasındaki Faz 2/3 yol haritası). */
export function ProjectDetailPage({ projectId, onBack }: ProjectDetailPageProps) {
  const { data: project, isLoading } = useProjectDetail(projectId);
  const tasksQuery = useProjectTasks(projectId);
  const risksQuery = useProjectRisks(projectId);
  const issuesQuery = useProjectIssues(projectId);
  const actualLogsQuery = useWorkLogs(dateKeyDaysAgo(90), todayKey(), WORK_LOG_ENTRY_TYPE.Actual);
  const employees = useEmployees();

  const [activeTab, setActiveTab] = useState<ProjectDetailTabKey>('overview');
  const [taskModal, setTaskModal] = useState<{ task?: ProjectTaskDto } | null>(null);
  const [riskModal, setRiskModal] = useState<{ risk?: ProjectRiskDto } | null>(null);
  const [issueModal, setIssueModal] = useState<{ issue?: ProjectIssueDto } | null>(null);

  const status = project ? (STATUS_LABEL[project.status] ?? { label: project.status, className: 'bg-slate-100 text-slate-600' }) : null;

  const tasks = tasksQuery.data?.items ?? [];
  const risks = risksQuery.data?.items ?? [];
  const issues = issuesQuery.data?.items ?? [];
  const evm = computeProjectEvmSummary(tasks);
  const milestones = tasks.filter((t) => t.isMilestone);

  const employeesById = useMemo(() => new Map(employees.data?.items.map((e) => [e.id, e.name])), [employees.data]);
  const resolveUser = (id: string | null) => (id ? employeesById.get(id) ?? 'Bilinmeyen kişi' : '—');

  const projectActualLogs = useMemo(
    () => (actualLogsQuery.data?.items ?? []).filter((l) => l.projectId === projectId),
    [actualLogsQuery.data, projectId],
  );
  const actualHours90d = projectActualLogs.reduce((sum, l) => sum + l.hours, 0);
  const activeEmployeeCount = new Set(projectActualLogs.map((l) => l.userId)).size;

  // Son 30 günün günlük gerçekleşen efor trendi — Overview'daki StatCardShell'in "chart" sahasında.
  const hoursTrend = useMemo<ChartPoint[]>(() => {
    const byDay = new Map<string, number>();
    for (const log of projectActualLogs) {
      if (log.workDate < dateKeyDaysAgo(30)) continue;
      byDay.set(log.workDate, (byDay.get(log.workDate) ?? 0) + log.hours);
    }
    const points: ChartPoint[] = [];
    for (let i = 29; i >= 0; i--) {
      const key = dateKeyDaysAgo(i);
      points.push({ label: key.slice(5), value: byDay.get(key) ?? 0 });
    }
    return points;
  }, [projectActualLogs]);

  return (
    <div className="flex flex-1 flex-col overflow-hidden bg-slate-50">
      <div className="shrink-0 bg-white px-6 pt-6">
        <button
          type="button"
          onClick={onBack}
          className="mb-4 flex w-fit items-center gap-1.5 text-sm font-medium text-slate-500 hover:text-indigo-600"
        >
          ← Projeler
        </button>

        {project && (
          <div className="mb-3 flex flex-wrap items-center gap-2">
            <h1 className="text-xl font-bold text-slate-800">{project.name}</h1>
            {status && <span className={`rounded-full px-2 py-0.5 text-xs font-medium ${status.className}`}>{status.label}</span>}
          </div>
        )}

        <ProjectDetailTabs activeKey={activeTab} onChange={setActiveTab} />
      </div>

      <div className="min-h-0 flex-1 overflow-y-auto p-6">
        {isLoading || !project ? (
          <div className="rounded-xl border border-slate-200 bg-white p-8 text-center text-slate-400">Yükleniyor…</div>
        ) : (
          <>
            {activeTab === 'overview' && (
              <OverviewTab
                project={project}
                tasks={tasks}
                evm={evm}
                milestones={milestones}
                actualHours90d={actualHours90d}
                activeEmployeeCount={activeEmployeeCount}
                hoursTrend={hoursTrend}
                resolveUser={resolveUser}
                risks={risks}
                issues={issues}
              />
            )}
            {activeTab === 'schedule' && <ScheduleTab tasks={tasks} resolveUser={resolveUser} />}
            {activeTab === 'tasks' && (
              <TasksTab
                tasks={tasks}
                resolveUser={resolveUser}
                onAddTask={() => setTaskModal({})}
                onEditTask={(task) => setTaskModal({ task })}
              />
            )}
            {activeTab === 'timesheet' && <TimesheetTab projectId={projectId} />}
            {activeTab === 'risks' && (
              <RisksTab
                risks={risks}
                resolveUser={resolveUser}
                onAdd={() => setRiskModal({})}
                onEdit={(risk) => setRiskModal({ risk })}
              />
            )}
            {activeTab === 'issues' && (
              <IssuesTab
                issues={issues}
                resolveUser={resolveUser}
                onAdd={() => setIssueModal({})}
                onEdit={(issue) => setIssueModal({ issue })}
              />
            )}
            {PLACEHOLDER_LABEL[activeTab] && <PlaceholderTab label={PLACEHOLDER_LABEL[activeTab]!} />}
          </>
        )}
      </div>

      {taskModal && <ProjectTaskFormModal projectId={projectId} task={taskModal.task} onClose={() => setTaskModal(null)} />}
      {riskModal && <RiskFormModal projectId={projectId} risk={riskModal.risk} onClose={() => setRiskModal(null)} />}
      {issueModal && <IssueFormModal projectId={projectId} issue={issueModal.issue} onClose={() => setIssueModal(null)} />}
    </div>
  );
}
