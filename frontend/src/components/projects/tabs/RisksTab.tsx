import { useDeleteProjectRiskMutation, useUpdateProjectRiskStatusMutation } from '../../../hooks/useProjectRiskMutations';
import { riskSeverityTier } from '../../../lib/projectRisk';
import { PROJECT_RISK_STATUS, type ProjectRiskDto } from '../../../api/types';

const SEVERITY_BADGE_CLASS: Record<string, string> = {
  low: 'bg-emerald-100 text-emerald-700',
  medium: 'bg-amber-100 text-amber-700',
  high: 'bg-orange-100 text-orange-700',
  critical: 'bg-red-100 text-red-700',
};

const STATUS_LABEL: Record<string, { label: string; className: string }> = {
  Open: { label: 'Açık', className: 'bg-red-50 text-red-600' },
  Mitigating: { label: 'Azaltılıyor', className: 'bg-amber-50 text-amber-700' },
  Closed: { label: 'Kapalı', className: 'bg-slate-100 text-slate-500' },
};

function formatDateTr(date: string): string {
  return new Date(`${date}T00:00:00`).toLocaleDateString('tr-TR');
}

interface RisksTabProps {
  risks: ProjectRiskDto[];
  resolveEmployee: (id: string | null) => string;
  onAdd: () => void;
  onEdit: (risk: ProjectRiskDto) => void;
}

export function RisksTab({ risks, resolveEmployee, onAdd, onEdit }: RisksTabProps) {
  const statusMutation = useUpdateProjectRiskStatusMutation();
  const deleteMutation = useDeleteProjectRiskMutation();

  const handleDelete = async (risk: ProjectRiskDto) => {
    if (!window.confirm(`"${risk.title}" riskini silmek istediğinize emin misiniz?`)) return;
    await deleteMutation.mutateAsync(risk.id);
  };

  const sorted = [...risks].sort((a, b) => b.probability * b.impact - a.probability * a.impact);

  return (
    <div className="rounded-xl border border-slate-200 bg-white">
      <div className="flex items-center justify-between border-b border-slate-200 p-4">
        <span className="text-sm font-semibold text-slate-700">Riskler ({risks.length})</span>
        <button type="button" onClick={onAdd} className="text-xs font-semibold text-indigo-600 hover:underline">
          + Risk Ekle
        </button>
      </div>
      {sorted.length === 0 ? (
        <p className="p-4 text-sm text-slate-400">Henüz risk kaydedilmemiş.</p>
      ) : (
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left text-xs font-medium uppercase tracking-wide text-slate-500">
              <th className="px-4 py-2">Risk</th>
              <th className="px-4 py-2 text-center">Skor</th>
              <th className="px-4 py-2">Sorumlu</th>
              <th className="px-4 py-2">Tespit</th>
              <th className="px-4 py-2 text-center">Durum</th>
              <th className="px-4 py-2 text-center">İşlemler</th>
            </tr>
          </thead>
          <tbody>
            {sorted.map((risk) => {
              const tier = riskSeverityTier(risk.probability, risk.impact);
              const status = STATUS_LABEL[risk.status] ?? { label: risk.status, className: 'bg-slate-100 text-slate-500' };
              return (
                <tr key={risk.id} className="border-b border-slate-100 last:border-0 hover:bg-slate-50">
                  <td className="px-4 py-2">
                    <div className="font-medium text-slate-700">{risk.title}</div>
                    {risk.description && <div className="max-w-[20rem] truncate text-xs text-slate-400">{risk.description}</div>}
                  </td>
                  <td className="px-4 py-2 text-center">
                    <span className={`inline-block min-w-[2.5rem] rounded px-1.5 py-0.5 text-xs font-bold ${SEVERITY_BADGE_CLASS[tier]}`}>
                      {risk.probability * risk.impact}
                    </span>
                    <div className="mt-0.5 text-[10px] text-slate-400">
                      O:{risk.probability} × E:{risk.impact}
                    </div>
                  </td>
                  <td className="px-4 py-2 text-slate-500">{resolveEmployee(risk.ownerEmployeeId)}</td>
                  <td className="whitespace-nowrap px-4 py-2 text-slate-500">{formatDateTr(risk.identifiedDate)}</td>
                  <td className="px-4 py-2 text-center">
                    <select
                      value={risk.status}
                      onChange={(e) =>
                        statusMutation.mutate({
                          id: risk.id,
                          status: PROJECT_RISK_STATUS[e.target.value as keyof typeof PROJECT_RISK_STATUS],
                        })
                      }
                      className={`rounded-full border-0 px-2 py-0.5 text-xs font-medium ${status.className}`}
                    >
                      <option value="Open">Açık</option>
                      <option value="Mitigating">Azaltılıyor</option>
                      <option value="Closed">Kapalı</option>
                    </select>
                  </td>
                  <td className="px-4 py-2 text-center">
                    <div className="flex items-center justify-center gap-2.5">
                      <button type="button" onClick={() => onEdit(risk)} className="text-xs text-slate-400 hover:text-slate-600">
                        Düzenle
                      </button>
                      <button type="button" onClick={() => handleDelete(risk)} className="text-xs text-red-400 hover:text-red-600">
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
