import { useState } from 'react';
import { useDirectoryUsers } from '../../../hooks/useDirectoryUsers';
import type { DirectoryDto } from '../../../api/types';

interface DirectoryUserListProps {
  directory: DirectoryDto;
  onBack: () => void;
  onSelectUser: (userId: string) => void;
}

const PAGE_SIZE_OPTIONS = [25, 50, 100];

export function DirectoryUserList({ directory, onBack, onSelectUser }: DirectoryUserListProps) {
  const [searchTerm, setSearchTerm] = useState('');
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(PAGE_SIZE_OPTIONS[0]);
  const users = useDirectoryUsers({ directoryId: directory.id, searchTerm, pageNumber, pageSize });
  const items = users.data?.items ?? [];
  const totalCount = users.data?.totalCount ?? 0;
  const totalPages = users.data?.totalPages ?? 1;

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

      <div className="mb-4 flex flex-wrap items-center gap-3">
        <input
          value={searchTerm}
          onChange={(e) => {
            setSearchTerm(e.target.value);
            setPageNumber(1);
          }}
          placeholder="Kullanıcı adı, görünen ad veya e-posta ara"
          className="w-full max-w-sm rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500"
        />

        <label className="flex items-center gap-2 text-sm text-slate-500">
          Sayfa başına
          <select
            value={pageSize}
            onChange={(e) => {
              setPageSize(Number(e.target.value));
              setPageNumber(1);
            }}
            className="rounded-md border border-slate-300 px-2 py-1.5 text-sm outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500"
          >
            {PAGE_SIZE_OPTIONS.map((size) => (
              <option key={size} value={size}>
                {size}
              </option>
            ))}
          </select>
        </label>

        {totalCount > 0 && (
          <span className="text-sm text-slate-400">{totalCount} kullanıcı</span>
        )}
      </div>

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

      {totalPages > 1 && (
        <div className="mt-4 flex items-center justify-between">
          <span className="text-sm text-slate-400">
            Sayfa {pageNumber} / {totalPages}
          </span>
          <div className="flex items-center gap-1">
            <button
              type="button"
              onClick={() => setPageNumber((p) => Math.max(1, p - 1))}
              disabled={pageNumber <= 1}
              className="rounded-md border border-slate-300 px-3 py-1.5 text-sm text-slate-600 hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-40"
            >
              Önceki
            </button>
            {Array.from({ length: totalPages }, (_, i) => i + 1)
              .filter(
                (page) =>
                  page === 1 ||
                  page === totalPages ||
                  Math.abs(page - pageNumber) <= 1,
              )
              .map((page, index, pages) => (
                <span key={page} className="flex items-center">
                  {index > 0 && pages[index - 1] !== page - 1 && (
                    <span className="px-1 text-slate-300">…</span>
                  )}
                  <button
                    type="button"
                    onClick={() => setPageNumber(page)}
                    className={
                      'min-w-[2rem] rounded-md px-2 py-1.5 text-sm ' +
                      (page === pageNumber
                        ? 'bg-indigo-600 text-white'
                        : 'text-slate-600 hover:bg-slate-50')
                    }
                  >
                    {page}
                  </button>
                </span>
              ))}
            <button
              type="button"
              onClick={() => setPageNumber((p) => Math.min(totalPages, p + 1))}
              disabled={pageNumber >= totalPages}
              className="rounded-md border border-slate-300 px-3 py-1.5 text-sm text-slate-600 hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-40"
            >
              Sonraki
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
