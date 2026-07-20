import { useMemo, useState } from 'react';
import { useProjects } from '../hooks/useProjects';
import { useDeleteProjectMutation } from '../hooks/useDeleteProjectMutation';
import { ProjectFormModal } from '../components/projects/ProjectFormModal';
import { ProjectDetailModal } from '../components/projects/ProjectDetailModal';
import { ApiError } from '../api/client';
import type { ProjectDto } from '../api/types';

const STATUS_LABEL: Record<string, { label: string; className: string }> = {
  Active: { label: 'Aktif', className: 'bg-emerald-50 text-emerald-700' },
  Completed: { label: 'Tamamlandı', className: 'bg-slate-100 text-slate-600' },
  Cancelled: { label: 'İptal Edildi', className: 'bg-red-50 text-red-600' },
};

export function ProjectsPage() {
  const projects = useProjects();
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

  const handleDelete = async (project: ProjectDto) => {
    if (!window.confirm(`"${project.name}" projesini pasife almak istediğinize emin misiniz?`)) return;
    setErrorMessage(null);
    try {
      await deleteMutation.mutateAsync(project.id);
    } catch (err) {
      setErrorMessage(err instanceof ApiError ? err.message : 'Beklenmeyen bir hata oluştu.');
    }
  };

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

      <div className="overflow-hidden rounded-xl border border-slate-200 bg-white">
        {projects.isLoading ? (
          <div className="p-8 text-center text-slate-400">Yükleniyor…</div>
        ) : projects.isError ? (
          <div className="p-8 text-center text-red-600">Projeler yüklenemedi.</div>
        ) : filteredItems.length === 0 ? (
          <div className="p-8 text-center text-slate-400">Proje bulunamadı.</div>
        ) : (
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-slate-200 bg-slate-50 text-left text-xs font-medium uppercase tracking-wide text-slate-500">
                <th className="px-4 py-3">Proje Adı</th>
                <th className="px-4 py-3">Açıklama</th>
                <th className="px-4 py-3">Durum</th>
                <th className="px-4 py-3 text-right">İşlemler</th>
              </tr>
            </thead>
            <tbody>
              {filteredItems.map((project) => {
                const status = STATUS_LABEL[project.status] ?? {
                  label: project.status,
                  className: 'bg-slate-100 text-slate-600',
                };
                return (
                  <tr key={project.id} className="border-b border-slate-100 last:border-0 hover:bg-slate-50">
                    <td className="px-4 py-3 font-medium text-slate-800">{project.name}</td>
                    <td className="max-w-xs truncate px-4 py-3 text-slate-500">{project.description || '—'}</td>
                    <td className="px-4 py-3">
                      <span className={`rounded-full px-2 py-0.5 text-xs font-medium ${status.className}`}>
                        {status.label}
                      </span>
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex justify-end gap-2 text-xs font-medium">
                        <button
                          type="button"
                          onClick={() => setViewingProjectId(project.id)}
                          className="rounded-lg border border-slate-200 px-2.5 py-1.5 text-slate-600 hover:bg-slate-100"
                        >
                          Görüntüle
                        </button>
                        <button
                          type="button"
                          onClick={() => setEditingProject(project)}
                          className="rounded-lg border border-slate-200 px-2.5 py-1.5 text-slate-600 hover:bg-slate-100"
                        >
                          Düzenle
                        </button>
                        <button
                          type="button"
                          onClick={() => handleDelete(project)}
                          className="rounded-lg border border-red-200 px-2.5 py-1.5 text-red-600 hover:bg-red-50"
                        >
                          Pasife Al
                        </button>
                      </div>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        )}
      </div>

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
