export type ProjectDetailTabKey =
  | 'overview'
  | 'schedule'
  | 'tasks'
  | 'resources'
  | 'budget'
  | 'risks'
  | 'issues'
  | 'changes'
  | 'documents'
  | 'stakeholders'
  | 'status'
  | 'timesheet'
  | 'approvals';

export const PROJECT_DETAIL_TABS: { key: ProjectDetailTabKey; label: string }[] = [
  { key: 'overview', label: 'Overview' },
  { key: 'schedule', label: 'Schedule' },
  { key: 'tasks', label: 'Tasks' },
  { key: 'timesheet', label: 'Gerçekleşen Efor' },
  { key: 'risks', label: 'Risks' },
  { key: 'issues', label: 'Issues' },
  { key: 'resources', label: 'Resources' },
  { key: 'budget', label: 'Budget' },
  { key: 'changes', label: 'Changes' },
  { key: 'approvals', label: 'Approvals' },
  { key: 'documents', label: 'Documents' },
  { key: 'stakeholders', label: 'Stakeholders' },
  { key: 'status', label: 'Status/Reports' },
];

interface ProjectDetailTabsProps {
  activeKey: ProjectDetailTabKey;
  onChange: (key: ProjectDetailTabKey) => void;
}

/** Proje detay sayfasının düz (sidebar'sız), Clarity PPM'deki tek satırlık şerit tarzı sekme
 * navigasyonu — AdminPage'in üst tab-strip deseninden uyarlandı (aynı border-b-2 aktif çizgisi). */
export function ProjectDetailTabs({ activeKey, onChange }: ProjectDetailTabsProps) {
  return (
    <nav className="flex flex-wrap gap-1 border-b border-slate-200">
      {PROJECT_DETAIL_TABS.map((tab) => (
        <button
          key={tab.key}
          type="button"
          onClick={() => onChange(tab.key)}
          className={
            'border-b-2 px-3 py-2 text-sm font-medium ' +
            (tab.key === activeKey
              ? 'border-indigo-600 text-indigo-700'
              : 'border-transparent text-slate-500 hover:text-slate-700')
          }
        >
          {tab.label}
        </button>
      ))}
    </nav>
  );
}
