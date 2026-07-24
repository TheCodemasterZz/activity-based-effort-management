import { useState } from 'react';
import { useLeaves } from '../../hooks/useLeaves';
import { useCreateLeaveMutation } from '../../hooks/useCreateLeaveMutation';
import { useDeleteLeaveMutation } from '../../hooks/useDeleteLeaveMutation';
import { ApiError } from '../../api/client';

interface LeaveScheduleModalProps {
  userId: string;
  userName: string;
  onClose: () => void;
}

function todayIso(): string {
  return new Date().toISOString().slice(0, 10);
}

function formatDateTr(dateIso: string): string {
  return new Date(`${dateIso}T00:00:00`).toLocaleDateString('tr-TR', { day: '2-digit', month: 'short', year: 'numeric' });
}

function formatTime(t: string | null): string {
  return t ? t.slice(0, 5) : '';
}

export function LeaveScheduleModal({ userId, userName, onClose }: LeaveScheduleModalProps) {
  const leaves = useLeaves({ userId });
  const createMutation = useCreateLeaveMutation();
  const deleteMutation = useDeleteLeaveMutation();

  const [isAdding, setIsAdding] = useState(false);
  const [startDate, setStartDate] = useState(todayIso());
  const [endDate, setEndDate] = useState(todayIso());
  const [isFullDay, setIsFullDay] = useState(true);
  const [startTime, setStartTime] = useState('09:00');
  const [endTime, setEndTime] = useState('12:00');
  const [description, setDescription] = useState('');
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const items = [...(leaves.data?.items ?? [])].sort((a, b) => b.startDate.localeCompare(a.startDate));

  const resetForm = () => {
    setStartDate(todayIso());
    setEndDate(todayIso());
    setIsFullDay(true);
    setStartTime('09:00');
    setEndTime('12:00');
    setDescription('');
    setErrorMessage(null);
  };

  const handleAdd = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMessage(null);
    try {
      await createMutation.mutateAsync({
        userId,
        startDate,
        endDate: isFullDay ? endDate : startDate,
        isFullDay,
        startTime: isFullDay ? null : `${startTime}:00`,
        endTime: isFullDay ? null : `${endTime}:00`,
        description: description.trim() || null,
      });
      resetForm();
      setIsAdding(false);
    } catch (err) {
      setErrorMessage(err instanceof ApiError ? err.message : 'Beklenmeyen bir hata oluştu.');
    }
  };

  const handleDelete = async (id: string) => {
    if (!window.confirm('Bu izin kaydını silmek istediğinize emin misiniz?')) return;
    await deleteMutation.mutateAsync(id);
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/40 p-4">
      <div className="w-full max-w-lg rounded-xl bg-white p-6 shadow-xl">
        <div className="mb-4 flex items-center justify-between">
          <div>
            <h2 className="text-lg font-semibold text-slate-800">İzin Programı</h2>
            <p className="text-xs text-slate-400">{userName}</p>
          </div>
          <button type="button" onClick={onClose} className="text-slate-400 hover:text-slate-600">
            ✕
          </button>
        </div>

        <div className="max-h-72 overflow-y-auto rounded-lg border border-slate-200">
          {leaves.isLoading ? (
            <div className="p-4 text-center text-xs text-slate-400">Yükleniyor…</div>
          ) : items.length === 0 ? (
            <div className="p-4 text-center text-xs text-slate-400">Kayıtlı izin yok.</div>
          ) : (
            <ul className="divide-y divide-slate-100">
              {items.map((leave) => (
                <li key={leave.id} className="flex items-center justify-between gap-2 px-3 py-2.5 text-sm">
                  <div className="min-w-0">
                    <div className="flex items-center gap-1.5">
                      <span className="font-medium text-slate-700">
                        {leave.startDate === leave.endDate
                          ? formatDateTr(leave.startDate)
                          : `${formatDateTr(leave.startDate)} – ${formatDateTr(leave.endDate)}`}
                      </span>
                      <span
                        className={`rounded-full px-1.5 py-0.5 text-[10px] font-semibold ${
                          leave.isFullDay ? 'bg-violet-100 text-violet-700' : 'bg-violet-50 text-violet-600'
                        }`}
                      >
                        {leave.isFullDay ? 'Tam Gün' : `${formatTime(leave.startTime)}–${formatTime(leave.endTime)}`}
                      </span>
                    </div>
                    {leave.description && <div className="truncate text-xs text-slate-400">{leave.description}</div>}
                  </div>
                  <button
                    type="button"
                    onClick={() => handleDelete(leave.id)}
                    className="shrink-0 text-xs font-medium text-red-500 hover:text-red-700"
                  >
                    Sil
                  </button>
                </li>
              ))}
            </ul>
          )}
        </div>

        {!isAdding ? (
          <button
            type="button"
            onClick={() => setIsAdding(true)}
            className="mt-3 w-full rounded-lg border border-dashed border-indigo-300 py-2 text-sm font-semibold text-indigo-600 hover:bg-indigo-50"
          >
            + İzin Ekle
          </button>
        ) : (
          <form className="mt-3 space-y-3 rounded-lg border border-slate-200 p-3" onSubmit={handleAdd}>
            <label className="flex items-center gap-2 text-xs font-medium text-slate-500">
              <input type="checkbox" checked={isFullDay} onChange={(e) => setIsFullDay(e.target.checked)} />
              Tam gün izin
            </label>

            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="mb-1 block text-xs font-medium text-slate-500">Başlangıç</label>
                <input
                  type="date"
                  value={startDate}
                  onChange={(e) => setStartDate(e.target.value)}
                  className="w-full rounded-lg border border-slate-200 px-2.5 py-1.5 text-sm"
                  required
                />
              </div>
              {isFullDay ? (
                <div>
                  <label className="mb-1 block text-xs font-medium text-slate-500">Bitiş</label>
                  <input
                    type="date"
                    value={endDate}
                    min={startDate}
                    onChange={(e) => setEndDate(e.target.value)}
                    className="w-full rounded-lg border border-slate-200 px-2.5 py-1.5 text-sm"
                    required
                  />
                </div>
              ) : (
                <div />
              )}
            </div>

            {!isFullDay && (
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="mb-1 block text-xs font-medium text-slate-500">Başlangıç Saati</label>
                  <input
                    type="time"
                    value={startTime}
                    onChange={(e) => setStartTime(e.target.value)}
                    className="w-full rounded-lg border border-slate-200 px-2.5 py-1.5 text-sm"
                    required
                  />
                </div>
                <div>
                  <label className="mb-1 block text-xs font-medium text-slate-500">Bitiş Saati</label>
                  <input
                    type="time"
                    value={endTime}
                    min={startTime}
                    onChange={(e) => setEndTime(e.target.value)}
                    className="w-full rounded-lg border border-slate-200 px-2.5 py-1.5 text-sm"
                    required
                  />
                </div>
              </div>
            )}

            <div>
              <label className="mb-1 block text-xs font-medium text-slate-500">Açıklama</label>
              <input
                type="text"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                placeholder="ör. Yıllık izin, doktor randevusu…"
                className="w-full rounded-lg border border-slate-200 px-2.5 py-1.5 text-sm"
              />
            </div>

            {errorMessage && <p className="text-xs text-red-600">{errorMessage}</p>}

            <div className="flex justify-end gap-2">
              <button
                type="button"
                onClick={() => {
                  setIsAdding(false);
                  resetForm();
                }}
                className="rounded-lg border border-slate-200 px-3 py-1.5 text-xs font-medium text-slate-600 hover:bg-slate-50"
              >
                Vazgeç
              </button>
              <button
                type="submit"
                disabled={createMutation.isPending}
                className="rounded-lg bg-indigo-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-indigo-700 disabled:opacity-50"
              >
                {createMutation.isPending ? 'Kaydediliyor…' : 'Kaydet'}
              </button>
            </div>
          </form>
        )}
      </div>
    </div>
  );
}
