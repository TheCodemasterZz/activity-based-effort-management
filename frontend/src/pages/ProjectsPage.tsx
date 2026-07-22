import { useMemo, useState } from 'react';
import { useProjects } from '../hooks/useProjects';
import { useAllProjectTasks } from '../hooks/useProjectTasks';
import { useAllProjectRisks } from '../hooks/useProjectRisks';
import { useAllProjectIssues } from '../hooks/useProjectIssues';
import { useEmployees } from '../hooks/useEmployees';
import { useWorkLogs } from '../hooks/useWorkLogs';
import { useDeleteProjectMutation } from '../hooks/useDeleteProjectMutation';
import { ProjectsTable } from '../components/projects/ProjectsTable';
import { ProjectFormModal } from '../components/projects/ProjectFormModal';
import { ProjectDetailPage } from './ProjectDetailPage';
import { StatCardShell } from '../components/dashboard/SummaryCards';
import { RatioGaugeWidget } from '../components/dashboard/charts/RatioGaugeWidget';
import { MqlFilterInput } from '../components/dashboard/MqlFilterInput';
import { ErrorState } from '../components/common/ErrorState';
import { computeProjectEvmSummary } from '../lib/projectSpi';
import { evaluateMql, type MqlFieldInfo, type MqlNode, type MqlRecord } from '../lib/mql';
import { ApiError } from '../api/client';
import { WORK_LOG_ENTRY_TYPE, type ProjectDto, type ProjectIssueDto, type ProjectRiskDto, type ProjectTaskDto } from '../api/types';

function dateKeyDaysAgo(days: number): string {
  const d = new Date();
  d.setDate(d.getDate() - days);
  return d.toISOString().slice(0, 10);
}

function todayKey(): string {
  return new Date().toISOString().slice(0, 10);
}

const PROJECT_STATUS_LABEL: Record<string, string> = {
  Active: 'Aktif',
  Completed: 'Tamamlandı',
  Cancelled: 'İptal Edildi',
};

const PROJECT_HEALTH_LABEL: Record<string, string> = {
  OnTrack: 'ON TRACK',
  AtRisk: 'AT RISK',
  NeedsHelp: 'NEEDS HELP',
};

// Proje listesine özgü MQL alan şeması — work log ekranlarının MQL_FIELD_INFO'suyla aynı
// parser/UI'ı (parseMqlQuery/evaluateMql/MqlFilterInput) kullanır, sadece alan kümesi farklıdır.
const PROJECT_MQL_FIELDS: MqlFieldInfo[] = [
  { field: 'name', label: 'Proje Adı', aliases: ['name', 'proje', 'ad'], kind: 'text' },
  { field: 'description', label: 'Açıklama', aliases: ['description', 'aciklama', 'açıklama'], kind: 'text' },
  { field: 'status', label: 'Durum', aliases: ['status', 'durum'], kind: 'text' },
  { field: 'health', label: 'Sağlık', aliases: ['health', 'saglik', 'sağlık'], kind: 'text' },
  { field: 'startDate', label: 'Başlangıç (yyyy-mm-dd)', aliases: ['startdate', 'baslangic', 'başlangıç'], kind: 'date' },
  { field: 'endDate', label: 'Bitiş (yyyy-mm-dd)', aliases: ['enddate', 'bitis', 'bitiş'], kind: 'date' },
  { field: 'spi', label: 'SPI', aliases: ['spi'], kind: 'number' },
  { field: 'completion', label: 'Tamamlanma (%)', aliases: ['completion', 'tamamlanma'], kind: 'number' },
  { field: 'actualHours', label: 'Gerçekleşen Saat', aliases: ['actualhours', 'saat', 'gerceklesen', 'gerçekleşen'], kind: 'number' },
  { field: 'employeeCount', label: 'Aktif Kişi', aliases: ['employeecount', 'kisi', 'kişi'], kind: 'number' },
  { field: 'taskCount', label: 'Görev Sayısı', aliases: ['taskcount', 'gorev', 'görev'], kind: 'number' },
  { field: 'milestoneCount', label: 'Kilometre Taşı', aliases: ['milestonecount', 'kilometretasi', 'kilometretaşı'], kind: 'number' },
];

const PROJECT_MQL_EXAMPLE = 'health = "AT RISK" AND spi < 1';

function buildProjectMqlRecord(
  project: ProjectDto,
  tasks: ProjectTaskDto[],
  stats: { actualHours: number; employeeIds: Set<string> } | undefined,
): MqlRecord {
  const evm = computeProjectEvmSummary(tasks);
  const milestoneCount = tasks.filter((t) => t.isMilestone).length;
  return {
    name: project.name,
    description: project.description ?? '',
    status: PROJECT_STATUS_LABEL[project.status] ?? project.status,
    health: PROJECT_HEALTH_LABEL[project.healthStatus] ?? project.healthStatus,
    startDate: project.startDate ?? '',
    endDate: project.endDate ?? '',
    spi: evm.spi ?? 0,
    completion: evm.percentComplete,
    actualHours: stats?.actualHours ?? 0,
    employeeCount: stats?.employeeIds.size ?? 0,
    taskCount: tasks.length,
    milestoneCount,
  };
}

export function ProjectsPage() {
  const projects = useProjects();
  const allTasks = useAllProjectTasks();
  const allRisks = useAllProjectRisks();
  const allIssues = useAllProjectIssues();
  const employees = useEmployees();
  // Kartlardaki "Gerçekleşen" ve "Aktif Kişi" göstergeleri için — son 90 gün, mock verinin
  // (içinde bulunulan ay) her koşulda kapsanmasını garanti eden güvenli bir pencere.
  const recentActualLogs = useWorkLogs(dateKeyDaysAgo(90), todayKey(), WORK_LOG_ENTRY_TYPE.Actual);
  const deleteMutation = useDeleteProjectMutation();

  const [mqlAst, setMqlAst] = useState<MqlNode | null>(null);
  const [createOpen, setCreateOpen] = useState(false);
  const [editingProject, setEditingProject] = useState<ProjectDto | null>(null);
  const [viewingProjectId, setViewingProjectId] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const tasksByProject = useMemo(() => {
    const map = new Map<string, ProjectTaskDto[]>();
    for (const task of allTasks.data?.items ?? []) {
      const list = map.get(task.projectId) ?? [];
      list.push(task);
      map.set(task.projectId, list);
    }
    return map;
  }, [allTasks.data]);

  const risksByProject = useMemo(() => {
    const map = new Map<string, ProjectRiskDto[]>();
    for (const risk of allRisks.data?.items ?? []) {
      const list = map.get(risk.projectId) ?? [];
      list.push(risk);
      map.set(risk.projectId, list);
    }
    return map;
  }, [allRisks.data]);

  const issuesByProject = useMemo(() => {
    const map = new Map<string, ProjectIssueDto[]>();
    for (const issue of allIssues.data?.items ?? []) {
      const list = map.get(issue.projectId) ?? [];
      list.push(issue);
      map.set(issue.projectId, list);
    }
    return map;
  }, [allIssues.data]);

  const employeesById = useMemo(
    () => new Map(employees.data?.items.map((e) => [e.id, e.name])),
    [employees.data],
  );
  const resolveEmployee = (id: string | null) => (id ? employeesById.get(id) ?? 'Bilinmeyen kişi' : '—');

  const projectStatsById = useMemo(() => {
    const map = new Map<string, { actualHours: number; employeeIds: Set<string> }>();
    for (const log of recentActualLogs.data?.items ?? []) {
      const entry = map.get(log.projectId) ?? { actualHours: 0, employeeIds: new Set<string>() };
      entry.actualHours += log.hours;
      entry.employeeIds.add(log.employeeId);
      map.set(log.projectId, entry);
    }
    return map;
  }, [recentActualLogs.data]);

  const filteredItems = useMemo(() => {
    const items = projects.data?.items ?? [];
    if (!mqlAst) return items;
    return items.filter((p) =>
      evaluateMql(
        mqlAst,
        buildProjectMqlRecord(p, tasksByProject.get(p.id) ?? [], projectStatsById.get(p.id)),
        PROJECT_MQL_FIELDS,
      ),
    );
  }, [projects.data, mqlAst, tasksByProject, projectStatsById]);

  const projectMqlFieldValues = useMemo(
    () => ({
      name: (projects.data?.items ?? []).map((p) => p.name),
      status: Object.values(PROJECT_STATUS_LABEL),
      health: Object.values(PROJECT_HEALTH_LABEL),
    }),
    [projects.data],
  );

  const handleDeactivate = async (project: ProjectDto) => {
    if (!window.confirm(`"${project.name}" projesini pasife almak istediğinize emin misiniz?`)) return;
    setErrorMessage(null);
    try {
      await deleteMutation.mutateAsync(project.id);
    } catch (err) {
      setErrorMessage(err instanceof ApiError ? err.message : 'Beklenmeyen bir hata oluştu.');
    }
  };

  const isLoading =
    projects.isLoading ||
    allTasks.isLoading ||
    recentActualLogs.isLoading ||
    allRisks.isLoading ||
    allIssues.isLoading ||
    employees.isLoading;

  // Portföyün tamamına bakan 5 özet widget — arama filtresinden BAĞIMSIZ, her zaman tüm
  // projeleri kapsar (SummaryCards ile aynı StatCardShell kabuğu kullanılıyor).
  const allProjects = projects.data?.items ?? [];
  const totalProjectCount = allProjects.length;
  const activeProjectCount = allProjects.filter((p) => p.status === 'Active').length;
  const atRiskProjectCount = allProjects.filter(
    (p) => p.healthStatus === 'AtRisk' || p.healthStatus === 'NeedsHelp',
  ).length;
  const spiValues = allProjects
    .map((p) => computeProjectEvmSummary(tasksByProject.get(p.id) ?? []).spi)
    .filter((spi): spi is number => spi !== null);
  const avgSpi = spiValues.length > 0 ? spiValues.reduce((sum, v) => sum + v, 0) / spiValues.length : null;
  const totalActualHours90d = Array.from(projectStatsById.values()).reduce((sum, s) => sum + s.actualHours, 0);

  if (viewingProjectId) {
    return <ProjectDetailPage projectId={viewingProjectId} onBack={() => setViewingProjectId(null)} />;
  }

  return (
    <div className="flex flex-1 flex-col overflow-hidden bg-slate-50 p-6">
      <div className="mb-4 flex shrink-0 items-center gap-3">
        <div className="min-w-0 flex-1">
          <MqlFilterInput
            onApply={setMqlAst}
            fieldValues={projectMqlFieldValues}
            fieldInfo={PROJECT_MQL_FIELDS}
            example={PROJECT_MQL_EXAMPLE}
          />
        </div>
        <button
          type="button"
          onClick={() => setCreateOpen(true)}
          className="shrink-0 rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-700"
        >
          + Proje Ekle
        </button>
      </div>

      <div className="mb-4 flex shrink-0 flex-col gap-3 sm:flex-row">
        <StatCardShell
          icon="📁"
          iconBg="bg-indigo-50 text-indigo-600"
          label="Toplam Proje"
          value={String(totalProjectCount)}
          caption="Portföydeki tüm projeler"
        />
        <StatCardShell
          icon="✅"
          iconBg="bg-emerald-50 text-emerald-600"
          label="Aktif Proje"
          value={String(activeProjectCount)}
          caption={totalProjectCount > 0 ? `${totalProjectCount} projeden` : 'Aktif durumdaki projeler'}
          chart={
            totalProjectCount > 0 ? (
              <RatioGaugeWidget
                percent={(activeProjectCount / totalProjectCount) * 100}
                color="#059669"
                label="Aktif proje oranı"
                size={44}
              />
            ) : undefined
          }
        />
        <StatCardShell
          icon="⚠️"
          iconBg="bg-red-50 text-red-600"
          label="Risk Altında"
          value={String(atRiskProjectCount)}
          caption="At Risk + Needs Help"
          chart={
            totalProjectCount > 0 ? (
              <RatioGaugeWidget
                percent={(atRiskProjectCount / totalProjectCount) * 100}
                color="#dc2626"
                label="Risk oranı"
                size={44}
              />
            ) : undefined
          }
        />
        <StatCardShell
          icon="📈"
          iconBg="bg-sky-50 text-sky-600"
          label="Ortalama SPI"
          value={avgSpi !== null ? avgSpi.toFixed(2) : '—'}
          caption={spiValues.length > 0 ? `${spiValues.length} projeden` : 'Henüz hesaplanabilir görev yok'}
        />
        <StatCardShell
          icon="⏱"
          iconBg="bg-amber-50 text-amber-600"
          label="Gerçekleşen Saat"
          value={`${totalActualHours90d.toFixed(0)}h`}
          caption="Son 90 gün, tüm projeler"
        />
      </div>

      {errorMessage && (
        <div className="mb-4 flex shrink-0 items-center gap-3 rounded-xl border border-red-200 bg-red-50 p-4 text-red-700">
          <span className="text-xl">⚠</span>
          <span className="text-sm">{errorMessage}</span>
        </div>
      )}

      {isLoading ? (
        <div className="rounded-xl border border-slate-200 bg-white p-8 text-center text-slate-400">Yükleniyor…</div>
      ) : projects.isError ? (
        <ErrorState />
      ) : (
        <div className="min-h-0 flex-1">
          <ProjectsTable
            projects={filteredItems}
            tasksByProject={tasksByProject}
            risksByProject={risksByProject}
            issuesByProject={issuesByProject}
            resolveEmployee={resolveEmployee}
            onView={(project) => setViewingProjectId(project.id)}
            onEdit={(project) => setEditingProject(project)}
            onDeactivate={handleDeactivate}
          />
        </div>
      )}

      {createOpen && <ProjectFormModal mode="create" onClose={() => setCreateOpen(false)} />}

      {editingProject && (
        <ProjectFormModal mode="edit" project={editingProject} onClose={() => setEditingProject(null)} />
      )}
    </div>
  );
}
