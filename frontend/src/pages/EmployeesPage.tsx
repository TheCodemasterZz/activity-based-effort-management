import { useMemo, useState } from 'react';
import { useUserRoster } from '../hooks/useUserRoster';
import { LeaveScheduleModal } from '../components/leaves/LeaveScheduleModal';
import { ErrorState } from '../components/common/ErrorState';

export function EmployeesPage() {
  const users = useUserRoster();
  const [search, setSearch] = useState('');
  const [selectedUser, setSelectedUser] = useState<{ id: string; name: string } | null>(null);

  const filteredItems = useMemo(() => {
    const items = users.data?.items ?? [];
    const query = search.trim().toLocaleLowerCase('tr');
    if (!query) return items;
    return items.filter((e) => e.name.toLocaleLowerCase('tr').includes(query));
  }, [users.data, search]);

  return (
    <div className="flex flex-1 flex-col overflow-y-auto bg-slate-50 p-6">
      <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-lg font-semibold text-slate-800">Çalışanlar</h1>
        <input
          type="text"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Çalışan ara…"
          className="w-56 rounded-lg border border-slate-200 px-3 py-2 text-sm"
        />
      </div>

      {users.isError ? (
        <ErrorState />
      ) : (
        <div className="overflow-hidden rounded-xl border border-slate-200 bg-white">
          {users.isLoading ? (
            <div className="p-8 text-center text-slate-400">Yükleniyor…</div>
          ) : filteredItems.length === 0 ? (
            <div className="p-8 text-center text-slate-400">Çalışan bulunamadı.</div>
          ) : (
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-slate-200 bg-slate-50 text-left text-xs font-medium uppercase tracking-wide text-slate-500">
                  <th className="px-4 py-3">Ad Soyad</th>
                  <th className="px-4 py-3">E-posta</th>
                  <th className="px-4 py-3 text-right">İşlemler</th>
                </tr>
              </thead>
              <tbody>
                {filteredItems.map((user) => (
                  <tr key={user.id} className="border-b border-slate-100 last:border-0 hover:bg-slate-50">
                    <td className="px-4 py-3 font-medium text-slate-800">{user.name}</td>
                    <td className="px-4 py-3 text-slate-500">{user.email ?? '—'}</td>
                    <td className="px-4 py-3 text-right">
                      <button
                        type="button"
                        onClick={() => setSelectedUser({ id: user.id, name: user.name })}
                        className="rounded-lg border border-slate-200 px-2.5 py-1.5 text-xs font-medium text-slate-600 hover:bg-slate-100"
                      >
                        İzin Programı
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      )}

      {selectedUser && (
        <LeaveScheduleModal
          userId={selectedUser.id}
          userName={selectedUser.name}
          onClose={() => setSelectedUser(null)}
        />
      )}
    </div>
  );
}
