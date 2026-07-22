import { useState } from 'react';
import { useDirectories } from '../../../hooks/useDirectories';
import { OrgChart } from './OrgChart';
import { DirectoryUserCardModal } from './DirectoryUserCardModal';

const ACTIVE_DIRECTORY_SOURCE = 1;

export function OrgChartSection() {
  const directories = useDirectories();
  const adDirectories = (directories.data?.items ?? []).filter(
    (d) => d.source === ACTIVE_DIRECTORY_SOURCE,
  );
  const [selectedDirectoryId, setSelectedDirectoryId] = useState<string | null>(null);
  const [selectedUserId, setSelectedUserId] = useState<string | null>(null);
  const effectiveDirectoryId = selectedDirectoryId ?? adDirectories[0]?.id ?? null;

  if (directories.isLoading) {
    return <div className="py-8 text-center text-sm text-slate-400">Yükleniyor…</div>;
  }

  if (adDirectories.length === 0) {
    return (
      <div className="rounded-xl border border-dashed border-slate-200 py-12 text-center text-sm text-slate-500">
        Önce Kullanıcı Klasörü'nden bir Active Directory dizini tanımlayın.
      </div>
    );
  }

  return (
    <div>
      <label className="mb-4 block max-w-sm">
        <span className="mb-1 block text-xs font-medium text-slate-600">Dizin</span>
        <select
          value={effectiveDirectoryId ?? ''}
          onChange={(e) => setSelectedDirectoryId(e.target.value)}
          className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500"
        >
          {adDirectories.map((directory) => (
            <option key={directory.id} value={directory.id}>
              {directory.name}
            </option>
          ))}
        </select>
      </label>

      {effectiveDirectoryId && (
        <OrgChart
          key={effectiveDirectoryId}
          directoryId={effectiveDirectoryId}
          onSelectUser={setSelectedUserId}
        />
      )}

      {selectedUserId && (
        <DirectoryUserCardModal
          userId={selectedUserId}
          onClose={() => setSelectedUserId(null)}
          onSelectUser={setSelectedUserId}
        />
      )}
    </div>
  );
}
