import { useMemo } from 'react';
import { useEmployees } from '../hooks/useEmployees';
import { useProjects } from '../hooks/useProjects';
import { useCustomers } from '../hooks/useCustomers';
import { useAllActivities } from '../hooks/useActivities';
import { useHolidays } from '../hooks/useHolidays';
import { useWorkLogs } from '../hooks/useWorkLogs';
import { getPeriodRange } from '../lib/dateUtils';
import { DashboardAreaChart } from '../components/dashboard/charts/DashboardAreaChart';
import { DashboardRankingBar } from '../components/dashboard/charts/DashboardRankingBar';

interface KpiCardProps {
  icon: string;
  iconBg: string;
  label: string;
  value: string;
  caption: string;
}

function KpiCard({ icon, iconBg, label, value, caption }: KpiCardProps) {
  return (
    <div className="flex-1 rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
      <div className="flex items-center gap-2">
        <span className={`flex h-9 w-9 items-center justify-center rounded-lg text-base ${iconBg}`}>{icon}</span>
        <span className="text-xs font-semibold uppercase tracking-wide text-slate-400">{label}</span>
      </div>
      <div className="mt-3 text-2xl font-bold text-slate-800">{value}</div>
      <div className="mt-1 text-xs text-slate-400">{caption}</div>
    </div>
  );
}

interface PanelProps {
  title: string;
  caption?: string;
  children: React.ReactNode;
}

function Panel({ title, caption, children }: PanelProps) {
  return (
    <div className="flex-1 rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
      <div className="mb-3">
        <div className="text-sm font-semibold text-slate-700">{title}</div>
        {caption && <div className="text-xs text-slate-400">{caption}</div>}
      </div>
      {children}
    </div>
  );
}

function formatDateTr(dateIso: string): string {
  return new Date(`${dateIso}T00:00:00`).toLocaleDateString('tr-TR', { day: '2-digit', month: 'long', weekday: 'long' });
}

export function HomePage() {
  const employees = useEmployees();
  const projects = useProjects();
  const customers = useCustomers();
  const activities = useAllActivities();
  const holidays = useHolidays();

  const monthRange = useMemo(() => getPeriodRange('daily', new Date()), []);
  const workLogs = useWorkLogs(monthRange.startKey, monthRange.endKey);

  const employeesById = useMemo(
    () => new Map(employees.data?.items.map((e) => [e.id, e.name]) ?? []),
    [employees.data],
  );
  const projectsById = useMemo(() => new Map(projects.data?.items.map((p) => [p.id, p.name]) ?? []), [projects.data]);
  const activitiesById = useMemo(
    () => new Map(activities.data?.items.map((a) => [a.id, a.name]) ?? []),
    [activities.data],
  );

  const logs = workLogs.data?.items ?? [];
  const todayKey = new Date().toISOString().slice(0, 10);

  const totalHours = logs.reduce((sum, l) => sum + l.hours, 0);
  const activeEmployeeCount = new Set(logs.map((l) => l.employeeId)).size;
  const totalEmployeeCount = employees.data?.items.length ?? 0;
  const totalProjectCount = projects.data?.items.length ?? 0;
  const totalCustomerCount = customers.data?.items.length ?? 0;

  const dailyTrend = useMemo(() => {
    const hoursByDate = new Map<string, number>();
    for (const log of logs) hoursByDate.set(log.workDate, (hoursByDate.get(log.workDate) ?? 0) + log.hours);
    return monthRange.columns
      .filter((c) => c.startKey <= todayKey)
      .map((c) => ({ label: c.label, value: hoursByDate.get(c.key) ?? 0 }));
  }, [logs, monthRange, todayKey]);

  const rankBy = (selectId: (id: string) => string | undefined, keyOf: (l: (typeof logs)[number]) => string) => {
    const hoursById = new Map<string, number>();
    for (const log of logs) {
      const key = keyOf(log);
      hoursById.set(key, (hoursById.get(key) ?? 0) + log.hours);
    }
    return [...hoursById.entries()]
      .map(([id, hours]) => ({ label: selectId(id) ?? 'Bilinmeyen', value: Math.round(hours * 10) / 10 }))
      .sort((a, b) => b.value - a.value)
      .slice(0, 5);
  };

  const topProjects = useMemo(
    () => rankBy((id) => projectsById.get(id), (l) => l.projectId),
    [logs, projectsById],
  );
  const topEmployees = useMemo(
    () => rankBy((id) => employeesById.get(id), (l) => l.employeeId),
    [logs, employeesById],
  );
  const topActivities = useMemo(
    () => rankBy((id) => activitiesById.get(id), (l) => l.activityL1Id),
    [logs, activitiesById],
  );

  const upcomingHolidays = useMemo(
    () =>
      (holidays.data?.items ?? [])
        .filter((h) => h.date >= todayKey)
        .sort((a, b) => a.date.localeCompare(b.date))
        .slice(0, 3),
    [holidays.data, todayKey],
  );

  const isLoading = employees.isLoading || projects.isLoading || customers.isLoading || workLogs.isLoading;

  return (
    <div className="flex flex-1 overflow-y-auto bg-slate-50 p-6">
      <div className="mx-auto w-full max-w-6xl">
        <div className="mb-5">
          <h1 className="text-xl font-bold text-slate-800">Ana Sayfa</h1>
          <p className="text-sm text-slate-500">Organizasyon genelinde efor takibine genel bakış — {monthRange.label}</p>
        </div>

        {isLoading ? (
          <div className="rounded-xl border border-slate-200 bg-white p-8 text-center text-slate-400">Yükleniyor…</div>
        ) : (
          <div className="flex flex-col gap-4">
            <div className="flex flex-col gap-3 sm:flex-row">
              <KpiCard icon="👥" iconBg="bg-indigo-50 text-indigo-600" label="Toplam Çalışan" value={String(totalEmployeeCount)} caption="Sistemde kayıtlı" />
              <KpiCard icon="📁" iconBg="bg-blue-50 text-blue-600" label="Toplam Proje" value={String(totalProjectCount)} caption="Aktif ve tamamlanan" />
              <KpiCard icon="🏢" iconBg="bg-emerald-50 text-emerald-600" label="Toplam Müşteri" value={String(totalCustomerCount)} caption="Sistemde kayıtlı" />
              <KpiCard icon="⏱" iconBg="bg-amber-50 text-amber-600" label="Bu Ay Toplam Efor" value={`${totalHours.toFixed(1)}h`} caption={`${activeEmployeeCount} kişi log girdi`} />
            </div>

            <div className="flex flex-col gap-4 lg:flex-row">
              <div className="lg:w-2/3">
                <Panel title="Aylık Efor Trendi" caption={`${monthRange.label} — güne göre toplam saat`}>
                  <DashboardAreaChart data={dailyTrend} color="#4f46e5" unit="h" />
                </Panel>
              </div>
              <div className="lg:w-1/3">
                <Panel title="Yaklaşan Tatiller" caption="Resmi tatil takviminden">
                  {upcomingHolidays.length === 0 ? (
                    <div className="flex h-[220px] items-center justify-center text-sm text-slate-400">
                      Yaklaşan tatil yok
                    </div>
                  ) : (
                    <ul className="flex h-[220px] flex-col justify-center gap-3">
                      {upcomingHolidays.map((h) => (
                        <li key={h.id} className="flex items-center gap-3 rounded-lg bg-red-50 px-3 py-2.5">
                          <span className="text-lg">🎌</span>
                          <div>
                            <div className="text-sm font-semibold capitalize text-slate-700">{h.name}</div>
                            <div className="text-xs capitalize text-slate-400">{formatDateTr(h.date)}</div>
                          </div>
                        </li>
                      ))}
                    </ul>
                  )}
                </Panel>
              </div>
            </div>

            <div className="flex flex-col gap-4 md:flex-row">
              <Panel title="En Çok Efor Harcanan Projeler" caption="Bu ay, saat bazında">
                <DashboardRankingBar data={topProjects} color="#2563eb" />
              </Panel>
              <Panel title="En Aktif Çalışanlar" caption="Bu ay, saat bazında">
                <DashboardRankingBar data={topEmployees} color="#059669" />
              </Panel>
              <Panel title="Aktivite Dağılımı" caption="Activity L1, bu ay saat bazında">
                <DashboardRankingBar data={topActivities} color="#f59e0b" />
              </Panel>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
