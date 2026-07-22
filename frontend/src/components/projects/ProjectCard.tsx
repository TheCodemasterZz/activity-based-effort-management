import { computeProjectEvmSummary, spiHealthTier } from '../../lib/projectSpi';
import type { ProjectDto, ProjectTaskDto } from '../../api/types';
import { MilestoneStrip } from './MilestoneStrip';

interface ProjectCardProps {
  project: ProjectDto;
  tasks: ProjectTaskDto[];
  actualHours: number;
  employeeCount: number;
  colorIndex: number;
  onView: () => void;
  onEdit: () => void;
  onDeactivate: () => void;
}

const HEADER_COLORS = [
  'bg-indigo-100', 'bg-rose-100', 'bg-teal-100', 'bg-amber-100', 'bg-sky-100', 'bg-violet-100',
];

const HEALTH_BADGE: Record<string, { label: string; className: string }> = {
  OnTrack: { label: 'ON TRACK', className: 'bg-emerald-500 text-white' },
  AtRisk: { label: 'AT RISK', className: 'bg-red-500 text-white' },
  NeedsHelp: { label: 'NEEDS HELP', className: 'bg-amber-500 text-white' },
};

const SPI_TIER_CLASS: Record<string, string> = {
  good: 'text-emerald-600',
  warning: 'text-amber-600',
  critical: 'text-red-600',
  unknown: 'text-slate-300',
};

function formatDateShort(date: string | null): string {
  if (!date) return '—';
  return new Date(`${date}T00:00:00`).toLocaleDateString('tr-TR', { day: '2-digit', month: 'short', year: 'numeric' });
}

/** Clarity PPM'in proje kart-grid ana sayfasından esinlenilmiş kart — renkli üst şerit + sağlık
 * rozeti, kilometre taşı şeridi, tarih aralığı, ve 3 özet gösterge (SPI, gerçekleşen saat, aktif
 * kişi). Alttaki ikon şeridi hızlı erişim içindir. */
export function ProjectCard({
  project, tasks, actualHours, employeeCount, colorIndex, onView, onEdit, onDeactivate,
}: ProjectCardProps) {
  const health = HEALTH_BADGE[project.healthStatus] ?? { label: project.healthStatus, className: 'bg-slate-400 text-white' };
  const evm = computeProjectEvmSummary(tasks);
  const spiTier = spiHealthTier(evm.spi);
  const milestones = tasks.filter((t) => t.isMilestone);

  return (
    <div className="flex flex-col overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm">
      <div className={`px-4 py-3 ${HEADER_COLORS[colorIndex % HEADER_COLORS.length]}`}>
        <div className="mb-1 flex items-start justify-between gap-2">
          <button type="button" onClick={onView} className="text-left text-sm font-semibold text-slate-800 hover:underline">
            {project.name}
          </button>
          <span className={`shrink-0 rounded px-1.5 py-0.5 text-[10px] font-bold tracking-wide ${health.className}`}>
            {health.label}
          </span>
        </div>

        {project.startDate && project.endDate ? (
          <>
            <MilestoneStrip startDate={project.startDate} endDate={project.endDate} milestones={milestones} />
            <div className="flex justify-between text-[10px] text-slate-500">
              <span>{formatDateShort(project.startDate)}</span>
              <span>{formatDateShort(project.endDate)}</span>
            </div>
          </>
        ) : (
          <p className="text-[10px] text-slate-400">Tarih aralığı belirtilmemiş</p>
        )}
      </div>

      <div className="grid grid-cols-3 gap-2 px-4 py-3 text-center">
        <div>
          <div className={`text-base font-bold ${SPI_TIER_CLASS[spiTier]}`}>{evm.spi ?? '—'}</div>
          <div className="text-[9px] font-medium uppercase tracking-wide text-slate-400">SPI</div>
        </div>
        <div>
          <div className="text-base font-bold text-slate-700">{actualHours.toFixed(0)}h</div>
          <div className="text-[9px] font-medium uppercase tracking-wide text-slate-400">Gerçekleşen</div>
        </div>
        <div>
          <div className="text-base font-bold text-slate-700">{employeeCount}</div>
          <div className="text-[9px] font-medium uppercase tracking-wide text-slate-400">Aktif Kişi</div>
        </div>
      </div>

      <div className="mt-auto flex justify-around border-t border-slate-100 bg-slate-50 py-2">
        <button type="button" onClick={onView} title="Görüntüle" className="text-slate-400 hover:text-indigo-600">
          📋
        </button>
        <button type="button" onClick={onEdit} title="Düzenle" className="text-slate-400 hover:text-indigo-600">
          ✏️
        </button>
        <button type="button" onClick={onDeactivate} title="Pasife Al" className="text-slate-400 hover:text-red-600">
          🗑️
        </button>
      </div>
    </div>
  );
}
