import { Fragment, useState } from 'react';
import type { PeriodColumn } from '../../lib/dateUtils';
import type { AccuracyRow } from '../../lib/groupWorkLogsAccuracy';

interface PlanningAccuracyTableProps {
  columns: PeriodColumn[];
  rows: AccuracyRow[];
  grandTotalActualByColumn: Record<string, number>;
  grandTotalPlannedByColumn: Record<string, number>;
  grandTotalActual: number;
  grandTotalPlanned: number;
}

function formatHours(value: number): string {
  return `${value % 1 === 0 ? value : value.toFixed(1)}h`;
}

type NameSort = 'asc' | 'desc' | null;

function sortRowsByLabel(rows: AccuracyRow[], dir: 'asc' | 'desc'): AccuracyRow[] {
  const sorted = [...rows].sort((a, b) =>
    dir === 'asc' ? a.rowLabel.localeCompare(b.rowLabel, 'tr') : b.rowLabel.localeCompare(a.rowLabel, 'tr'),
  );
  return sorted.map((row) => (row.children ? { ...row, children: sortRowsByLabel(row.children, dir) } : row));
}

export type VarianceStatus = 'none' | 'onTrack' | 'moderate' | 'large' | 'critical';

/** Sapma sınıflandırması: plan yoksa (hem actual hem planned 0) 'none'; plan vardı ama hiç
 * gerçekleşmediyse (ya da tam tersi) doğrudan en kötü durum olan 'critical'; aksi halde
 * yüzdesel sapmaya göre 4 kademe. Sayfa üstündeki oran widget'ları (bkz.
 * PlanningAccuracyVarianceWidgets) da hücrelerle birebir aynı sonucu üretmek için bunu kullanır. */
export function varianceStatus(actual: number, planned: number): VarianceStatus {
  if (actual === 0 && planned === 0) return 'none';
  if (planned === 0 || actual === 0) return 'critical';
  const pct = Math.abs((actual - planned) / planned) * 100;
  if (pct <= 10) return 'onTrack';
  if (pct <= 30) return 'moderate';
  if (pct <= 50) return 'large';
  return 'critical';
}

// Kapasite Yönetimi'ndeki CapacityCell ile aynı görsel dil: ince renkli metin değil, hücreyi
// dolduran, kutu gibi görünen renkli bir arka plan.
const CELL_STATUS_CLASS: Record<Exclude<VarianceStatus, 'none'>, string> = {
  onTrack: 'bg-emerald-100 text-emerald-700',
  moderate: 'bg-amber-400 text-white',
  large: 'bg-orange-500 text-white',
  critical: 'bg-red-700 text-white',
};

function AccuracyCell({ actual, planned }: { actual: number; planned: number }) {
  const status = varianceStatus(actual, planned);
  if (status === 'none') {
    return (
      <div className="rounded-md bg-slate-50 px-2 py-1.5 text-center text-[11px] text-slate-300">—</div>
    );
  }
  const pct = planned > 0 ? Math.round(((actual - planned) / planned) * 1000) / 10 : null;
  const title =
    planned === 0
      ? `Plan yoktu, ${formatHours(actual)} gerçekleşti.`
      : actual === 0
        ? `${formatHours(planned)} planlanmıştı, hiç gerçekleşmedi.`
        : `Sapma: ${pct! > 0 ? '+' : ''}${pct}% (${formatHours(actual)} / ${formatHours(planned)})`;

  return (
    <div
      className={`rounded-md px-2 py-1.5 text-center text-[11px] font-semibold ${CELL_STATUS_CLASS[status]}`}
      title={title}
    >
      {formatHours(actual)} / {formatHours(planned)}
    </div>
  );
}

interface RowsProps {
  rows: AccuracyRow[];
  columns: PeriodColumn[];
  collapsed: Set<string>;
  onToggle: (path: string) => void;
}

function TableRows({ rows, columns, collapsed, onToggle }: RowsProps) {
  return (
    <>
      {rows.map((row) => {
        const hasChildren = !!row.children && row.children.length > 0;
        const isCollapsed = collapsed.has(row.path);

        return (
          <Fragment key={row.path}>
            <tr className="border-b border-slate-200 last:border-0 hover:bg-slate-50">
              <td
                className="sticky left-0 z-10 w-[28rem] max-w-[28rem] border-r border-slate-200 bg-white px-3 py-2 font-medium text-slate-700"
                style={{ paddingLeft: `${0.75 + row.depth * 1.25}rem` }}
              >
                {hasChildren && (
                  <button
                    type="button"
                    onClick={() => onToggle(row.path)}
                    className="mr-1.5 inline-block w-3 text-slate-400"
                  >
                    {isCollapsed ? '▶' : '▼'}
                  </button>
                )}
                {row.rowLabel}
              </td>
              {columns.map((column) => (
                <td key={column.key} className="border-r border-slate-200 p-1">
                  <AccuracyCell actual={row.cellActual[column.key] ?? 0} planned={row.cellPlanned[column.key] ?? 0} />
                </td>
              ))}
              <td className="p-1">
                <AccuracyCell actual={row.totalActual} planned={row.totalPlanned} />
              </td>
            </tr>
            {hasChildren && !isCollapsed && (
              <TableRows rows={row.children!} columns={columns} collapsed={collapsed} onToggle={onToggle} />
            )}
          </Fragment>
        );
      })}
    </>
  );
}

export const PLANNING_ACCURACY_LEGEND_ITEMS: { swatchClass: string; label: string }[] = [
  { swatchClass: 'bg-emerald-100', label: 'Plan tuttu (±%10 içi sapma)' },
  { swatchClass: 'bg-amber-400', label: 'Orta sapma (%10–30)' },
  { swatchClass: 'bg-orange-500', label: 'Büyük sapma (%30–50)' },
  { swatchClass: 'bg-red-700', label: 'Kritik sapma (>%50)' },
  { swatchClass: 'bg-slate-50 border border-slate-200', label: 'Ne plan ne gerçekleşen var (—)' },
];

/** Planlama Doğruluğu tablosu — WorkLogTable'daki expand/collapse ağaç mantığının sadeleştirilmiş
 * (tıklama/sürükleme/onay-izin renklendirmesi olmayan, salt-okunur) hali. Her hücrede
 * "Gerçekleşen / Planlanan" gösterilir, arka plan rengi sapma büyüklüğünü işaret eder. */
export function PlanningAccuracyTable({
  columns,
  rows,
  grandTotalActualByColumn,
  grandTotalPlannedByColumn,
  grandTotalActual,
  grandTotalPlanned,
}: PlanningAccuracyTableProps) {
  const [collapsed, setCollapsed] = useState<Set<string>>(new Set());
  // Diğer tablolarda olduğu gibi her zaman A-Z sıralı başlar — başlığa tıklayarak değiştirilebilir.
  const [nameSort, setNameSort] = useState<NameSort>('asc');
  const displayRows = nameSort ? sortRowsByLabel(rows, nameSort) : rows;

  const toggle = (path: string) => {
    setCollapsed((prev) => {
      const next = new Set(prev);
      if (next.has(path)) next.delete(path);
      else next.add(path);
      return next;
    });
  };

  return (
    <div className="h-full overflow-auto rounded-xl border border-slate-200 bg-white">
      <table className="min-w-full border-collapse text-sm">
        <thead>
          <tr className="border-b border-slate-200 bg-slate-50">
            <th className="sticky left-0 top-0 z-30 w-[28rem] min-w-[28rem] max-w-[28rem] border-r border-b border-slate-200 bg-slate-50 px-3 py-2 text-left font-semibold text-slate-500">
              <button
                type="button"
                onClick={() => setNameSort((prev) => (prev === 'asc' ? 'desc' : prev === 'desc' ? null : 'asc'))}
                className="flex items-center gap-1.5 hover:text-slate-700"
                title="İsme göre sırala"
              >
                <span>İsim</span>
                <span
                  className={
                    'text-base leading-none ' + (nameSort ? 'font-bold text-indigo-600' : 'text-slate-400')
                  }
                >
                  {nameSort === 'asc' ? '▲' : nameSort === 'desc' ? '▼' : '⇅'}
                </span>
              </button>
            </th>
            {columns.map((column) => (
              <th
                key={column.key}
                className="sticky top-0 z-20 min-w-[6rem] border-r border-b border-slate-200 bg-slate-50 px-2 py-2 text-center font-semibold text-slate-500"
              >
                <div>{column.label}</div>
                {column.sublabel && <div className="text-[10px] font-normal text-slate-400">{column.sublabel}</div>}
              </th>
            ))}
            <th className="sticky top-0 z-20 min-w-[6rem] border-b border-slate-200 bg-slate-50 px-3 py-2 text-center font-semibold text-slate-600">
              TOPLAM
            </th>
          </tr>
        </thead>
        <tbody>
          {rows.length === 0 && (
            <tr>
              <td colSpan={columns.length + 2} className="px-4 py-8 text-center text-slate-400">
                Bu dönem için karşılaştırılacak (geçmiş) veri bulunamadı.
              </td>
            </tr>
          )}
          <TableRows rows={displayRows} columns={columns} collapsed={collapsed} onToggle={toggle} />
        </tbody>
        <tfoot>
          <tr className="border-t border-slate-200 bg-slate-50">
            <td className="sticky left-0 z-10 w-[28rem] max-w-[28rem] border-r border-slate-200 bg-slate-50 px-3 py-2 font-semibold text-slate-700">
              GENEL TOPLAM
            </td>
            {columns.map((column) => (
              <td key={column.key} className="border-r border-slate-200 p-1">
                <AccuracyCell
                  actual={grandTotalActualByColumn[column.key] ?? 0}
                  planned={grandTotalPlannedByColumn[column.key] ?? 0}
                />
              </td>
            ))}
            <td className="p-1">
              <AccuracyCell actual={grandTotalActual} planned={grandTotalPlanned} />
            </td>
          </tr>
        </tfoot>
      </table>
    </div>
  );
}
