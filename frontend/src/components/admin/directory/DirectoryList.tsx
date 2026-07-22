import { useState } from 'react';
import { ApiError } from '../../../api/client';
import { useDirectories } from '../../../hooks/useDirectories';
import {
  useDeleteDirectoryMutation,
  useSyncDirectoryMutation,
} from '../../../hooks/useDirectoryMutations';
import type { DirectoryDto } from '../../../api/types';

const SOURCE_LABEL: Record<number, string> = {
  0: 'Internal',
  1: 'Active Directory',
};

const SCHEDULE_LABEL: Record<number, string> = {
  0: 'Kapalı',
  1: 'Saatlik',
  2: 'Günlük',
  3: 'Haftalık',
};

interface DirectoryListProps {
  onAdd: () => void;
  onEdit: (directory: DirectoryDto) => void;
  onViewAttributeMappings: (directory: DirectoryDto) => void;
}

function formatLastSynced(value: string | null): string {
  if (!value) return 'Hiç senkronize edilmedi';
  return new Date(value).toLocaleString('tr-TR');
}

export function DirectoryList({ onAdd, onEdit, onViewAttributeMappings }: DirectoryListProps) {
  const directories = useDirectories();
  const syncMutation = useSyncDirectoryMutation();
  const deleteMutation = useDeleteDirectoryMutation();
  const [message, setMessage] = useState<{ text: string; isError: boolean } | null>(null);
  // Hangi dizinin senkronize edildiğini tutar; aksi halde tek bir mutation'ın isPending'i
  // tüm satırları aynı anda "senkronize ediliyor" gösterirdi.
  const [syncingId, setSyncingId] = useState<string | null>(null);

  const handleSync = async (directory: DirectoryDto) => {
    setMessage(null);
    setSyncingId(directory.id);
    try {
      const result = await syncMutation.mutateAsync(directory.id);
      setMessage({
        text: `${result.directoryName}: ${result.added} eklendi, ${result.updated} güncellendi, ${result.deactivated} pasife alındı.`,
        isError: false,
      });
    } catch (error) {
      setMessage({
        text: error instanceof ApiError ? error.message : 'Senkronizasyon başarısız oldu.',
        isError: true,
      });
    } finally {
      setSyncingId(null);
    }
  };

  const handleDelete = async (directory: DirectoryDto) => {
    if (!window.confirm(`"${directory.name}" dizinini silmek istediğinize emin misiniz?`)) return;

    setMessage(null);
    try {
      await deleteMutation.mutateAsync(directory.id);
    } catch (error) {
      setMessage({
        text: error instanceof ApiError ? error.message : 'Dizin silinemedi.',
        isError: true,
      });
    }
  };

  const items = directories.data?.items ?? [];

  return (
    <div>
      <div className="mb-4 flex items-center justify-between">
        <p className="text-sm text-slate-500">
          Kullanıcıların çekileceği dizinler. Birden fazla dizin tanımlanabilir.
        </p>
        <button
          type="button"
          onClick={onAdd}
          className="rounded-lg bg-indigo-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-indigo-700"
        >
          Yeni Dizin Ekle
        </button>
      </div>

      {message && (
        <p
          role="status"
          className={
            'mb-4 rounded-md px-3 py-2 text-sm ' +
            (message.isError ? 'bg-rose-50 text-rose-700' : 'bg-emerald-50 text-emerald-700')
          }
        >
          {message.text}
        </p>
      )}

      {directories.isLoading ? (
        <div className="py-8 text-center text-sm text-slate-400">Yükleniyor…</div>
      ) : items.length === 0 ? (
        <div className="rounded-xl border border-dashed border-slate-200 py-12 text-center text-sm text-slate-500">
          Henüz dizin tanımlanmamış.
        </div>
      ) : (
        <table className="w-full text-left text-sm">
          <thead>
            <tr className="border-b border-slate-100 text-xs uppercase tracking-wide text-slate-400">
              <th className="py-2 pr-4 font-medium">Ad</th>
              <th className="py-2 pr-4 font-medium">Tip</th>
              <th className="py-2 pr-4 font-medium">Sunucu</th>
              <th className="py-2 pr-4 font-medium">Zamanlama</th>
              <th className="py-2 pr-4 font-medium">Son Senkron</th>
              <th className="py-2 font-medium">İşlemler</th>
            </tr>
          </thead>
          <tbody>
            {items.map((directory) => (
              <tr key={directory.id} className="border-b border-slate-50 last:border-0">
                <td className="py-2 pr-4 text-slate-700">
                  {directory.name}
                  {!directory.isActive && (
                    <span className="ml-2 rounded-full bg-slate-100 px-2 py-0.5 text-xs text-slate-500">
                      Pasif
                    </span>
                  )}
                </td>
                <td className="py-2 pr-4 text-slate-500">{SOURCE_LABEL[directory.source] ?? '—'}</td>
                <td className="py-2 pr-4 text-slate-500">
                  {directory.hostname ? `${directory.hostname}:${directory.port}` : '—'}
                </td>
                <td className="py-2 pr-4 text-slate-500">
                  {SCHEDULE_LABEL[directory.syncSchedule] ?? '—'}
                </td>
                <td className="py-2 pr-4 text-slate-500">
                  {directory.source === 1 ? formatLastSynced(directory.lastSyncedUtc) : '—'}
                </td>
                <td className="py-2">
                  <div className="flex gap-2 text-xs">
                    {directory.source === 1 && (
                      <button
                        type="button"
                        onClick={() => onViewAttributeMappings(directory)}
                        className="text-indigo-600 hover:underline"
                      >
                        AD Attributes
                      </button>
                    )}
                    {directory.source === 1 && (
                      <button
                        type="button"
                        onClick={() => handleSync(directory)}
                        disabled={syncingId !== null}
                        className="text-indigo-600 hover:underline disabled:text-slate-300"
                      >
                        {syncingId === directory.id ? 'Senkronize ediliyor…' : 'Senkronize Et'}
                      </button>
                    )}
                    <button
                      type="button"
                      onClick={() => onEdit(directory)}
                      className="text-slate-600 hover:underline"
                    >
                      Düzenle
                    </button>
                    <button
                      type="button"
                      onClick={() => handleDelete(directory)}
                      className="text-rose-600 hover:underline"
                    >
                      Sil
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}
