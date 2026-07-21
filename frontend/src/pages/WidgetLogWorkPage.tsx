import { useMemo, useState } from 'react';
import { WorkLogFormModal, type WorkLogFormInitialValues } from '../components/logentry/WorkLogFormModal';
import { useEmployeeById } from '../hooks/useEmployeeById';
import { useProjectDetail } from '../hooks/useProjects';
import { useCustomerSearch } from '../hooks/useCustomers';

/**
 * Uygulamanın geri kalanından bağımsız, chrome'suz (header/menü yok) tek başına açılabilen
 * "widget" sayfası — ör. Jira'daki "Log Work" butonundan `?widget=log-work&employeeId=...`
 * gibi bir URL ile derin bağlantı (deep link) olarak açılması hedeflenir. Bu uygulamada
 * gerçek bir router yok; App.tsx bu sayfayı `window.location.search`'teki `widget` parametresine
 * bakarak normal navigasyonun tamamen dışında, doğrudan render eder.
 */
export function WidgetLogWorkPage() {
  const params = useMemo(() => new URLSearchParams(window.location.search), []);
  // Bu bir prototip/demo geçidi — gerçek bir imza/JWT doğrulaması backend'de yapılmıyor
  // (Test Mode'da böyle bir altyapı yok), sadece linkte bir token parametresinin bulunması
  // zorunlu kılınıyor; token'sız açılan bir link doğrudan reddedilir.
  const tokenParam = params.get('token');
  const employeeIdParam = params.get('employeeId');
  const projectIdParam = params.get('projectId');
  const customerIdParam = params.get('customerId');
  const activityL1IdParam = params.get('activityL1Id') ?? undefined;
  const activityL2IdParam = params.get('activityL2Id') ?? undefined;
  const dateParam = params.get('date') ?? undefined;
  const hoursParam = params.get('hours');
  const descriptionParam = params.get('description') ?? undefined;

  const employee = useEmployeeById(employeeIdParam);
  const project = useProjectDetail(projectIdParam);
  // Müşteri adı için özel bir getById uç noktası yok — mevcut proje-bağımlı müşteri aramasıyla
  // (diğer formlarda da kullanılan aynı desen) proje kapsamındaki müşteriler çekilip id'ye göre
  // eşleştirilir.
  const customerSearch = useCustomerSearch('', projectIdParam);
  const customerLabel = customerIdParam
    ? customerSearch.data?.items.find((c) => c.id === customerIdParam)?.name
    : undefined;

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

  const isResolving =
    (!!employeeIdParam && employee.isLoading) ||
    (!!projectIdParam && project.isLoading) ||
    (!!customerIdParam && customerSearch.isLoading);

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
    employeeId: employee.data?.id,
    employeeLabel: employee.data?.name,
    projectId: project.data?.id,
    projectLabel: project.data?.name,
    customerId: customerIdParam ?? undefined,
    customerLabel,
    activityL1Id: activityL1IdParam,
    activityL2Id: activityL2IdParam,
    date: dateParam,
    hours: hoursParam ? Number(hoursParam) : undefined,
    description: descriptionParam,
  };

  return (
    <div className="min-h-screen bg-slate-50">
      <WorkLogFormModal mode="create" allowDateRange={false} initialValues={initialValues} onClose={() => setClosed(true)} />
    </div>
  );
}
