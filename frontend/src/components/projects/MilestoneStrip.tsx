import type { ProjectTaskDto } from '../../api/types';

interface MilestoneStripProps {
  startDate: string;
  endDate: string;
  milestones: ProjectTaskDto[];
}

function dateToRatio(dateKey: string, startKey: string, endKey: string): number {
  const start = new Date(`${startKey}T00:00:00`).getTime();
  const end = new Date(`${endKey}T00:00:00`).getTime();
  const point = new Date(`${dateKey}T00:00:00`).getTime();
  if (end <= start) return 0;
  return Math.min(1, Math.max(0, (point - start) / (end - start)));
}

/** Clarity PPM kartındaki elmas-şeritli zaman çizelgesinin karşılığı — proje başlangıç/bitiş
 * aralığında, IsMilestone=true olan görevlerin baseline bitiş tarihine göre orantılı konumlanan
 * elmas işaretleri. Geçmişte kalan (bitmiş sayılması gereken) kilometre taşları dolu, gelecektekiler
 * boş elmas olarak çizilir. */
export function MilestoneStrip({ startDate, endDate, milestones }: MilestoneStripProps) {
  const todayKey = new Date().toISOString().slice(0, 10);

  return (
    <div className="relative my-2 h-4">
      <div className="absolute left-0 right-0 top-1/2 h-px -translate-y-1/2 bg-slate-200" />
      {milestones.map((m) => {
        const ratio = dateToRatio(m.baselineEndDate, startDate, endDate);
        const isPast = m.baselineEndDate <= todayKey;
        return (
          <div
            key={m.id}
            className="absolute top-1/2 -translate-x-1/2 -translate-y-1/2"
            style={{ left: `${ratio * 100}%` }}
            title={`${m.name} — ${m.baselineEndDate}${m.status === 'Done' ? ' (Bitti)' : ''}`}
          >
            <div
              className={
                'h-2.5 w-2.5 rotate-45 ' +
                (m.status === 'Done'
                  ? 'bg-indigo-600'
                  : isPast
                    ? 'border-2 border-red-500 bg-white'
                    : 'border-2 border-indigo-300 bg-white')
              }
            />
          </div>
        );
      })}
    </div>
  );
}
