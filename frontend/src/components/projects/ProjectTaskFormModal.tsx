import { useState } from 'react';
import { useCreateProjectTaskMutation, useUpdateProjectTaskMutation } from '../../hooks/useProjectTaskMutations';
import { useEmployeeSearch } from '../../hooks/useEmployees';
import { useEmployeeById } from '../../hooks/useEmployeeById';
import { AsyncSearchSelect } from '../common/AsyncSearchSelect';
import { ApiError } from '../../api/client';
import type { ProjectTaskDto } from '../../api/types';

interface ProjectTaskFormModalProps {
  projectId: string;
  task?: ProjectTaskDto;
  onClose: () => void;
}

function todayIso(): string {
  return new Date().toISOString().slice(0, 10);
}

export function ProjectTaskFormModal({ projectId, task, onClose }: ProjectTaskFormModalProps) {
  const isEdit = !!task;
  const [name, setName] = useState(task?.name ?? '');
  const [startDate, setStartDate] = useState(task?.startDate ?? todayIso());
  const [endDate, setEndDate] = useState(task?.endDate ?? todayIso());
  const [estimatedEffortHours, setEstimatedEffortHours] = useState(String(task?.estimatedEffortHours ?? ''));
  const [isMilestone, setIsMilestone] = useState(task?.isMilestone ?? false);
  const [assignedEmployeeId, setAssignedEmployeeId] = useState(task?.assignedEmployeeId ?? '');
  const [assigneeLabel, setAssigneeLabel] = useState('');
  const [assigneeQuery, setAssigneeQuery] = useState('');
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const existingAssignee = useEmployeeById(task?.assignedEmployeeId ?? null);
  const assigneeSearch = useEmployeeSearch(assigneeQuery);
  const resolvedAssigneeLabel = assigneeLabel || existingAssignee.data?.name || '';

  const createMutation = useCreateProjectTaskMutation();
  const updateMutation = useUpdateProjectTaskMutation();
  const isPending = createMutation.isPending || updateMutation.isPending;

  const hours = Number(estimatedEffortHours.replace(',', '.'));
  const canSubmit = name.trim().length > 0 && endDate >= startDate && !Number.isNaN(hours) && hours >= 0;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMessage(null);
    if (!canSubmit) return;

    try {
      if (isEdit && task) {
        await updateMutation.mutateAsync({
          id: task.id,
          payload: {
            name: name.trim(),
            startDate,
            endDate,
            estimatedEffortHours: hours,
            isMilestone,
            // WBS ilişkileri (üst görev/bağımlılık) bu formda düzenlenmiyor — mevcut değerleri
            // olduğu gibi geri gönderip kazara sıfırlanmalarını önlüyoruz.
            parentTaskId: task.parentTaskId,
            dependsOnTaskId: task.dependsOnTaskId,
            assignedEmployeeId: assignedEmployeeId || null,
          },
        });
      } else {
        await createMutation.mutateAsync({
          projectId,
          name: name.trim(),
          startDate,
          endDate,
          estimatedEffortHours: hours,
          isMilestone,
          assignedEmployeeId: assignedEmployeeId || null,
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
          <h2 className="text-lg font-semibold text-slate-800">{isEdit ? 'Görevi Düzenle' : 'Görev Ekle'}</h2>
          <button type="button" onClick={onClose} className="text-slate-400 hover:text-slate-600">
            ✕
          </button>
        </div>

        {isEdit && (
          <p className="mb-3 text-xs text-slate-400">
            Baseline (ilk onaylanan plan: {task!.baselineEffortHours}h, {task!.baselineEndDate}) burada değişmez —
            SPI hesabı bu ilk plana göre yapılmaya devam eder.
          </p>
        )}

        <form className="space-y-3" onSubmit={handleSubmit}>
          <div>
            <label className="mb-1 block text-xs font-medium text-slate-500">Görev Adı</label>
            <input
              type="text"
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="ör. Ekran Tasarımı"
              className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm"
              required
            />
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="mb-1 block text-xs font-medium text-slate-500">Başlangıç</label>
              <input
                type="date"
                value={startDate}
                onChange={(e) => setStartDate(e.target.value)}
                className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm"
              />
            </div>
            <div>
              <label className="mb-1 block text-xs font-medium text-slate-500">Bitiş</label>
              <input
                type="date"
                value={endDate}
                min={startDate}
                onChange={(e) => setEndDate(e.target.value)}
                className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm"
              />
            </div>
          </div>

          <div>
            <label className="mb-1 block text-xs font-medium text-slate-500">Tahmini Efor (saat)</label>
            <input
              type="text"
              value={estimatedEffortHours}
              onChange={(e) => setEstimatedEffortHours(e.target.value)}
              placeholder="ör. 16"
              className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm"
            />
          </div>

          <div>
            <label className="mb-1 block text-xs font-medium text-slate-500">Sorumlu</label>
            <AsyncSearchSelect
              selectedLabel={resolvedAssigneeLabel || null}
              onSearch={setAssigneeQuery}
              options={(assigneeSearch.data?.items ?? []).map((e) => ({ id: e.id, label: e.name }))}
              isLoading={assigneeSearch.isLoading}
              onSelect={(option) => {
                setAssignedEmployeeId(option.id);
                setAssigneeLabel(option.label);
              }}
              placeholder="Kişi ara…"
            />
          </div>

          <label className="flex items-center gap-2 text-xs font-medium text-slate-500">
            <input type="checkbox" checked={isMilestone} onChange={(e) => setIsMilestone(e.target.checked)} />
            Kilometre taşı (önemli bir tarih işareti)
          </label>

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
