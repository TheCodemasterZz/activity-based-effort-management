import { useMemo } from 'react';
import { StatCardShell } from './SummaryCards';
import { RatioGaugeWidget } from './charts/RatioGaugeWidget';
import { varianceStatus, type VarianceStatus } from './PlanningAccuracyTable';
import type { AccuracyRow } from '../../lib/groupWorkLogsAccuracy';
import type { PeriodColumn } from '../../lib/dateUtils';

interface PlanningAccuracyVarianceWidgetsProps {
  rows: AccuracyRow[];
  columns: PeriodColumn[];
}

function collectLeafRows(rows: AccuracyRow[]): AccuracyRow[] {
  return rows.flatMap((row) => (row.children && row.children.length > 0 ? collectLeafRows(row.children) : [row]));
}

const TIERS: { key: VarianceStatus; icon: string; iconBg: string; label: string; caption: string; color: string }[] = [
  { key: 'onTrack', icon: '✅', iconBg: 'bg-emerald-50 text-emerald-600', label: 'Plan Tuttu', caption: '±%10 içi sapma', color: '#059669' },
  { key: 'moderate', icon: '⚠️', iconBg: 'bg-amber-50 text-amber-600', label: 'Orta Sapma', caption: '%10–30 arası', color: '#f59e0b' },
  { key: 'large', icon: '🔶', iconBg: 'bg-orange-50 text-orange-600', label: 'Büyük Sapma', caption: '%30–50 arası', color: '#f97316' },
  { key: 'critical', icon: '🔴', iconBg: 'bg-red-50 text-red-600', label: 'Kritik Sapma', caption: '>%50', color: '#dc2626' },
  { key: 'none', icon: '➖', iconBg: 'bg-slate-100 text-slate-500', label: 'Veri Yok', caption: 'Ne plan ne gerçekleşen var', color: '#94a3b8' },
];

/** Gerçekleşen Efor sayfasındaki (bkz. SummaryCards) özet kart tasarımını yeniden kullanarak,
 * tablodaki hücrelerin sapma dağılımını (Plan Tuttu/Orta/Büyük/Kritik/Veri Yok) oran olarak
 * gösteren 5 widget — hücre renklerinin kaynağı olan aynı varianceStatus fonksiyonunu kullanır,
 * bu yüzden tabloyla her zaman tutarlıdır. */
export function PlanningAccuracyVarianceWidgets({ rows, columns }: PlanningAccuracyVarianceWidgetsProps) {
  const counts = useMemo(() => {
    const tally: Record<VarianceStatus, number> = { none: 0, onTrack: 0, moderate: 0, large: 0, critical: 0 };
    const leafRows = collectLeafRows(rows);
    let total = 0;
    for (const row of leafRows) {
      for (const column of columns) {
        const status = varianceStatus(row.cellActual[column.key] ?? 0, row.cellPlanned[column.key] ?? 0);
        tally[status]++;
        total++;
      }
    }
    return { tally, total };
  }, [rows, columns]);

  return (
    <div className="mb-4 flex flex-col gap-3 sm:flex-row">
      {TIERS.map((tier) => {
        const count = counts.tally[tier.key];
        const percent = counts.total > 0 ? (count / counts.total) * 100 : 0;
        return (
          <StatCardShell
            key={tier.key}
            icon={tier.icon}
            iconBg={tier.iconBg}
            label={tier.label}
            value={`%${percent.toFixed(0)}`}
            caption={`${tier.caption} · ${count} hücre`}
            chart={<RatioGaugeWidget percent={percent} color={tier.color} label={tier.label} size={44} />}
          />
        );
      })}
    </div>
  );
}
