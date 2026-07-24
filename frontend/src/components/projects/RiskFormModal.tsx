import { useState } from 'react';
import { useCreateProjectRiskMutation, useUpdateProjectRiskMutation } from '../../hooks/useProjectRiskMutations';
import { useUserSearch } from '../../hooks/useUserRoster';
import { useUserById } from '../../hooks/useUserRoster';
import { AsyncSearchSelect } from '../common/AsyncSearchSelect';
import { ApiError } from '../../api/client';
import type { ProjectRiskDto } from '../../api/types';

interface RiskFormModalProps {
  projectId: string;
  risk?: ProjectRiskDto;
  onClose: () => void;
}

function todayIso(): string {
  return new Date().toISOString().slice(0, 10);
}

export function RiskFormModal({ projectId, risk, onClose }: RiskFormModalProps) {
  const isEdit = !!risk;
  const [title, setTitle] = useState(risk?.title ?? '');
  const [description, setDescription] = useState(risk?.description ?? '');
  const [probability, setProbability] = useState(risk?.probability ?? 3);
  const [impact, setImpact] = useState(risk?.impact ?? 3);
  const [mitigationPlan, setMitigationPlan] = useState(risk?.mitigationPlan ?? '');
  const [identifiedDate, setIdentifiedDate] = useState(risk?.identifiedDate ?? todayIso());
  const [ownerId, setOwnerId] = useState(risk?.ownerUserId ?? '');
  const [ownerLabel, setOwnerLabel] = useState('');
  const [ownerQuery, setOwnerQuery] = useState('');
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const existingOwner = useUserById(risk?.ownerUserId ?? null);
  const ownerSearch = useUserSearch(ownerQuery);
  const resolvedOwnerLabel = ownerLabel || existingOwner.data?.name || '';

  const createMutation = useCreateProjectRiskMutation();
  const updateMutation = useUpdateProjectRiskMutation();
  const isPending = createMutation.isPending || updateMutation.isPending;

  const canSubmit = title.trim().length > 0;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMessage(null);
    if (!canSubmit) return;

    try {
      const payload = {
        title: title.trim(),
        description: description.trim() || null,
        probability,
        impact,
        mitigationPlan: mitigationPlan.trim() || null,
        ownerUserId: ownerId || null,
        identifiedDate,
      };
      if (isEdit && risk) {
        await updateMutation.mutateAsync({ id: risk.id, payload });
      } else {
        await createMutation.mutateAsync({ projectId, ...payload });
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
          <h2 className="text-lg font-semibold text-slate-800">{isEdit ? 'Riski Düzenle' : 'Risk Ekle'}</h2>
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
              placeholder="ör. Anahtar personel kaybı riski"
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
              <label className="mb-1 block text-xs font-medium text-slate-500">Olasılık (1-5)</label>
              <input
                type="number"
                min={1}
                max={5}
                value={probability}
                onChange={(e) => setProbability(Number(e.target.value))}
                className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm"
              />
            </div>
            <div>
              <label className="mb-1 block text-xs font-medium text-slate-500">Etki (1-5)</label>
              <input
                type="number"
                min={1}
                max={5}
                value={impact}
                onChange={(e) => setImpact(Number(e.target.value))}
                className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm"
              />
            </div>
          </div>

          <div>
            <label className="mb-1 block text-xs font-medium text-slate-500">Tespit Tarihi</label>
            <input
              type="date"
              value={identifiedDate}
              onChange={(e) => setIdentifiedDate(e.target.value)}
              className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm"
            />
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

          <div>
            <label className="mb-1 block text-xs font-medium text-slate-500">Azaltım Planı</label>
            <textarea
              value={mitigationPlan}
              onChange={(e) => setMitigationPlan(e.target.value)}
              rows={2}
              className="w-full resize-none rounded-lg border border-slate-200 px-3 py-2 text-sm"
            />
          </div>

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
