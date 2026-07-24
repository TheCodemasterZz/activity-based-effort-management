import { useDeleteProjectIssueMutation, useUpdateProjectIssueStatusMutation } from '../../../hooks/useProjectIssueMutations';
import { PROJECT_ISSUE_STATUS, type ProjectIssueDto } from '../../../api/types';

const PRIORITY_BADGE_CLASS: Record<string, string> = {
  Low: 'bg-emerald-100 text-emerald-700',
  Medium: 'bg-amber-100 text-amber-700',
  High: 'bg-orange-100 text-orange-700',
  Critical: 'bg-red-100 text-red-700',
};

const STATUS_LABEL: Record<string, { label: string; className: string }> = {
  Open: { label: 'Açık', className: 'bg-red-50 text-red-600' },
  InProgress: { label: 'Devam Ediyor', className: 'bg-amber-50 text-amber-700' },
  Resolved: { label: 'Çözüldü', className: 'bg-sky-50 text-sky-700' },
  Closed: { label: 'Kapalı', className: 'bg-slate-100 text-slate-500' },
};

function formatDateTr(date: string): string {
  return new Date(`${date}T00:00:00`).toLocaleDateString('tr-TR');
}

interface IssuesTabProps {
  issues: ProjectIssueDto[];
  resolveUser: (id: string | null) => string;
  onAdd: () => void;
  onEdit: (issue: ProjectIssueDto) => void;
}

export function IssuesTab({ issues, resolveUser, onAdd, onEdit }: IssuesTabProps) {
  const statusMutation = useUpdateProjectIssueStatusMutation();
  const deleteMutation = useDeleteProjectIssueMutation();
  const todayKey = new Date().toISOString().slice(0, 10);

  const handleDelete = async (issue: ProjectIssueDto) => {
    if (!window.confirm(`"${issue.title}" sorununu silmek istediğinize emin misiniz?`)) return;
    await deleteMutation.mutateAsync(issue.id);
  };

  const sorted = [...issues].sort((a, b) => (a.dueDate ?? '9999-99-99').localeCompare(b.dueDate ?? '9999-99-99'));

  return (
    <div className="rounded-xl border border-slate-200 bg-white">
      <div className="flex items-center justify-between border-b border-slate-200 p-4">
        <span className="text-sm font-semibold text-slate-700">Sorunlar ({issues.length})</span>
        <button type="button" onClick={onAdd} className="text-xs font-semibold text-indigo-600 hover:underline">
          + Sorun Ekle
        </button>
      </div>
      {sorted.length === 0 ? (
        <p className="p-4 text-sm text-slate-400">Henüz sorun kaydedilmemiş.</p>
      ) : (
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left text-xs font-medium uppercase tracking-wide text-slate-500">
              <th className="px-4 py-2">Sorun</th>
              <th className="px-4 py-2 text-center">Öncelik</th>
              <th className="px-4 py-2">Sorumlu</th>
              <th className="px-4 py-2">Son Tarih</th>
              <th className="px-4 py-2 text-center">Durum</th>
              <th className="px-4 py-2 text-center">İşlemler</th>
            </tr>
          </thead>
          <tbody>
            {sorted.map((issue) => {
              const isOverdue = !!issue.dueDate && issue.dueDate < todayKey && issue.status !== 'Closed' && issue.status !== 'Resolved';
              const status = STATUS_LABEL[issue.status] ?? { label: issue.status, className: 'bg-slate-100 text-slate-500' };
              return (
                <tr key={issue.id} className={`border-b border-slate-100 last:border-0 hover:bg-slate-50 ${isOverdue ? 'bg-red-50/50' : ''}`}>
                  <td className="px-4 py-2">
                    <div className="font-medium text-slate-700">{issue.title}</div>
                    {issue.description && <div className="max-w-[20rem] truncate text-xs text-slate-400">{issue.description}</div>}
                  </td>
                  <td className="px-4 py-2 text-center">
                    <span className={`inline-block rounded px-1.5 py-0.5 text-xs font-bold ${PRIORITY_BADGE_CLASS[issue.priority] ?? 'bg-slate-100 text-slate-500'}`}>
                      {issue.priority}
                    </span>
                  </td>
                  <td className="px-4 py-2 text-slate-500">{resolveUser(issue.ownerUserId)}</td>
                  <td className={`whitespace-nowrap px-4 py-2 ${isOverdue ? 'font-semibold text-red-600' : 'text-slate-500'}`}>
                    {issue.dueDate ? formatDateTr(issue.dueDate) : '—'}
                    {isOverdue && ' ⚠'}
                  </td>
                  <td className="px-4 py-2 text-center">
                    <select
                      value={issue.status}
                      onChange={(e) =>
                        statusMutation.mutate({
                          id: issue.id,
                          status: PROJECT_ISSUE_STATUS[e.target.value as keyof typeof PROJECT_ISSUE_STATUS],
                        })
                      }
                      className={`rounded-full border-0 px-2 py-0.5 text-xs font-medium ${status.className}`}
                    >
                      <option value="Open">Açık</option>
                      <option value="InProgress">Devam Ediyor</option>
                      <option value="Resolved">Çözüldü</option>
                      <option value="Closed">Kapalı</option>
                    </select>
                  </td>
                  <td className="px-4 py-2 text-center">
                    <div className="flex items-center justify-center gap-2.5">
                      <button type="button" onClick={() => onEdit(issue)} className="text-xs text-slate-400 hover:text-slate-600">
                        Düzenle
                      </button>
                      <button type="button" onClick={() => handleDelete(issue)} className="text-xs text-red-400 hover:text-red-600">
                        Sil
                      </button>
                    </div>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      )}
    </div>
  );
}
