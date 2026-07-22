import { useMemo, useState } from 'react';
import { useProjects } from '../hooks/useProjects';
import { useAllProjectTasks } from '../hooks/useProjectTasks';
import { useWorkLogs } from '../hooks/useWorkLogs';
import { useDeleteProjectMutation } from '../hooks/useDeleteProjectMutation';
import { ProjectCard } from '../components/projects/ProjectCard';
import { ProjectFormModal } from '../components/projects/ProjectFormModal';
import { ProjectDetailModal } from '../components/projects/ProjectDetailModal';
import { ApiError } from '../api/client';
import { WORK_LOG_ENTRY_TYPE, type ProjectDto, type ProjectTaskDto } from '../api/types';

function dateKeyDaysAgo(days: number): string {
  const d = new Date();
  d.setDate(d.getDate() - days);
  return d.toISOString().slice(0, 10);
}

function todayKey(): string {
  return new Date().toISOString().slice(0, 10);
}

export function ProjectsPage() {
  const projects = useProjects();
  const allTasks = useAllProjectTasks();
  // Kartlardaki "Gerçekleşen" ve "Aktif Kişi" göstergeleri için — son 90 gün, mock verinin
  // (içinde bulunulan ay) her koşulda kapsanmasını garanti eden güvenli bir pencere.
  const recentActualLogs = useWorkLogs(dateKeyDaysAgo(90), todayKey(), WORK_LOG_ENTRY_TYPE.Actual);
  const deleteMutation = useDeleteProjectMutation();

  const [search, setSearch] = useState('');
  const [createOpen, setCreateOpen] = useState(false);
  const [editingProject, setEditingProject] = useState<ProjectDto | null>(null);
  const [viewingProjectId, setViewingProjectId] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const filteredItems = useMemo(() => {
    const items = projects.data?.items ?? [];
    const query = search.trim().toLocaleLowerCase('tr');
    if (!query) return items;
    return items.filter((p) => p.name.toLocaleLowerCase('tr').includes(query));
  }, [projects.data, search]);

  const tasksByProject = useMemo(() => {
    const map = new Map<string, ProjectTaskDto[]>();
    for (const task of allTasks.data?.items ?? []) {
      const list = map.get(task.projectId) ?? [];
      list.push(task);
      map.set(task.projectId, list);
    }
    return map;
  }, [allTasks.data]);

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

  const handleDeactivate = async (project: ProjectDto) => {
    if (!window.confirm(`"${project.name}" projesini pasife almak istediğinize emin misiniz?`)) return;
    setErrorMessage(null);
    try {
      await deleteMutation.mutateAsync(project.id);
    } catch (err) {
      setErrorMessage(err instanceof ApiError ? err.message : 'Beklenmeyen bir hata oluştu.');
    }
  };

  const isLoading = projects.isLoading || allTasks.isLoading || recentActualLogs.isLoading;

  return (
    <div className="flex flex-1 flex-col overflow-y-auto bg-slate-50 p-6">
      <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-lg font-semibold text-slate-800">Projeler</h1>
        <div className="flex items-center gap-3">
          <input
            type="text"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Proje ara…"
            className="w-56 rounded-lg border border-slate-200 px-3 py-2 text-sm"
          />
          <button
            type="button"
            onClick={() => setCreateOpen(true)}
            className="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-700"
          >
            + Proje Ekle
          </button>
        </div>
      </div>

      {errorMessage && (
        <div className="mb-4 flex items-center gap-3 rounded-xl border border-red-200 bg-red-50 p-4 text-red-700">
          <span className="text-xl">⚠</span>
          <span className="text-sm">{errorMessage}</span>
        </div>
      )}

      {isLoading ? (
        <div className="rounded-xl border border-slate-200 bg-white p-8 text-center text-slate-400">Yükleniyor…</div>
      ) : projects.isError ? (
        <div className="rounded-xl border border-slate-200 bg-white p-8 text-center text-red-600">
          Projeler yüklenemedi.
        </div>
      ) : filteredItems.length === 0 ? (
        <div className="rounded-xl border border-slate-200 bg-white p-8 text-center text-slate-400">
          Proje bulunamadı.
        </div>
      ) : (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
          {filteredItems.map((project, index) => {
            const stats = projectStatsById.get(project.id);
            return (
              <ProjectCard
                key={project.id}
                project={project}
                tasks={tasksByProject.get(project.id) ?? []}
                actualHours={stats?.actualHours ?? 0}
                employeeCount={stats?.employeeIds.size ?? 0}
                colorIndex={index}
                onView={() => setViewingProjectId(project.id)}
                onEdit={() => setEditingProject(project)}
                onDeactivate={() => handleDeactivate(project)}
              />
            );
          })}
        </div>
      )}

      {createOpen && <ProjectFormModal mode="create" onClose={() => setCreateOpen(false)} />}

      {editingProject && (
        <ProjectFormModal mode="edit" project={editingProject} onClose={() => setEditingProject(null)} />
      )}

      {viewingProjectId && (
        <ProjectDetailModal projectId={viewingProjectId} onClose={() => setViewingProjectId(null)} />
      )}
    </div>
  );
}
