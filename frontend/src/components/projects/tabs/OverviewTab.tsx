import { StatCardShell } from '../../dashboard/SummaryCards';
import { TrendAreaWidget } from '../../dashboard/charts/TrendAreaWidget';
import { computeTrendPercent, type ChartPoint } from '../../../lib/trend';
import { MilestoneStrip } from '../MilestoneStrip';
import {
  RAG_CELL_CLASS,
  healthRag,
  issueRag,
  performanceRag,
  riskRag,
  scheduleRag,
  timeElapsedPercent,
  type RagResult,
} from '../../../lib/projectRag';
import type { ProjectDetailDto, ProjectIssueDto, ProjectRiskDto, ProjectTaskDto } from '../../../api/types';
import type { ProjectEvmSummary } from '../../../lib/projectSpi';

const PRIORITY_LABEL: Record<string, string> = {
  Low: 'Düşük',
  Medium: 'Orta',
  High: 'Yüksek',
  Critical: 'Kritik',
};

interface OverviewTabProps {
  project: ProjectDetailDto;
  tasks: ProjectTaskDto[];
  evm: ProjectEvmSummary;
  milestones: ProjectTaskDto[];
  actualHours90d: number;
  activeEmployeeCount: number;
  hoursTrend: ChartPoint[];
  resolveEmployee: (id: string | null) => string;
  risks: ProjectRiskDto[];
  issues: ProjectIssueDto[];
}

function formatDateTr(date: string | null): string {
  if (!date) return '—';
  return new Date(`${date}T00:00:00`).toLocaleDateString('tr-TR');
}

function RagIndicator({ label, rag }: { label: string; rag: RagResult }) {
  return (
    <div className="flex flex-col gap-1" title={rag.detail}>
      <div className="text-xs font-medium uppercase tracking-wide text-slate-400">{label}</div>
      <span className={`inline-block w-fit rounded px-2 py-1 text-xs font-semibold ${RAG_CELL_CLASS[rag.tier]}`}>
        {rag.label}
      </span>
    </div>
  );
}

export function OverviewTab({
  project, tasks, evm, milestones, actualHours90d, activeEmployeeCount, hoursTrend, resolveEmployee, risks, issues,
}: OverviewTabProps) {
  const todayKey = new Date().toISOString().slice(0, 10);
  const elapsedPct = timeElapsedPercent(project.startDate, project.endDate);

  return (
    <div className="space-y-4">
      <div className="rounded-xl border border-slate-200 bg-white p-4">
        <div className="mb-3 text-sm font-semibold text-slate-700">Sağlık Özeti</div>
        <div className="grid grid-cols-2 gap-4 sm:grid-cols-4">
          <RagIndicator label="Genel Sağlık" rag={healthRag(project)} />
          <RagIndicator label="Zaman" rag={scheduleRag(elapsedPct, evm.percentComplete)} />
          <RagIndicator label="Performans" rag={performanceRag(evm)} />
          <RagIndicator label="Risk" rag={riskRag(risks)} />
          <div className="hidden">
            <RagIndicator label="Sorun" rag={issueRag(issues, todayKey)} />
          </div>
        </div>
      </div>

      <div className="grid grid-cols-1 gap-3 rounded-xl border border-slate-200 bg-white p-4 sm:grid-cols-2 lg:grid-cols-4">
        <div>
          <div className="text-xs font-medium uppercase tracking-wide text-slate-400">Sponsor</div>
          <div className="text-sm font-medium text-slate-700">{project.sponsor || '—'}</div>
        </div>
        <div>
          <div className="text-xs font-medium uppercase tracking-wide text-slate-400">Proje Yöneticisi</div>
          <div className="text-sm font-medium text-slate-700">{resolveEmployee(project.projectManagerEmployeeId)}</div>
        </div>
        <div>
          <div className="text-xs font-medium uppercase tracking-wide text-slate-400">Öncelik</div>
          <div className="text-sm font-medium text-slate-700">{PRIORITY_LABEL[project.priority] ?? project.priority}</div>
        </div>
        <div>
          <div className="text-xs font-medium uppercase tracking-wide text-slate-400">Stratejik Hedef</div>
          <div className="text-sm font-medium text-slate-700">{project.strategicGoal || '—'}</div>
        </div>
      </div>

      {project.startDate && project.endDate && (
        <div className="rounded-xl border border-slate-200 bg-white p-4">
          <div className="mb-1 text-xs font-medium uppercase tracking-wide text-slate-400">Zaman Çizelgesi</div>
          <MilestoneStrip startDate={project.startDate} endDate={project.endDate} milestones={milestones} />
          <div className="flex justify-between text-xs text-slate-400">
            <span>{formatDateTr(project.startDate)}</span>
            <span>{formatDateTr(project.endDate)}</span>
          </div>
        </div>
      )}

      <div className="flex flex-col gap-3 sm:flex-row sm:flex-wrap">
        <StatCardShell
          icon="🎯"
          iconBg="bg-indigo-50 text-indigo-600"
          label="Tamamlanma"
          value={`%${evm.percentComplete}`}
          caption={`${evm.doneTaskCount}/${evm.totalTaskCount} görev bitti`}
        />
        <StatCardShell
          icon="◆"
          iconBg="bg-violet-50 text-violet-600"
          label="Kilometre Taşı"
          value={String(milestones.length)}
          caption={`${milestones.filter((m) => m.status === 'Done').length} tamamlandı`}
        />
        <StatCardShell
          icon="⏱"
          iconBg="bg-amber-50 text-amber-600"
          label="Gerçekleşen Saat"
          value={`${actualHours90d.toFixed(0)}h`}
          caption="Son 90 gün"
          trendPercent={computeTrendPercent(hoursTrend)}
          chart={<TrendAreaWidget data={hoursTrend} color="#f59e0b" unit="h" height={34} />}
        />
        <StatCardShell
          icon="👥"
          iconBg="bg-emerald-50 text-emerald-600"
          label="Aktif Kişi"
          value={String(activeEmployeeCount)}
          caption="Son 90 gün efor girenler"
        />
      </div>

      <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
        <div className="rounded-xl border border-slate-200 bg-white p-4">
          <div className="mb-2 text-sm font-semibold text-slate-700">Müşteriler ({project.customers.length})</div>
          <div className="flex flex-wrap gap-1.5">
            {project.customers.length === 0 ? (
              <span className="text-sm text-slate-400">Atanmış müşteri yok</span>
            ) : (
              project.customers.map((c) => (
                <span key={c.id} className="rounded-full bg-slate-100 px-2.5 py-1 text-xs text-slate-600">
                  {c.name}
                </span>
              ))
            )}
          </div>
        </div>

        <div className="rounded-xl border border-slate-200 bg-white p-4">
          <div className="mb-2 text-sm font-semibold text-slate-700">Çalışanlar ({project.employees.length})</div>
          <div className="flex flex-wrap gap-1.5">
            {project.employees.length === 0 ? (
              <span className="text-sm text-slate-400">Atanmış çalışan yok</span>
            ) : (
              project.employees.map((e) => (
                <span key={e.id} className="rounded-full bg-slate-100 px-2.5 py-1 text-xs text-slate-600">
                  {e.name}
                </span>
              ))
            )}
          </div>
        </div>
      </div>

      <p className="text-xs text-slate-400">Toplam görev: {tasks.length}</p>
    </div>
  );
}
