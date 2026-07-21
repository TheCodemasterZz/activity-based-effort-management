import { useState } from 'react';
import { useDirectoryUsers } from '../../../hooks/useDirectoryUsers';
import type { DirectoryDto } from '../../../api/types';

interface DirectoryUserListProps {
  directory: DirectoryDto;
  onBack: () => void;
  onSelectUser: (userId: string) => void;
}

export function DirectoryUserList({ directory, onBack, onSelectUser }: DirectoryUserListProps) {
  const [searchTerm, setSearchTerm] = useState('');
  const users = useDirectoryUsers({ directoryId: directory.id, searchTerm });
  const items = users.data?.items ?? [];

  return (
    <div>
      <div className="mb-4 flex items-center justify-between">
        <h2 className="text-base font-semibold text-slate-800">{directory.name} — Kullanıcılar</h2>
        <button
          type="button"
          onClick={onBack}
          className="text-sm text-slate-500 hover:text-slate-700"
        >
          ← Listeye dön
        </button>
      </div>

      <input
        value={searchTerm}
        onChange={(e) => setSearchTerm(e.target.value)}
        placeholder="Kullanıcı adı, görünen ad veya e-posta ara"
        className="mb-4 w-full max-w-sm rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500"
      />

      {users.isLoading ? (
        <div className="py-8 text-center text-sm text-slate-400">Yükleniyor…</div>
      ) : items.length === 0 ? (
        <div className="rounded-xl border border-dashed border-slate-200 py-12 text-center text-sm text-slate-500">
          {searchTerm ? 'Aramayla eşleşen kullanıcı yok.' : 'Bu dizinde henüz kullanıcı yok.'}
        </div>
      ) : (
        <table className="w-full text-left text-sm">
          <thead>
            <tr className="border-b border-slate-100 text-xs uppercase tracking-wide text-slate-400">
              <th className="py-2 pr-4 font-medium">Kullanıcı Adı</th>
              <th className="py-2 pr-4 font-medium">Görünen Ad</th>
              <th className="py-2 pr-4 font-medium">E-posta</th>
              <th className="py-2 font-medium">Durum</th>
            </tr>
          </thead>
          <tbody>
            {items.map((user) => (
              <tr
                key={user.id}
                onClick={() => onSelectUser(user.id)}
                className="cursor-pointer border-b border-slate-50 last:border-0 hover:bg-slate-50"
              >
                <td className="py-2 pr-4 text-indigo-600">{user.username}</td>
                <td className="py-2 pr-4 text-slate-700">{user.displayName ?? '—'}</td>
                <td className="py-2 pr-4 text-slate-500">{user.email ?? '—'}</td>
                <td className="py-2">
                  <span
                    className={
                      'rounded-full px-2 py-0.5 text-xs font-medium ' +
                      (user.isActive
                        ? 'bg-emerald-50 text-emerald-700'
                        : 'bg-slate-100 text-slate-500')
                    }
                  >
                    {user.isActive ? 'Aktif' : 'Pasif'}
                  </span>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}
