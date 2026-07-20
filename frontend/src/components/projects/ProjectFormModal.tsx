import { useState } from 'react';
import { useCreateProjectMutation } from '../../hooks/useCreateProjectMutation';
import { useUpdateProjectMutation } from '../../hooks/useUpdateProjectMutation';
import { useDeleteProjectMutation } from '../../hooks/useDeleteProjectMutation';
import { ApiError } from '../../api/client';
import type { ProjectDto } from '../../api/types';

interface ProjectFormModalProps {
  mode: 'create' | 'edit';
  project?: ProjectDto;
  onClose: () => void;
  onDeleted?: () => void;
}

export function ProjectFormModal({ mode, project, onClose, onDeleted }: ProjectFormModalProps) {
  const [name, setName] = useState(project?.name ?? '');
  const [description, setDescription] = useState(project?.description ?? '');
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const createMutation = useCreateProjectMutation();
  const updateMutation = useUpdateProjectMutation();
  const deleteMutation = useDeleteProjectMutation();

  const isPending = createMutation.isPending || updateMutation.isPending || deleteMutation.isPending;
  const canSubmit = name.trim().length > 0;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMessage(null);

    try {
      const payload = { name: name.trim(), description: description.trim() || null };
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
      <div className="w-full max-w-md rounded-xl bg-white p-6 shadow-xl">
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
