import { useMemo, useState } from 'react';
import { AsyncSearchSelect } from '../components/common/AsyncSearchSelect';
import { useEmployeeSearch } from '../hooks/useEmployees';
import { useProjectSearch } from '../hooks/useProjects';
import { pushSuccessNotification } from '../lib/notifications';

const PARAM_ROWS: { param: string; description: string; required: boolean }[] = [
  {
    param: 'widget',
    description:
      'log-work (gerçekleşen efor) veya plan-work (planlanan efor) — sayfanın hangi modda ve widget ' +
      'olarak (menüsüz/chrome\'suz) açılacağını belirler.',
    required: true,
  },
  {
    param: 'token',
    description:
      'Erişim token\'ı — bu olmadan widget "Yetkisiz erişim" gösterir. (Şu an sadece varlığı kontrol ediliyor; ' +
      'gerçek imza/JWT doğrulaması backend tarafında henüz yok, prod öncesi eklenmesi gerekir.)',
    required: true,
  },
  { param: 'employeeId', description: 'Kişinin GUID\'i — widget\'ın kimin adına açılacağını belirler.', required: true },
  { param: 'projectId', description: 'Projenin GUID\'i — widget\'ın hangi proje için açılacağını belirler.', required: true },
  { param: 'activityL1Id', description: 'Önceden seçili Activity L1 GUID\'i.', required: false },
  { param: 'activityL2Id', description: 'Önceden seçili Activity L2 GUID\'i.', required: false },
];

// date/hours/description BİLEREK bu listede yok ve URL'den okunmuyor — bunlar formda kullanıcının
// kendisinin dolduracağı alanlardır, request üzerinden önceden doldurulamaz (bkz. WidgetLogWorkPage.tsx).

type WidgetMode = 'log-work' | 'plan-work';

function buildWidgetUrl(mode: WidgetMode, params: Record<string, string | undefined>): string {
  const url = new URL(window.location.origin + window.location.pathname);
  url.searchParams.set('widget', mode);
  for (const [key, value] of Object.entries(params)) {
    if (value) url.searchParams.set(key, value);
  }
  return url.toString();
}

/**
 * Work Log giriş ekranının parametrik/gömülebilir "widget" sürümünü tanıtan ve örnek/test
 * linki üretmeyi kolaylaştıran, uygulama içi (normal navigasyona dahil) dokümantasyon sayfası.
 * Gerçek widget sayfasının kendisi (menüsüz sürüm) WidgetLogWorkPage.tsx'tedir.
 */
function generateDemoToken(): string {
  return Math.random().toString(36).slice(2) + Math.random().toString(36).slice(2);
}

export function WidgetsPage() {
  const [mode, setMode] = useState<WidgetMode>('log-work');
  const [token, setToken] = useState(generateDemoToken);
  const [employeeId, setEmployeeId] = useState('');
  const [employeeLabel, setEmployeeLabel] = useState('');
  const [employeeQuery, setEmployeeQuery] = useState('');
  const employeeSearch = useEmployeeSearch(employeeQuery);

  const [projectId, setProjectId] = useState('');
  const [projectLabel, setProjectLabel] = useState('');
  const [projectQuery, setProjectQuery] = useState('');
  const projectSearch = useProjectSearch(projectQuery, employeeId || null);

  const generatedUrl = useMemo(
    () => buildWidgetUrl(mode, { token, employeeId, projectId }),
    [mode, token, employeeId, projectId],
  );

  const exampleUrl = buildWidgetUrl(mode, {
    token: '<erişim-token\'ınız>',
    employeeId: '00000000-0000-0000-0000-000000000000',
    projectId: '00000000-0000-0000-0000-000000000000',
  });

  const copy = async (text: string) => {
    await navigator.clipboard.writeText(text);
    pushSuccessNotification('Link panoya kopyalandı.');
  };

  return (
    <div className="mx-auto max-w-4xl space-y-6 p-6">
      <div>
        <h1 className="text-xl font-semibold text-slate-800">Widgets</h1>
        <p className="mt-1 text-sm text-slate-500">
          Efor giriş formunu (gerçekleşen — Log Work, ya da planlanan — Plan Work), uygulamanın geri kalanı olmadan
          (menüsüz, tek başına) URL parametreleriyle önceden doldurulmuş şekilde açan gömülebilir bir sayfa.
          Örneğin Jira'daki bir "Log Work" butonuna bu linki bağlayarak, kişi ve proje bilgisi önceden dolu şekilde
          doğrudan efor girişine yönlendirebilirsiniz.
        </p>
      </div>

      <div className="rounded-xl border border-slate-200 bg-white p-4">
        <h2 className="mb-2 text-sm font-semibold text-slate-700">Desteklenen Parametreler</h2>
        <table className="w-full text-left text-sm">
          <thead>
            <tr className="border-b border-slate-100 text-xs uppercase tracking-wide text-slate-400">
              <th className="py-1.5 pr-4 font-medium">Parametre</th>
              <th className="py-1.5 pr-4 font-medium">Açıklama</th>
              <th className="py-1.5 font-medium">Zorunlu</th>
            </tr>
          </thead>
          <tbody>
            {PARAM_ROWS.map((row) => (
              <tr key={row.param} className="border-b border-slate-50 last:border-0">
                <td className="py-1.5 pr-4 font-mono text-xs text-indigo-700">{row.param}</td>
                <td className="py-1.5 pr-4 text-slate-600">{row.description}</td>
                <td className="py-1.5 text-slate-500">{row.required ? 'Evet' : 'Hayır'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div className="rounded-xl border border-slate-200 bg-white p-4">
        <h2 className="mb-2 text-sm font-semibold text-slate-700">Mod</h2>
        <div className="flex gap-1.5">
          {(['log-work', 'plan-work'] as const).map((m) => (
            <button
              key={m}
              type="button"
              onClick={() => setMode(m)}
              className={
                'rounded-lg border px-3 py-1.5 text-sm font-medium ' +
                (mode === m
                  ? 'border-indigo-600 bg-indigo-600 text-white'
                  : 'border-slate-200 text-slate-600 hover:bg-slate-50')
              }
            >
              {m === 'log-work' ? 'Log Work (gerçekleşen)' : 'Plan Work (planlanan)'}
            </button>
          ))}
        </div>
      </div>

      <div className="rounded-xl border border-slate-200 bg-white p-4">
        <h2 className="mb-2 text-sm font-semibold text-slate-700">Örnek Link</h2>
        <div className="flex items-center gap-2">
          <input
            readOnly
            value={exampleUrl}
            className="w-full min-w-0 rounded-lg border border-slate-200 bg-slate-50 px-3 py-2 font-mono text-xs text-slate-600"
          />
          <button
            type="button"
            onClick={() => copy(exampleUrl)}
            className="shrink-0 rounded-lg border border-slate-200 px-3 py-2 text-xs font-medium text-slate-600 hover:bg-slate-50"
          >
            Kopyala
          </button>
        </div>
      </div>

      <div className="rounded-xl border border-slate-200 bg-white p-4">
        <h2 className="mb-3 text-sm font-semibold text-slate-700">Test Linki Oluştur</h2>
        <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
          <div className="md:col-span-2">
            <label className="mb-1 block text-xs font-medium text-slate-500">Token</label>
            <div className="flex items-center gap-2">
              <input
                type="text"
                value={token}
                onChange={(e) => setToken(e.target.value)}
                className="w-full min-w-0 rounded-lg border border-slate-200 px-3 py-2 font-mono text-xs"
              />
              <button
                type="button"
                onClick={() => setToken(generateDemoToken())}
                className="shrink-0 rounded-lg border border-slate-200 px-3 py-2 text-xs font-medium text-slate-600 hover:bg-slate-50"
              >
                Yeniden Üret
              </button>
            </div>
          </div>
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
                setProjectId('');
                setProjectLabel('');
              }}
              placeholder="Kişi ara…"
            />
          </div>
          <div>
            <label className="mb-1 block text-xs font-medium text-slate-500">Proje</label>
            <AsyncSearchSelect
              selectedLabel={projectLabel || null}
              onSearch={setProjectQuery}
              options={(projectSearch.data?.items ?? []).map((p) => ({ id: p.id, label: p.name }))}
              isLoading={projectSearch.isLoading}
              onSelect={(option) => {
                setProjectId(option.id);
                setProjectLabel(option.label);
              }}
              placeholder="Proje ara…"
              disabled={!employeeId}
              disabledMessage="Önce kişi seçin"
            />
          </div>
        </div>

        <div className="mt-3 flex items-center gap-2">
          <input
            readOnly
            value={generatedUrl}
            className="w-full min-w-0 rounded-lg border border-slate-200 bg-slate-50 px-3 py-2 font-mono text-xs text-slate-600"
          />
          <button
            type="button"
            onClick={() => copy(generatedUrl)}
            className="shrink-0 rounded-lg border border-slate-200 px-3 py-2 text-xs font-medium text-slate-600 hover:bg-slate-50"
          >
            Kopyala
          </button>
          <a
            href={generatedUrl}
            target="_blank"
            rel="noreferrer"
            className="shrink-0 rounded-lg bg-indigo-600 px-3 py-2 text-xs font-semibold text-white hover:bg-indigo-700"
          >
            Yeni Sekmede Aç
          </a>
        </div>
      </div>
    </div>
  );
}
