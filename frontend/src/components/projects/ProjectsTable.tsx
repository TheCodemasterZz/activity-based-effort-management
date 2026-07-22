import { useState } from 'react';
import { computeProjectEvmSummary } from '../../lib/projectSpi';
import {
  RAG_CELL_CLASS,
  healthRag,
  issueRag,
  performanceRag,
  riskRag,
  scheduleRag,
  type RagResult,
  type RagTier,
} from '../../lib/projectRag';
import type { ProjectDto, ProjectIssueDto, ProjectRiskDto, ProjectTaskDto } from '../../api/types';

interface ProjectsTableProps {
  projects: ProjectDto[];
  tasksByProject: Map<string, ProjectTaskDto[]>;
  risksByProject: Map<string, ProjectRiskDto[]>;
  issuesByProject: Map<string, ProjectIssueDto[]>;
  resolveEmployee: (id: string | null) => string;
  onView: (project: ProjectDto) => void;
  onEdit: (project: ProjectDto) => void;
  onDeactivate: (project: ProjectDto) => void;
}

const STATUS_BADGE: Record<string, { label: string; className: string }> = {
  Active: { label: 'Aktif', className: 'bg-emerald-50 text-emerald-700' },
  Completed: { label: 'Tamamlandı', className: 'bg-slate-100 text-slate-600' },
  Cancelled: { label: 'İptal Edildi', className: 'bg-red-50 text-red-600' },
};

// Sağlık durumuna göre satırın tamamına hafif bir zemin rengi — tablo taranırken riskli
// projelerin göze çarpması için (AccuracyCell'deki tam hücre renklendirme geleneğiyle aynı ruh).
const HEALTH_ROW_CLASS: Record<string, string> = {
  OnTrack: '',
  AtRisk: 'bg-red-50/60',
  NeedsHelp: 'bg-amber-50/60',
};

function formatDateShort(date: string | null): string {
  if (!date) return '—';
  return new Date(`${date}T00:00:00`).toLocaleDateString('tr-TR', { day: '2-digit', month: 'short', year: 'numeric' });
}

function timeElapsedPercent(start: string | null, end: string | null): number | null {
  if (!start || !end) return null;
  const startMs = new Date(`${start}T00:00:00`).getTime();
  const endMs = new Date(`${end}T00:00:00`).getTime();
  const nowMs = Date.now();
  if (endMs <= startMs) return null;
  return Math.min(100, Math.max(0, ((nowMs - startMs) / (endMs - startMs)) * 100));
}

type SortKey =
  | 'name' | 'status' | 'startDate' | 'endDate' | 'elapsed'
  | 'completion' | 'taskDurum'
  | 'ragHealth' | 'ragSchedule' | 'ragPerformance' | 'ragRisk' | 'ragIssue';
type SortDir = 'asc' | 'desc' | null;

interface EnrichedRow {
  project: ProjectDto;
  status: { label: string; className: string };
  rowClass: string;
  percentComplete: number;
  doneCount: number;
  inProgressCount: number;
  notStartedCount: number;
  totalTasks: number;
  elapsedPct: number | null;
  completionColor: string;
  ragHealth: RagResult;
  ragSchedule: RagResult;
  ragPerformance: RagResult;
  ragRisk: RagResult;
  ragIssue: RagResult;
}

// RAG hücrelerini sıralarken kötü→iyi (red→amber→gray→green) doğal bir sıra izlensin diye.
const RAG_TIER_RANK: Record<RagTier, number> = { red: 0, amber: 1, gray: 2, green: 3 };

// RAG hücreleri span/badge değil, hücrenin tamamını (td'nin kendisini) kaplayan tam arkaplan
// rengiyle render edilir — "sadece yazı arkaplanı değil, ilgili hücreyi tamamıyla kapsasın".
function RagCell({ rag, hidden }: { rag: RagResult; hidden?: boolean }) {
  return (
    <td
      title={rag.detail}
      className={`w-28 border-r border-slate-100 px-3 py-1.5 text-center text-[11px] font-semibold ${RAG_CELL_CLASS[rag.tier]} ${hidden ? 'hidden' : ''}`}
    >
      {rag.label}
    </td>
  );
}

// Her sütun için sıralama değeri — metin alanları Türkçe locale ile, sayısal/tarih alanları
// doğrudan karşılaştırılır. Değeri olmayanlar (SPI hesaplanamıyor, tarih yok, vb.) sıralamada
// en sona düşecek şekilde -Infinity/boş string ile temsil edilir.
const SORT_VALUE: Record<SortKey, (r: EnrichedRow) => string | number> = {
  name: (r) => r.project.name.toLocaleLowerCase('tr'),
  status: (r) => r.status.label.toLocaleLowerCase('tr'),
  startDate: (r) => r.project.startDate ?? '',
  endDate: (r) => r.project.endDate ?? '',
  elapsed: (r) => r.elapsedPct ?? -1,
  completion: (r) => r.percentComplete,
  taskDurum: (r) => (r.totalTasks > 0 ? r.doneCount / r.totalTasks : -1),
  ragHealth: (r) => RAG_TIER_RANK[r.ragHealth.tier],
  ragSchedule: (r) => RAG_TIER_RANK[r.ragSchedule.tier],
  ragPerformance: (r) => RAG_TIER_RANK[r.ragPerformance.tier],
  ragRisk: (r) => RAG_TIER_RANK[r.ragRisk.tier],
  ragIssue: (r) => RAG_TIER_RANK[r.ragIssue.tier],
};

function SortableTh({
  label, sortKey, activeKey, dir, onSort, className, align = 'left', title,
}: {
  label: string;
  sortKey: SortKey;
  activeKey: SortKey | null;
  dir: SortDir;
  onSort: (key: SortKey) => void;
  className?: string;
  align?: 'left' | 'right' | 'center';
  title?: string;
}) {
  const isActive = activeKey === sortKey;
  return (
    <th
      className={`sticky top-0 z-10 border-b border-slate-200 px-3 py-2 font-semibold text-slate-500 ${
        align === 'right' ? 'text-right' : align === 'center' ? 'text-center' : 'text-left'
      } ${className ?? ''}`}
    >
      <button
        type="button"
        onClick={() => onSort(sortKey)}
        className={
          'flex items-center gap-1.5 hover:text-slate-700 ' +
          (align === 'right' ? 'ml-auto' : align === 'center' ? 'mx-auto' : '')
        }
        title={title ?? `${label}'e göre sırala`}
      >
        <span>{label}</span>
        <span className={'text-base leading-none ' + (isActive && dir ? 'font-bold text-indigo-600' : 'text-slate-400')}>
          {isActive && dir === 'asc' ? '▲' : isActive && dir === 'desc' ? '▼' : '⇅'}
        </span>
      </button>
    </th>
  );
}

/** Proje portföyünü tek bakışta karşılaştırmak için renklendirilmiş bir veri tablosu — kart
 * grid'in yerini alır. Her satırda: durum rozeti (+ sağlığa göre satır zemin rengi), zaman
 * çizelgesi ilerlemesi, görev tamamlanma (zamana göre önde/geride renk kodlu), görev durum
 * dağılım çubuğu ve RAG sağlık göstergeleri (Genel Sağlık/Zaman/Performans/Risk). Her sütun
 * başlığına tıklayarak artan/azalan/doğal sırada (A-Z / Z-A / eklenme sırası) sıralanabilir. */
export function ProjectsTable({
  projects, tasksByProject, risksByProject, issuesByProject, resolveEmployee, onView, onEdit, onDeactivate,
}: ProjectsTableProps) {
  const [sortKey, setSortKey] = useState<SortKey>('name');
  const [sortDir, setSortDir] = useState<SortDir>('asc');
  const todayKey = new Date().toISOString().slice(0, 10);

  const handleSort = (key: SortKey) => {
    if (sortKey !== key) {
      setSortKey(key);
      setSortDir('asc');
    } else {
      setSortDir((prev) => (prev === 'asc' ? 'desc' : prev === 'desc' ? null : 'asc'));
    }
  };

  const enriched: EnrichedRow[] = projects.map((project) => {
    const tasks = tasksByProject.get(project.id) ?? [];
    const evm = computeProjectEvmSummary(tasks);
    const status = STATUS_BADGE[project.status] ?? { label: project.status, className: 'bg-slate-100 text-slate-600' };
    const rowClass = HEALTH_ROW_CLASS[project.healthStatus] ?? '';

    const doneCount = tasks.filter((t) => t.status === 'Done').length;
    const inProgressCount = tasks.filter((t) => t.status === 'InProgress').length;
    const notStartedCount = tasks.filter((t) => t.status === 'NotStarted').length;
    const totalTasks = tasks.length;

    const elapsedPct = timeElapsedPercent(project.startDate, project.endDate);
    const completionColor =
      elapsedPct === null
        ? 'bg-indigo-500'
        : evm.percentComplete >= elapsedPct
          ? 'bg-emerald-500'
          : evm.percentComplete >= elapsedPct - 15
            ? 'bg-amber-400'
            : 'bg-red-500';

    const risks = risksByProject.get(project.id) ?? [];
    const issues = issuesByProject.get(project.id) ?? [];

    return {
      project, status, rowClass,
      percentComplete: evm.percentComplete,
      doneCount, inProgressCount, notStartedCount, totalTasks,
      elapsedPct, completionColor,
      ragHealth: healthRag(project),
      ragSchedule: scheduleRag(elapsedPct, evm.percentComplete),
      ragPerformance: performanceRag(evm),
      ragRisk: riskRag(risks),
      ragIssue: issueRag(issues, todayKey),
    };
  });

  const sorted =
    sortDir === null
      ? enriched
      : [...enriched].sort((a, b) => {
          const va = SORT_VALUE[sortKey](a);
          const vb = SORT_VALUE[sortKey](b);
          const cmp = typeof va === 'string' && typeof vb === 'string' ? va.localeCompare(vb, 'tr') : (va as number) - (vb as number);
          return sortDir === 'asc' ? cmp : -cmp;
        });

  return (
    <div className="h-full overflow-auto rounded-xl border border-slate-200 bg-white">
      <table className="min-w-full border-collapse text-sm">
        <thead>
          <tr className="border-b border-slate-200 bg-slate-50">
            <th className="sticky left-0 top-0 z-20 min-w-[16rem] border-r border-b border-slate-200 bg-slate-50 px-3 py-2 text-left font-semibold text-slate-500">
              <button
                type="button"
                onClick={() => handleSort('name')}
                className="flex items-center gap-1.5 hover:text-slate-700"
                title="Proje adına göre sırala"
              >
                <span>Proje</span>
                <span
                  className={
                    'text-base leading-none ' +
                    (sortKey === 'name' && sortDir ? 'font-bold text-indigo-600' : 'text-slate-400')
                  }
                >
                  {sortKey === 'name' && sortDir === 'asc' ? '▲' : sortKey === 'name' && sortDir === 'desc' ? '▼' : '⇅'}
                </span>
              </button>
            </th>
            <SortableTh label="Durum" sortKey="status" activeKey={sortKey} dir={sortDir} onSort={handleSort} />
            <SortableTh label="Başlangıç" sortKey="startDate" activeKey={sortKey} dir={sortDir} onSort={handleSort} />
            <SortableTh label="Bitiş" sortKey="endDate" activeKey={sortKey} dir={sortDir} onSort={handleSort} />
            <SortableTh label="İlerleme" sortKey="elapsed" activeKey={sortKey} dir={sortDir} onSort={handleSort} className="w-32" />
            <SortableTh label="Tamamlanma" sortKey="completion" activeKey={sortKey} dir={sortDir} onSort={handleSort} className="w-32" />
            <SortableTh
              label="Görev Durumu"
              sortKey="taskDurum"
              activeKey={sortKey}
              dir={sortDir}
              onSort={handleSort}
              className="min-w-[8rem]"
              title="Bitme oranına göre sırala"
            />
            <SortableTh label="Genel" sortKey="ragHealth" activeKey={sortKey} dir={sortDir} onSort={handleSort} align="center" title="Genel Sağlık RAG'a göre sırala" className="w-28" />
            <SortableTh label="Zaman" sortKey="ragSchedule" activeKey={sortKey} dir={sortDir} onSort={handleSort} align="center" title="Zaman (Schedule) RAG'a göre sırala" className="w-28" />
            <SortableTh label="Performans" sortKey="ragPerformance" activeKey={sortKey} dir={sortDir} onSort={handleSort} align="center" title="Performans (SPI) RAG'a göre sırala" className="w-28" />
            <SortableTh label="Risk" sortKey="ragRisk" activeKey={sortKey} dir={sortDir} onSort={handleSort} align="center" title="Risk RAG'a göre sırala" className="w-28" />
            <SortableTh label="Sorun" sortKey="ragIssue" activeKey={sortKey} dir={sortDir} onSort={handleSort} align="center" title="Sorun (Issue) RAG'a göre sırala" className="hidden w-28" />
            <th className="sticky top-0 z-10 border-b border-slate-200 px-3 py-2 text-center font-semibold text-slate-500">İşlemler</th>
          </tr>
        </thead>
        <tbody>
          {sorted.length === 0 && (
            <tr>
              <td colSpan={12} className="px-4 py-8 text-center text-slate-400">Proje bulunamadı.</td>
            </tr>
          )}
          {sorted.map((r) => {
            const { project } = r;
            const pct = (n: number) => (r.totalTasks > 0 ? (n / r.totalTasks) * 100 : 0);

            return (
              <tr key={project.id} className={`border-b border-slate-100 last:border-0 hover:bg-slate-50 ${r.rowClass}`}>
                <td className="sticky left-0 z-10 border-r border-slate-200 bg-white px-3 py-1.5">
                  <button type="button" onClick={() => onView(project)} className="text-left font-medium text-slate-700 hover:text-indigo-600 hover:underline">
                    {project.name}
                  </button>
                  <div className="mt-0.5 max-w-[14rem] truncate text-xs text-slate-400">
                    {resolveEmployee(project.projectManagerEmployeeId)}
                  </div>
                </td>
                <td className="px-3 py-1.5">
                  <span className={`rounded-full px-2 py-0.5 text-xs font-medium ${r.status.className}`}>{r.status.label}</span>
                </td>
                <td className="whitespace-nowrap px-3 py-1.5 text-xs text-slate-500">{formatDateShort(project.startDate)}</td>
                <td className="whitespace-nowrap px-3 py-1.5 text-xs text-slate-500">{formatDateShort(project.endDate)}</td>
                <td className="relative w-32 overflow-hidden border-r border-slate-100 bg-slate-100 p-0 text-center">
                  {r.elapsedPct === null ? (
                    <span className="text-xs text-slate-300">—</span>
                  ) : (
                    <>
                      <div className="absolute inset-y-0 left-0 bg-indigo-500" style={{ width: `${r.elapsedPct}%` }} />
                      <span className="absolute inset-0 z-10 flex items-center justify-center">
                        <span className="rounded bg-black/15 px-1.5 py-0.5 text-[11px] font-semibold text-white">
                          %{r.elapsedPct.toFixed(0)}
                        </span>
                      </span>
                    </>
                  )}
                </td>
                <td className="relative w-32 overflow-hidden bg-slate-100 p-0 text-center">
                  <div className={`absolute inset-y-0 left-0 ${r.completionColor}`} style={{ width: `${r.percentComplete}%` }} />
                  <span className="absolute inset-0 z-10 flex items-center justify-center">
                    <span className="rounded bg-black/15 px-1.5 py-0.5 text-[11px] font-semibold text-white">
                      %{r.percentComplete}
                    </span>
                  </span>
                </td>
                <td className="px-3 py-1.5">
                  {r.totalTasks > 0 ? (
                    <div
                      className="flex h-2 w-full overflow-hidden rounded-full bg-slate-100"
                      title={`${r.doneCount} bitti · ${r.inProgressCount} devam ediyor · ${r.notStartedCount} başlamadı`}
                    >
                      {r.doneCount > 0 && <div className="h-full bg-emerald-500" style={{ width: `${pct(r.doneCount)}%` }} />}
                      {r.inProgressCount > 0 && <div className="h-full bg-amber-400" style={{ width: `${pct(r.inProgressCount)}%` }} />}
                      {r.notStartedCount > 0 && <div className="h-full bg-slate-300" style={{ width: `${pct(r.notStartedCount)}%` }} />}
                    </div>
                  ) : (
                    <span className="text-xs text-slate-300">—</span>
                  )}
                  <div className="mt-0.5 text-[10px] text-slate-400">{r.doneCount}/{r.totalTasks} görev</div>
                </td>
                <RagCell rag={r.ragHealth} />
                <RagCell rag={r.ragSchedule} />
                <RagCell rag={r.ragPerformance} />
                <RagCell rag={r.ragRisk} />
                <RagCell rag={r.ragIssue} hidden />
                <td className="px-3 py-1.5">
                  <div className="flex items-center justify-center gap-2.5">
                    <button type="button" onClick={() => onView(project)} title="Görüntüle" className="text-slate-400 hover:text-indigo-600">📋</button>
                    <button type="button" onClick={() => onEdit(project)} title="Düzenle" className="text-slate-400 hover:text-indigo-600">✏️</button>
                    <button type="button" onClick={() => onDeactivate(project)} title="Pasife Al" className="text-slate-400 hover:text-red-600">🗑️</button>
                  </div>
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}
