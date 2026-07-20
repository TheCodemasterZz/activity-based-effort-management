import { useMemo, useState } from 'react';
import { addWeeks, endOfWeek, format, startOfWeek } from 'date-fns';
import { tr } from 'date-fns/locale/tr';
import { AsyncSearchSelect } from '../common/AsyncSearchSelect';
import { useEmployeeSearch } from '../../hooks/useEmployees';
import { useCreateWorkLogApprovalMutation } from '../../hooks/useCreateWorkLogApprovalMutation';
import { ApiError } from '../../api/client';

interface WorkLogApprovalModalProps {
  onClose: () => void;
  onApproved?: () => void;
}

const WEEK_OFFSET_TABS: { value: -1 | 0 | 1; label: string }[] = [
  { value: -1, label: 'Önceki Hafta' },
  { value: 0, label: 'Bu Hafta' },
  { value: 1, label: 'Sonraki Hafta' },
];

function dateKey(d: Date): string {
  return format(d, 'yyyy-MM-dd');
}

/** Onay sadece tam hafta (Pazartesi–Pazar) bazında verilebilir — bu yüzden serbest tarih girişi
 * yerine "Önceki/Bu/Sonraki hafta" seçimi kullanılıyor; bir haftanın yalnızca bir kısmının
 * onaylanması (ör. Pazartesi onaylı, Salı onaysız, Çarşamba onaylı) böylece hiç mümkün olmuyor. */
export function WorkLogApprovalModal({ onClose, onApproved }: WorkLogApprovalModalProps) {
  const [employeeId, setEmployeeId] = useState('');
  const [employeeLabel, setEmployeeLabel] = useState('');
  const [employeeQuery, setEmployeeQuery] = useState('');
  const employeeSearch = useEmployeeSearch(employeeQuery);

  const [weekOffset, setWeekOffset] = useState<-1 | 0 | 1>(0);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const approveMutation = useCreateWorkLogApprovalMutation();

  const week = useMemo(() => {
    const base = addWeeks(new Date(), weekOffset);
    const start = startOfWeek(base, { weekStartsOn: 1 });
    const end = endOfWeek(base, { weekStartsOn: 1 });
    return {
      startKey: dateKey(start),
      endKey: dateKey(end),
      label: `${format(start, 'd MMM', { locale: tr })} – ${format(end, 'd MMM yyyy', { locale: tr })}`,
    };
  }, [weekOffset]);

  const canSubmit = !!employeeId;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMessage(null);
    setSuccessMessage(null);
    try {
      await approveMutation.mutateAsync({
        employeeId,
        periodType: 1,
        periodStart: week.startKey,
        periodEnd: week.endKey,
      });
      setSuccessMessage(`${employeeLabel} için ${week.label} haftası onaylandı.`);
      onApproved?.();
    } catch (err) {
      setErrorMessage(err instanceof ApiError ? err.message : 'Beklenmeyen bir hata oluştu.');
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/40 p-4">
      <div className="w-full max-w-md rounded-xl bg-white p-6 shadow-xl">
        <div className="mb-4 flex items-center justify-between">
          <h2 className="text-lg font-semibold text-slate-800">Efor Onayı</h2>
          <button type="button" onClick={onClose} className="text-slate-400 hover:text-slate-600">
            ✕
          </button>
        </div>

        <form className="space-y-3" onSubmit={handleSubmit}>
          <div>
            <label className="mb-1 block text-xs font-medium text-slate-500">Kişi</label>
            <AsyncSearchSelect
              selectedLabel={employeeLabel || null}
              onSearch={setEmployeeQuery}
              options={(employeeSearch.data?.items ?? []).map((e) => ({ id: e.id, label: e.name }))}
              isLoading={employeeSearch.isLoading}
              onSelect={(option) => {
                setEmployeeId(option.id);
                setEmployeeLabel(option.label);
              }}
              placeholder="Kişi ara…"
            />
          </div>

          <div>
            <label className="mb-1 block text-xs font-medium text-slate-500">Hafta</label>
            <div className="flex gap-1.5">
              {WEEK_OFFSET_TABS.map((tab) => (
                <button
                  key={tab.value}
                  type="button"
                  onClick={() => setWeekOffset(tab.value)}
                  className={
                    'flex-1 rounded-lg border px-2.5 py-1.5 text-sm font-medium ' +
                    (weekOffset === tab.value
                      ? 'border-indigo-600 bg-indigo-600 text-white'
                      : 'border-slate-200 text-slate-600 hover:bg-slate-50')
                  }
                >
                  {tab.label}
                </button>
              ))}
            </div>
            <div className="mt-2 rounded-lg bg-slate-50 px-3 py-2 text-center text-sm font-medium text-slate-700">
              {week.label}
            </div>
          </div>

          <p className="text-xs text-slate-400">
            Seçilen kişinin bu haftadaki (Pazartesi–Pazar) tüm efor kayıtları toplu olarak onaylanır ve bundan sonra
            değiştirilemez/silinemez. Onay her zaman tam bir haftayı kapsar.
          </p>

          {errorMessage && <p className="text-sm text-red-600">{errorMessage}</p>}
          {successMessage && <p className="text-sm text-emerald-600">{successMessage}</p>}

          <div className="mt-4 flex justify-end gap-2">
            <button
              type="button"
              onClick={onClose}
              className="rounded-lg border border-slate-200 px-4 py-2 text-sm font-medium text-slate-600 hover:bg-slate-50"
            >
              Kapat
            </button>
            <button
              type="submit"
              disabled={!canSubmit || approveMutation.isPending}
              className="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-700 disabled:cursor-not-allowed disabled:opacity-50"
            >
              {approveMutation.isPending ? 'Onaylanıyor…' : 'Onayla'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
