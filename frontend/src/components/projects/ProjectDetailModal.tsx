import { useProjectDetail } from '../../hooks/useProjects';

interface ProjectDetailModalProps {
  projectId: string;
  onClose: () => void;
}

const STATUS_LABEL: Record<string, { label: string; className: string }> = {
  Active: { label: 'Aktif', className: 'bg-emerald-50 text-emerald-700' },
  Completed: { label: 'Tamamlandı', className: 'bg-slate-100 text-slate-600' },
  Cancelled: { label: 'İptal Edildi', className: 'bg-red-50 text-red-600' },
};

export function ProjectDetailModal({ projectId, onClose }: ProjectDetailModalProps) {
  const { data: project, isLoading } = useProjectDetail(projectId);
  const status = project ? (STATUS_LABEL[project.status] ?? { label: project.status, className: 'bg-slate-100 text-slate-600' }) : null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/40 p-4">
      <div className="w-full max-w-lg rounded-xl bg-white p-6 shadow-xl">
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
              <div className="flex items-center gap-2">
                <h3 className="text-base font-semibold text-slate-800">{project.name}</h3>
                {status && (
                  <span className={`rounded-full px-2 py-0.5 text-xs font-medium ${status.className}`}>
                    {status.label}
                  </span>
                )}
              </div>
              {project.description && <p className="mt-1 text-sm text-slate-500">{project.description}</p>}
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
    </div>
  );
}
