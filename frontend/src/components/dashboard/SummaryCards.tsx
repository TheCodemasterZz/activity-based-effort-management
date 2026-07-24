import type { ReactNode } from 'react';
import { TrendAreaWidget } from './charts/TrendAreaWidget';
import { TrendBarWidget } from './charts/TrendBarWidget';
import { RatioGaugeWidget } from './charts/RatioGaugeWidget';
import { computeTrendPercent, type ChartPoint } from '../../lib/trend';

function TrendBadge({ percent }: { percent: number | null | undefined }) {
  if (percent === null || percent === undefined || !Number.isFinite(percent)) return null;
  const isUp = percent >= 0;
  return (
    <span
      className={`inline-flex shrink-0 items-center gap-0.5 rounded-full px-1.5 py-0.5 text-[11px] font-semibold ${
        isUp ? 'bg-emerald-50 text-emerald-600' : 'bg-rose-50 text-rose-600'
      }`}
    >
      {isUp ? '▲' : '▼'} {Math.abs(percent).toFixed(1)}%
    </span>
  );
}

export interface StatCardShellProps {
  icon: string;
  iconBg: string;
  label: string;
  value: string;
  caption: string;
  trendPercent?: number | null;
  chart?: ReactNode;
}

/** Diğer sayfalardaki özet widget'ların (bkz. Planlama Doğruluğu) da kullandığı ortak kart
 * kabuğu — ikon rozeti + etiket + değer + alt açıklama + sağda opsiyonel küçük grafik/gösterge. */
export function StatCardShell({ icon, iconBg, label, value, caption, trendPercent, chart }: StatCardShellProps) {
  return (
    <div className="flex flex-1 flex-col justify-between gap-2 overflow-hidden rounded-xl border border-slate-200 bg-white p-3">
      <div className="flex items-center justify-between gap-2">
        <div className="flex items-center gap-1.5">
          <span className={`flex h-6 w-6 items-center justify-center rounded-lg text-sm ${iconBg}`}>{icon}</span>
          <span className="text-xs font-semibold uppercase tracking-wide text-slate-400">{label}</span>
        </div>
        <TrendBadge percent={trendPercent} />
      </div>
      <div className="flex items-end justify-between gap-3">
        <div>
          <div className="text-xl font-bold text-slate-800">{value}</div>
          <div className="mt-0.5 text-xs text-slate-400">{caption}</div>
        </div>
        {chart}
      </div>
    </div>
  );
}

interface SummaryCardsProps {
  totalHours: number;
  totalCount: number;
  activePeopleCount: number;
  totalUserCount: number;
  avgDailyHours: number;
  approvedHours: number;
  periodLabel: string;
  hoursSeries: ChartPoint[];
  countSeries: ChartPoint[];
  avgSeries: ChartPoint[];
  approvedHoursSeries: ChartPoint[];
}

export function SummaryCards({
  totalHours,
  totalCount,
  activePeopleCount,
  totalUserCount,
  avgDailyHours,
  approvedHours,
  periodLabel,
  hoursSeries,
  countSeries,
  avgSeries,
  approvedHoursSeries,
}: SummaryCardsProps) {
  const activeRatio = totalUserCount > 0 ? (activePeopleCount / totalUserCount) * 100 : 0;
  const approvedRatio = totalHours > 0 ? (approvedHours / totalHours) * 100 : 0;

  return (
    <div className="flex flex-col gap-3 sm:flex-row">
      <StatCardShell
        icon="⏱"
        iconBg="bg-indigo-50 text-indigo-600"
        label="Toplam Saat"
        value={`${totalHours.toFixed(1)}h`}
        caption={periodLabel}
        trendPercent={computeTrendPercent(hoursSeries)}
        chart={<TrendAreaWidget data={hoursSeries} color="#4f46e5" unit="h" height={34} />}
      />
      <StatCardShell
        icon="📄"
        iconBg="bg-blue-50 text-blue-600"
        label="Toplam Kayıt"
        value={String(totalCount)}
        caption="Girilen log sayısı"
        trendPercent={computeTrendPercent(countSeries)}
        chart={<TrendBarWidget data={countSeries} color="#2563eb" height={34} />}
      />
      <StatCardShell
        icon="👥"
        iconBg="bg-emerald-50 text-emerald-600"
        label="Aktif Kişi"
        value={String(activePeopleCount)}
        caption={totalUserCount > 0 ? `${totalUserCount} kişiden` : 'Log giren kişi sayısı'}
        chart={
          totalUserCount > 0 ? (
            <RatioGaugeWidget percent={activeRatio} color="#059669" label="Aktif kişi oranı" size={44} />
          ) : undefined
        }
      />
      <StatCardShell
        icon="📊"
        iconBg="bg-amber-50 text-amber-600"
        label="Ortalama Günlük Saat"
        value={`${avgDailyHours.toFixed(1)}h`}
        caption="Kişi başı ortalama"
        trendPercent={computeTrendPercent(avgSeries)}
        chart={<TrendBarWidget data={avgSeries} color="#f59e0b" unit="h" height={34} />}
      />
      <StatCardShell
        icon="🔒"
        iconBg="bg-teal-50 text-teal-600"
        label="Onaylanan Efor Süresi"
        value={`${approvedHours.toFixed(1)}h`}
        caption={totalHours > 0 ? `Toplamın %${approvedRatio.toFixed(0)}'i` : 'Henüz onay yok'}
        trendPercent={computeTrendPercent(approvedHoursSeries)}
        chart={<TrendAreaWidget data={approvedHoursSeries} color="#0d9488" unit="h" height={34} />}
      />
    </div>
  );
}
