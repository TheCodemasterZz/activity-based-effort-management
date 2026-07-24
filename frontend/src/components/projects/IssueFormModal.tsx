import { useState } from 'react';
import { useCreateProjectIssueMutation, useUpdateProjectIssueMutation } from '../../hooks/useProjectIssueMutations';
import { useEmployeeSearch } from '../../hooks/useEmployees';
import { useEmployeeById } from '../../hooks/useEmployeeById';
import { AsyncSearchSelect } from '../common/AsyncSearchSelect';
import { ApiError } from '../../api/client';
import { PROJECT_ISSUE_PRIORITY, type ProjectIssueDto, type ProjectIssuePriorityValue } from '../../api/types';

interface IssueFormModalProps {
  projectId: string;
  issue?: ProjectIssueDto;
  onClose: () => void;
}

const PRIORITY_OPTIONS: { value: ProjectIssuePriorityValue; label: string }[] = [
  { value: PROJECT_ISSUE_PRIORITY.Low, label: 'Düşük' },
  { value: PROJECT_ISSUE_PRIORITY.Medium, label: 'Orta' },
  { value: PROJECT_ISSUE_PRIORITY.High, label: 'Yüksek' },
  { value: PROJECT_ISSUE_PRIORITY.Critical, label: 'Kritik' },
];

export function IssueFormModal({ projectId, issue, onClose }: IssueFormModalProps) {
  const isEdit = !!issue;
  const [title, setTitle] = useState(issue?.title ?? '');
  const [description, setDescription] = useState(issue?.description ?? '');
  const [priority, setPriority] = useState<ProjectIssuePriorityValue>(
    PROJECT_ISSUE_PRIORITY[(issue?.priority as keyof typeof PROJECT_ISSUE_PRIORITY) ?? 'Medium'] ?? PROJECT_ISSUE_PRIORITY.Medium,
  );
  const [dueDate, setDueDate] = useState(issue?.dueDate ?? '');
  const [resolution, setResolution] = useState(issue?.resolution ?? '');
  const [ownerId, setOwnerId] = useState(issue?.ownerUserId ?? '');
  const [ownerLabel, setOwnerLabel] = useState('');
  const [ownerQuery, setOwnerQuery] = useState('');
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const existingOwner = useEmployeeById(issue?.ownerUserId ?? null);
  const ownerSearch = useEmployeeSearch(ownerQuery);
  const resolvedOwnerLabel = ownerLabel || existingOwner.data?.name || '';

  const createMutation = useCreateProjectIssueMutation();
  const updateMutation = useUpdateProjectIssueMutation();
  const isPending = createMutation.isPending || updateMutation.isPending;

  const canSubmit = title.trim().length > 0;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMessage(null);
    if (!canSubmit) return;

    try {
      if (isEdit && issue) {
        await updateMutation.mutateAsync({
          id: issue.id,
          payload: {
            title: title.trim(),
            description: description.trim() || null,
            priority,
            ownerUserId: ownerId || null,
            dueDate: dueDate || null,
            resolution: resolution.trim() || null,
          },
        });
      } else {
        await createMutation.mutateAsync({
          projectId,
          title: title.trim(),
          description: description.trim() || null,
          priority,
          ownerUserId: ownerId || null,
          dueDate: dueDate || null,
        });
      }
      onClose();
    } catch (err) {
      setErrorMessage(err instanceof ApiError ? err.message : 'Beklenmeyen bir hata oluştu.');
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/40 p-4">
      <div className="max-h-[90vh] w-full max-w-md overflow-y-auto rounded-xl bg-white p-6 shadow-xl">
        <div className="mb-4 flex items-center justify-between">
          <h2 className="text-lg font-semibold text-slate-800">{isEdit ? 'Sorunu Düzenle' : 'Sorun Ekle'}</h2>
          <button type="button" onClick={onClose} className="text-slate-400 hover:text-slate-600">
            ✕
          </button>
        </div>

        <form className="space-y-3" onSubmit={handleSubmit}>
          <div>
            <label className="mb-1 block text-xs font-medium text-slate-500">Başlık</label>
            <input
              type="text"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              placeholder="ör. Test ortamı erişim sorunu"
              className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm"
              required
            />
          </div>

          <div>
            <label className="mb-1 block text-xs font-medium text-slate-500">Açıklama</label>
            <textarea
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              rows={3}
              className="w-full resize-none rounded-lg border border-slate-200 px-3 py-2 text-sm"
            />
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="mb-1 block text-xs font-medium text-slate-500">Öncelik</label>
              <select
                value={priority}
                onChange={(e) => setPriority(Number(e.target.value) as ProjectIssuePriorityValue)}
                className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm"
              >
                {PRIORITY_OPTIONS.map((opt) => (
                  <option key={opt.value} value={opt.value}>
                    {opt.label}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label className="mb-1 block text-xs font-medium text-slate-500">Son Tarih</label>
              <input
                type="date"
                value={dueDate}
                onChange={(e) => setDueDate(e.target.value)}
                className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm"
              />
            </div>
          </div>

          <div>
            <label className="mb-1 block text-xs font-medium text-slate-500">Sorumlu</label>
            <AsyncSearchSelect
              selectedLabel={resolvedOwnerLabel || null}
              onSearch={setOwnerQuery}
              options={(ownerSearch.data?.items ?? []).map((e) => ({ id: e.id, label: e.name }))}
              isLoading={ownerSearch.isLoading}
              onSelect={(option) => {
                setOwnerId(option.id);
                setOwnerLabel(option.label);
              }}
              placeholder="Kişi ara…"
            />
          </div>

          {isEdit && (
            <div>
              <label className="mb-1 block text-xs font-medium text-slate-500">Çözüm Notu</label>
              <textarea
                value={resolution}
                onChange={(e) => setResolution(e.target.value)}
                rows={2}
                className="w-full resize-none rounded-lg border border-slate-200 px-3 py-2 text-sm"
              />
            </div>
          )}

          {errorMessage && <p className="text-sm text-red-600">{errorMessage}</p>}

          <div className="mt-4 flex justify-end gap-2">
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
        </form>
      </div>
    </div>
  );
}
