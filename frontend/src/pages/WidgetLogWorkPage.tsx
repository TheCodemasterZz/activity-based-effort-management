import { useMemo, useState } from 'react';
import { WorkLogFormModal, type WorkLogFormInitialValues } from '../components/logentry/WorkLogFormModal';
import { useEmployeeById } from '../hooks/useEmployeeById';
import { useProjectDetail } from '../hooks/useProjects';
import { WORK_LOG_ENTRY_TYPE } from '../api/types';

/**
 * Uygulamanın geri kalanından bağımsız, chrome'suz (header/menü yok) tek başına açılabilen
 * "widget" sayfası — ör. Jira'daki "Log Work" butonundan `?widget=log-work&userId=...`
 * gibi bir URL ile derin bağlantı (deep link) olarak açılması hedeflenir. `widget=plan-work`
 * ile aynı sayfa Planned (Plan Work) modunda açılır. Bu uygulamada gerçek bir router yok;
 * App.tsx bu sayfayı `window.location.search`'teki `widget` parametresine bakarak normal
 * navigasyonun tamamen dışında, doğrudan render eder.
 */
export function WidgetLogWorkPage() {
  const params = useMemo(() => new URLSearchParams(window.location.search), []);
  const entryType =
    params.get('widget') === 'plan-work' ? WORK_LOG_ENTRY_TYPE.Planned : WORK_LOG_ENTRY_TYPE.Actual;
  // Bu bir prototip/demo geçidi — gerçek bir imza/JWT doğrulaması backend'de yapılmıyor
  // (Test Mode'da böyle bir altyapı yok), sadece linkte bir token parametresinin bulunması
  // zorunlu kılınıyor; token'sız açılan bir link doğrudan reddedilir.
  const tokenParam = params.get('token');
  // userId ve projectId zorunlu request alanlarıdır — widget'ın "kimin, hangi proje için"
  // log gireceği belli olmadan açılamaz. date/hours/description ise BİLEREK request'ten
  // alınmıyor: bunlar formda kullanıcının kendisinin dolduracağı alanlardır, URL üzerinden
  // önceden doldurulmaları istenmiyor.
  const userIdParam = params.get('userId');
  const projectIdParam = params.get('projectId');
  const activityL1IdParam = params.get('activityL1Id') ?? undefined;
  const activityL2IdParam = params.get('activityL2Id') ?? undefined;

  const employee = useEmployeeById(userIdParam);
  const project = useProjectDetail(projectIdParam);

  const [closed, setClosed] = useState(false);

  if (!tokenParam) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-slate-50 p-6">
        <div className="max-w-sm rounded-xl border border-red-200 bg-white p-8 text-center shadow-sm">
          <div className="mb-2 text-3xl">🔒</div>
          <p className="text-sm font-semibold text-red-700">Yetkisiz erişim</p>
          <p className="mt-2 text-sm text-slate-500">
            Bu link geçerli bir <code className="rounded bg-slate-100 px-1 py-0.5 text-xs">token</code> parametresi
            içermiyor. Lütfen bu widget'ı size sağlanan tam linkle açın.
          </p>
        </div>
      </div>
    );
  }

  if (!userIdParam || !projectIdParam) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-slate-50 p-6">
        <div className="max-w-sm rounded-xl border border-red-200 bg-white p-8 text-center shadow-sm">
          <div className="mb-2 text-3xl">⚠</div>
          <p className="text-sm font-semibold text-red-700">Eksik parametre</p>
          <p className="mt-2 text-sm text-slate-500">
            Bu widget için <code className="rounded bg-slate-100 px-1 py-0.5 text-xs">userId</code> ve{' '}
            <code className="rounded bg-slate-100 px-1 py-0.5 text-xs">projectId</code> parametrelerinin ikisi de
            zorunludur. Lütfen bu widget'ı size sağlanan tam linkle açın.
          </p>
        </div>
      </div>
    );
  }

  const isResolving = employee.isLoading || project.isLoading;

  if (closed) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-slate-50 p-6">
        <div className="rounded-xl border border-slate-200 bg-white p-8 text-center shadow-sm">
          <div className="mb-2 text-3xl">✅</div>
          <p className="text-sm font-medium text-slate-700">İşlem tamamlandı. Bu pencereyi kapatabilirsiniz.</p>
        </div>
      </div>
    );
  }

  if (isResolving) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-slate-50">
        <p className="text-sm text-slate-400">Yükleniyor…</p>
      </div>
    );
  }

  const initialValues: WorkLogFormInitialValues = {
    userId: employee.data?.id,
    userLabel: employee.data?.name,
    projectId: project.data?.id,
    projectLabel: project.data?.name,
    activityL1Id: activityL1IdParam,
    activityL2Id: activityL2IdParam,
  };

  return (
    <div className="min-h-screen bg-slate-50">
      <WorkLogFormModal
        mode="create"
        allowDateRange={false}
        initialValues={initialValues}
        entryType={entryType}
        onClose={() => setClosed(true)}
      />
    </div>
  );
}
