import { useState } from 'react';
import { useCreateProjectMutation } from '../../hooks/useCreateProjectMutation';
import { useUpdateProjectMutation } from '../../hooks/useUpdateProjectMutation';
import { useDeleteProjectMutation } from '../../hooks/useDeleteProjectMutation';
import { useEmployeeSearch } from '../../hooks/useEmployees';
import { useEmployeeById } from '../../hooks/useEmployeeById';
import { AsyncSearchSelect } from '../common/AsyncSearchSelect';
import { ApiError } from '../../api/client';
import { PROJECT_PRIORITY, type ProjectDto, type ProjectPriorityValue } from '../../api/types';

interface ProjectFormModalProps {
  mode: 'create' | 'edit';
  project?: ProjectDto;
  onClose: () => void;
  onDeleted?: () => void;
}

const PRIORITY_OPTIONS: { value: ProjectPriorityValue; label: string }[] = [
  { value: PROJECT_PRIORITY.Low, label: 'Düşük' },
  { value: PROJECT_PRIORITY.Medium, label: 'Orta' },
  { value: PROJECT_PRIORITY.High, label: 'Yüksek' },
  { value: PROJECT_PRIORITY.Critical, label: 'Kritik' },
];

export function ProjectFormModal({ mode, project, onClose, onDeleted }: ProjectFormModalProps) {
  const [name, setName] = useState(project?.name ?? '');
  const [description, setDescription] = useState(project?.description ?? '');
  const [startDate, setStartDate] = useState(project?.startDate ?? '');
  const [endDate, setEndDate] = useState(project?.endDate ?? '');
  const [sponsor, setSponsor] = useState(project?.sponsor ?? '');
  const [priority, setPriority] = useState<ProjectPriorityValue>(
    PROJECT_PRIORITY[(project?.priority as keyof typeof PROJECT_PRIORITY) ?? 'Medium'] ?? PROJECT_PRIORITY.Medium,
  );
  const [strategicGoal, setStrategicGoal] = useState(project?.strategicGoal ?? '');
  const [projectManagerId, setProjectManagerId] = useState(project?.projectManagerEmployeeId ?? '');
  const [projectManagerLabel, setProjectManagerLabel] = useState('');
  const [pmQuery, setPmQuery] = useState('');
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const existingPm = useEmployeeById(project?.projectManagerEmployeeId ?? null);
  const pmSearch = useEmployeeSearch(pmQuery);
  const pmLabel = projectManagerLabel || existingPm.data?.name || '';

  const createMutation = useCreateProjectMutation();
  const updateMutation = useUpdateProjectMutation();
  const deleteMutation = useDeleteProjectMutation();

  const isPending = createMutation.isPending || updateMutation.isPending || deleteMutation.isPending;
  const canSubmit = name.trim().length > 0;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMessage(null);

    try {
      const payload = {
        name: name.trim(),
        description: description.trim() || null,
        startDate: startDate || null,
        endDate: endDate || null,
        sponsor: sponsor.trim() || null,
        projectManagerEmployeeId: projectManagerId || null,
        priority,
        strategicGoal: strategicGoal.trim() || null,
      };
      if (mode === 'edit' && project) {
        await updateMutation.mutateAsync({ id: project.id, payload });
      } else {
        await createMutation.mutateAsync(payload);
      }
      onClose();
    } catch (err) {
      setErrorMessage(err instanceof ApiError ? err.message : 'Beklenmeyen bir hata oluştu.');
    }
  };

  const handleDelete = async () => {
    if (!project) return;
    if (!window.confirm(`"${project.name}" projesini pasife almak istediğinize emin misiniz?`)) return;

    setErrorMessage(null);
    try {
      await deleteMutation.mutateAsync(project.id);
      onDeleted?.();
      onClose();
    } catch (err) {
      setErrorMessage(err instanceof ApiError ? err.message : 'Beklenmeyen bir hata oluştu.');
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/40 p-4">
      <div className="max-h-[90vh] w-full max-w-lg overflow-y-auto rounded-xl bg-white p-6 shadow-xl">
        <div className="mb-4 flex items-center justify-between">
          <h2 className="text-lg font-semibold text-slate-800">
            {mode === 'edit' ? 'Projeyi Düzenle' : 'Proje Ekle'}
          </h2>
          <button type="button" onClick={onClose} className="text-slate-400 hover:text-slate-600">
            ✕
          </button>
        </div>

        <form className="space-y-3" onSubmit={handleSubmit}>
          <div>
            <label className="mb-1 block text-xs font-medium text-slate-500">Proje Adı</label>
            <input
              type="text"
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="ör. Awesome Frozen Shoes Projesi"
              className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm"
              required
            />
          </div>

          <div>
            <label className="mb-1 block text-xs font-medium text-slate-500">Açıklama</label>
            <textarea
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Proje hakkında kısa bir açıklama…"
              rows={4}
              className="w-full resize-none rounded-lg border border-slate-200 px-3 py-2 text-sm"
            />
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="mb-1 block text-xs font-medium text-slate-500">Sponsor</label>
              <input
                type="text"
                value={sponsor}
                onChange={(e) => setSponsor(e.target.value)}
                placeholder="ör. Genel Müdür Yardımcısı"
                className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm"
              />
            </div>
            <div>
              <label className="mb-1 block text-xs font-medium text-slate-500">Öncelik</label>
              <select
                value={priority}
                onChange={(e) => setPriority(Number(e.target.value) as ProjectPriorityValue)}
                className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm"
              >
                {PRIORITY_OPTIONS.map((opt) => (
                  <option key={opt.value} value={opt.value}>
                    {opt.label}
                  </option>
                ))}
              </select>
            </div>
          </div>

          <div>
            <label className="mb-1 block text-xs font-medium text-slate-500">Proje Yöneticisi</label>
            <AsyncSearchSelect
              selectedLabel={pmLabel || null}
              onSearch={setPmQuery}
              options={(pmSearch.data?.items ?? []).map((e) => ({ id: e.id, label: e.name }))}
              isLoading={pmSearch.isLoading}
              onSelect={(option) => {
                setProjectManagerId(option.id);
                setProjectManagerLabel(option.label);
              }}
              placeholder="Kişi ara…"
            />
          </div>

          <div>
            <label className="mb-1 block text-xs font-medium text-slate-500">Stratejik Hedef</label>
            <input
              type="text"
              value={strategicGoal}
              onChange={(e) => setStrategicGoal(e.target.value)}
              placeholder="ör. Müşteri deneyimini iyileştirme"
              className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm"
            />
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="mb-1 block text-xs font-medium text-slate-500">Başlangıç Tarihi</label>
              <input
                type="date"
                value={startDate}
                onChange={(e) => setStartDate(e.target.value)}
                className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm"
              />
            </div>
            <div>
              <label className="mb-1 block text-xs font-medium text-slate-500">Bitiş Tarihi</label>
              <input
                type="date"
                value={endDate}
                min={startDate || undefined}
                onChange={(e) => setEndDate(e.target.value)}
                className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm"
              />
            </div>
          </div>

          {errorMessage && <p className="text-sm text-red-600">{errorMessage}</p>}

          <div className="mt-4 flex items-center justify-between">
            {mode === 'edit' ? (
              <button
                type="button"
                onClick={handleDelete}
                disabled={isPending}
                className="rounded-lg border border-red-200 px-4 py-2 text-sm font-medium text-red-600 hover:bg-red-50 disabled:opacity-50"
              >
                Pasife Al
              </button>
            ) : (
              <span />
            )}
            <div className="flex gap-2">
              <button
                type="button"
                onClick={onClose}
                className="rounded-lg border border-slate-200 px-4 py-2 text-sm font-medium text-slate-600 hover:bg-slate-50"
              >
                Vazgeç
              </button>
              <button
                type="submit"
                disabled={!canSubmit || isPending}
                className="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-700 disabled:cursor-not-allowed disabled:opacity-50"
              >
                {isPending ? 'Kaydediliyor…' : 'Kaydet'}
              </button>
            </div>
          </div>
        </form>
      </div>
    </div>
  );
}
