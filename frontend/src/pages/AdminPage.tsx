import { useState } from 'react';
import { useEmployees } from '../hooks/useEmployees';
import { useNotifications } from '../hooks/useNotifications';
import { useValueStreams } from '../hooks/useValueStreams';
import { useAllActivities } from '../hooks/useActivities';
import { useHolidays } from '../hooks/useHolidays';
import { UserDirectorySection } from '../components/admin/directory/UserDirectorySection';
import { AttributeMappingsSection } from '../components/admin/directory/AttributeMappingsSection';
import { OrgChartSection } from '../components/admin/directory/OrgChartSection';

type SectionKind =
  | 'employees'
  | 'notifications'
  | 'valueStreams'
  | 'activities'
  | 'holidays'
  | 'workCalendars'
  | 'userDirectory'
  | 'attributeMappings'
  | 'orgChart'
  | 'placeholder';

interface AdminSection {
  key: string;
  label: string;
  kind: SectionKind;
}

interface AdminGroup {
  header: string;
  sections: AdminSection[];
}

interface AdminTab {
  key: string;
  label: string;
  groups: AdminGroup[];
}

const ADMIN_TABS: AdminTab[] = [
  {
    key: 'general',
    label: 'Genel',
    groups: [
      {
        header: 'GENEL AYARLAR',
        sections: [
          { key: 'company', label: 'Şirket Bilgileri', kind: 'placeholder' },
          { key: 'notifications', label: 'Bildirimler', kind: 'notifications' },
        ],
      },
    ],
  },
  {
    key: 'users',
    label: 'Kullanıcı Yönetimi',
    groups: [
      {
        header: 'KULLANICI YÖNETİMİ',
        sections: [
          { key: 'employees', label: 'Çalışanlar', kind: 'employees' },
          { key: 'userDirectory', label: 'Kullanıcı Klasörü', kind: 'userDirectory' },
          { key: 'attributeMappings', label: 'Alan Eşlemeleri', kind: 'attributeMappings' },
          { key: 'orgChart', label: 'Organizasyon Şeması', kind: 'orgChart' },
          { key: 'roles', label: 'Roller ve İzinler', kind: 'placeholder' },
        ],
      },
    ],
  },
  {
    key: 'system',
    label: 'Sistem',
    groups: [
      {
        header: 'KATALOG',
        sections: [
          { key: 'valueStreams', label: "Value Stream'ler", kind: 'valueStreams' },
          { key: 'activities', label: 'Aktivite Kataloğu', kind: 'activities' },
        ],
      },
      {
        header: 'TAKVİM',
        sections: [
          { key: 'holidays', label: 'Resmi Tatiller', kind: 'holidays' },
          { key: 'workCalendars', label: 'Mesai Takvimleri', kind: 'workCalendars' },
        ],
      },
    ],
  },
];

function Placeholder({ label }: { label: string }) {
  return (
    <div className="flex flex-col items-center justify-center rounded-xl border border-dashed border-slate-200 py-16 text-center">
      <div className="mb-2 text-3xl">🚧</div>
      <p className="text-sm font-medium text-slate-500">{label} bölümü yakında eklenecek.</p>
    </div>
  );
}

function EmployeesSection() {
  const employees = useEmployees();
  return (
    <table className="w-full text-left text-sm">
      <thead>
        <tr className="border-b border-slate-100 text-xs uppercase tracking-wide text-slate-400">
          <th className="py-2 pr-4 font-medium">Ad Soyad</th>
          <th className="py-2 pr-4 font-medium">E-posta</th>
        </tr>
      </thead>
      <tbody>
        {employees.data?.items.map((e) => (
          <tr key={e.id} className="border-b border-slate-50 last:border-0">
            <td className="py-2 pr-4 text-slate-700">{e.name}</td>
            <td className="py-2 pr-4 text-slate-500">{e.email ?? '—'}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}

function NotificationsSection() {
  const notifications = useNotifications();
  return (
    <table className="w-full text-left text-sm">
      <thead>
        <tr className="border-b border-slate-100 text-xs uppercase tracking-wide text-slate-400">
          <th className="py-2 pr-4 font-medium">Mesaj</th>
          <th className="py-2 pr-4 font-medium">Tarih</th>
          <th className="py-2 font-medium">Durum</th>
        </tr>
      </thead>
      <tbody>
        {notifications.data?.items.map((n) => (
          <tr key={n.id} className="border-b border-slate-50 last:border-0">
            <td className="py-2 pr-4 text-slate-700">{n.message}</td>
            <td className="py-2 pr-4 text-slate-500">{new Date(n.createdAtUtc).toLocaleDateString('tr-TR')}</td>
            <td className="py-2">
              <span
                className={
                  'rounded-full px-2 py-0.5 text-xs font-medium ' +
                  (n.isRead ? 'bg-slate-100 text-slate-500' : 'bg-indigo-50 text-indigo-700')
                }
              >
                {n.isRead ? 'Okundu' : 'Okunmadı'}
              </span>
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}

function ValueStreamsSection() {
  const valueStreams = useValueStreams();
  return (
    <table className="w-full text-left text-sm">
      <thead>
        <tr className="border-b border-slate-100 text-xs uppercase tracking-wide text-slate-400">
          <th className="py-2 pr-4 font-medium">Ad</th>
          <th className="py-2 font-medium">Açıklama</th>
        </tr>
      </thead>
      <tbody>
        {valueStreams.data?.items.map((v) => (
          <tr key={v.id} className="border-b border-slate-50 last:border-0">
            <td className="py-2 pr-4 text-slate-700">{v.name}</td>
            <td className="py-2 text-slate-500">{v.description ?? '—'}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}

function ActivitiesSection() {
  const activities = useAllActivities();
  const items = activities.data?.items ?? [];
  const topLevel = items.filter((a) => !a.parentActivityId);
  const subByParent = new Map<string, typeof items>();
  for (const a of items) {
    if (!a.parentActivityId) continue;
    const list = subByParent.get(a.parentActivityId) ?? [];
    list.push(a);
    subByParent.set(a.parentActivityId, list);
  }

  return (
    <div className="space-y-4">
      {topLevel.map((l1) => (
        <div key={l1.id}>
          <div className="text-sm font-semibold text-slate-700">{l1.name}</div>
          <ul className="mt-1 space-y-0.5 pl-4 text-sm text-slate-500">
            {(subByParent.get(l1.id) ?? []).map((l2) => (
              <li key={l2.id}>· {l2.name}</li>
            ))}
          </ul>
        </div>
      ))}
    </div>
  );
}

function HolidaysSection() {
  const holidays = useHolidays();
  return (
    <table className="w-full text-left text-sm">
      <thead>
        <tr className="border-b border-slate-100 text-xs uppercase tracking-wide text-slate-400">
          <th className="py-2 pr-4 font-medium">Tarih</th>
          <th className="py-2 font-medium">Ad</th>
        </tr>
      </thead>
      <tbody>
        {holidays.data?.items
          .slice()
          .sort((a, b) => a.date.localeCompare(b.date))
          .map((h) => (
            <tr key={h.id} className="border-b border-slate-50 last:border-0">
              <td className="py-2 pr-4 text-slate-700">
                {new Date(`${h.date}T00:00:00`).toLocaleDateString('tr-TR')}
              </td>
              <td className="py-2 text-slate-500">{h.name}</td>
            </tr>
          ))}
      </tbody>
    </table>
  );
}

function WorkCalendarsSection() {
  // Sistemde şu an için sabit iki mesai takvimi var (seed veride HasData ile tanımlı); ayrı bir
  // "tüm takvimleri listele" API uç noktası olmadığından bilgi amaçlı statik olarak gösteriliyor.
  const calendars = [
    { name: 'Standart Ofis Mesaisi', schedule: 'Pzt–Cum 09:00–18:00, Cmt–Paz kapalı' },
    { name: 'Esnek Vardiya', schedule: 'Pzt–Cum 09:00–17:00, Cmt 10:00–14:00, Paz kapalı' },
  ];
  return (
    <table className="w-full text-left text-sm">
      <thead>
        <tr className="border-b border-slate-100 text-xs uppercase tracking-wide text-slate-400">
          <th className="py-2 pr-4 font-medium">Ad</th>
          <th className="py-2 font-medium">Program</th>
        </tr>
      </thead>
      <tbody>
        {calendars.map((c) => (
          <tr key={c.name} className="border-b border-slate-50 last:border-0">
            <td className="py-2 pr-4 text-slate-700">{c.name}</td>
            <td className="py-2 text-slate-500">{c.schedule}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}

function SectionContent({ section }: { section: AdminSection }) {
  switch (section.kind) {
    case 'employees':
      return <EmployeesSection />;
    case 'notifications':
      return <NotificationsSection />;
    case 'valueStreams':
      return <ValueStreamsSection />;
    case 'activities':
      return <ActivitiesSection />;
    case 'holidays':
      return <HolidaysSection />;
    case 'workCalendars':
      return <WorkCalendarsSection />;
    case 'userDirectory':
      return <UserDirectorySection />;
    case 'attributeMappings':
      return <AttributeMappingsSection />;
    case 'orgChart':
      return <OrgChartSection />;
    case 'placeholder':
      return <Placeholder label={section.label} />;
  }
}

/**
 * Jira'nın Administration ekranındaki yapıya (üst kategori sekmeleri + sol tarafta başlıklarla
 * gruplanmış alt bölümler + sağda seçili bölümün içerik/tablo alanı) benzer şekilde tasarlanmış
 * admin sayfası — header'daki ⚙️ ikonu buraya yönlendirir. Backend'de zaten karşılığı olan
 * bölümler (Çalışanlar, Bildirimler, Value Stream'ler, Aktiviteler, Resmi Tatiller) gerçek veriyle
 * dolduruldu; henüz karşılığı olmayanlar (Şirket Bilgileri, Roller) "yakında" ile işaretlendi.
 */
export function AdminPage() {
  const [activeTabKey, setActiveTabKey] = useState(ADMIN_TABS[0].key);
  const activeTab = ADMIN_TABS.find((t) => t.key === activeTabKey) ?? ADMIN_TABS[0];

  const [activeSectionKey, setActiveSectionKey] = useState(activeTab.groups[0].sections[0].key);
  const activeSection =
    activeTab.groups.flatMap((g) => g.sections).find((s) => s.key === activeSectionKey) ??
    activeTab.groups[0].sections[0];

  const selectTab = (tabKey: string) => {
    const tab = ADMIN_TABS.find((t) => t.key === tabKey);
    if (!tab) return;
    setActiveTabKey(tabKey);
    setActiveSectionKey(tab.groups[0].sections[0].key);
  };

  return (
    <div className="flex h-full flex-col">
      <div className="border-b border-slate-200 bg-white px-6">
        <h1 className="pt-4 text-lg font-semibold text-slate-800">Mesainame Yönetim Paneli</h1>
        <nav className="mt-3 flex gap-1">
          {ADMIN_TABS.map((tab) => (
            <button
              key={tab.key}
              type="button"
              onClick={() => selectTab(tab.key)}
              className={
                'border-b-2 px-3 py-2 text-sm font-medium ' +
                (tab.key === activeTabKey
                  ? 'border-indigo-600 text-indigo-700'
                  : 'border-transparent text-slate-500 hover:text-slate-700')
              }
            >
              {tab.label}
            </button>
          ))}
        </nav>
      </div>

      <div className="flex flex-1 overflow-hidden">
        <aside className="w-64 shrink-0 overflow-y-auto border-r border-slate-200 bg-slate-50 p-4">
          {activeTab.groups.map((group) => (
            <div key={group.header} className="mb-5">
              <div className="mb-1.5 px-2 text-xs font-semibold tracking-wide text-slate-400">{group.header}</div>
              <div className="space-y-0.5">
                {group.sections.map((section) => (
                  <button
                    key={section.key}
                    type="button"
                    onClick={() => setActiveSectionKey(section.key)}
                    className={
                      'block w-full rounded-md px-2 py-1.5 text-left text-sm ' +
                      (section.key === activeSectionKey
                        ? 'bg-indigo-100 font-medium text-indigo-700'
                        : 'text-slate-600 hover:bg-slate-100')
                    }
                  >
                    {section.label}
                  </button>
                ))}
              </div>
            </div>
          ))}
        </aside>

        <main className="flex-1 overflow-y-auto p-6">
          <h2 className="mb-4 text-base font-semibold text-slate-800">{activeSection.label}</h2>
          <div className="rounded-xl border border-slate-200 bg-white p-4">
            <SectionContent section={activeSection} />
          </div>
        </main>
      </div>
    </div>
  );
}
