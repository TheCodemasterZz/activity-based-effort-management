import { Fragment, useEffect, useState, type CSSProperties } from 'react';
import { eachDateKeyInRange, isCurrentColumn, type PeriodColumn } from '../../lib/dateUtils';
import type { GroupedRow } from '../../lib/groupWorkLogs';
import type { EmployeeWorkLogDto } from '../../api/types';

interface WorkLogTableProps {
  columns: PeriodColumn[];
  rows: GroupedRow[];
  grandTotalByColumn: Record<string, number>;
  grandTotal: number;
  holidayDateKeys: Set<string>;
  /** employeeId'ye göre onaylı [start,end] dönemleri — kaydı olmayan ama onaylı bir haftaya
   * denk gelen boş günleri de doğru renklendirebilmek için (bkz. cellApprovalStatus). */
  approvedRangesByEmployee?: Map<string, { start: string; end: string }[]>;
  /** employeeId'ye göre izin dönemleri — tam gün veya saatlik (kısmi) olabilir. */
  leaveRangesByEmployee?: Map<string, LeaveRange[]>;
  onCellClick: (row: GroupedRow, column: PeriodColumn) => void;
  /** Aynı satırda birden fazla hücre sürüklenerek seçildiğinde tetiklenir (toplu tarih aralığı ekleme). */
  onRangeSelect: (row: GroupedRow, startColumn: PeriodColumn, endColumn: PeriodColumn) => void;
}

interface DragState {
  row: GroupedRow;
  anchorIndex: number;
  currentIndex: number;
}

function formatHours(value: number | undefined): string {
  const v = value ?? 0;
  return `${v % 1 === 0 ? v : v.toFixed(1)}h`;
}

function isWeekendColumn(column: PeriodColumn): boolean {
  if (column.startKey !== column.endKey) return false;
  const day = new Date(`${column.startKey}T00:00:00`).getDay();
  return day === 0 || day === 6;
}

type ApprovalStatus = 'none' | 'partial' | 'full';

export interface LeaveRange {
  start: string;
  end: string;
  isFullDay: boolean;
}

type LeaveStatus = 'none' | 'partial' | 'full';

/** Bir hücrenin kapsadığı gün(ler) içinde çalışanın tam günlük veya kısmi (saatlik) izni var mı.
 * Tam gün izin varsa 'full', sadece kısmi izin varsa 'partial' döner. */
function cellLeaveStatus(
  employeeId: string | undefined,
  column: PeriodColumn,
  leaveRangesByEmployee: Map<string, LeaveRange[]> | undefined,
): LeaveStatus {
  if (!employeeId || !leaveRangesByEmployee) return 'none';
  const ranges = leaveRangesByEmployee.get(employeeId);
  if (!ranges || ranges.length === 0) return 'none';

  const days = eachDateKeyInRange(column.startKey, column.endKey);
  let hasFullDay = false;
  let hasPartial = false;

  for (const day of days) {
    for (const range of ranges) {
      if (day >= range.start && day <= range.end) {
        if (range.isFullDay) hasFullDay = true;
        else hasPartial = true;
      }
    }
  }

  if (hasFullDay) return 'full';
  if (hasPartial) return 'partial';
  return 'none';
}

/** Verilen günü kapsayan bir onay dönemi var mı. */
function isDateWithinRanges(dateKeyValue: string, ranges: { start: string; end: string }[] | undefined): boolean {
  if (!ranges) return false;
  return ranges.some((r) => dateKeyValue >= r.start && dateKeyValue <= r.end);
}

/** Bir hücrenin onay durumunu belirler. Önce, o hücrenin kapsadığı günlerin çalışan bazlı onay
 * dönemleriyle örtüşme oranına bakar — böylece kaydı olmayan ama onaylı bir haftaya denk gelen
 * boş günler de doğru renklenir (yalnızca satır tek bir çalışana karşılık geliyorsa, ör. "Kişi"
 * boyutuna göre gruplanmışsa). Bu bilgi yoksa, var olan kayıtların isApproved bayrağına döner. */
function cellApprovalStatus(
  logs: EmployeeWorkLogDto[] | undefined,
  employeeId: string | undefined,
  column: PeriodColumn,
  approvedRangesByEmployee: Map<string, { start: string; end: string }[]> | undefined,
): ApprovalStatus {
  if (employeeId && approvedRangesByEmployee) {
    const ranges = approvedRangesByEmployee.get(employeeId);
    const days = eachDateKeyInRange(column.startKey, column.endKey);
    const approvedDays = days.filter((d) => isDateWithinRanges(d, ranges)).length;
    if (approvedDays > 0) return approvedDays === days.length ? 'full' : 'partial';
    // Bu çalışan için hiç onay yoksa, kayıtların kendi isApproved bayrağına düş (aşağıda).
  }

  if (!logs || logs.length === 0) return 'none';
  const approvedCount = logs.filter((l) => l.isApproved).length;
  if (approvedCount === 0) return 'none';
  return approvedCount === logs.length ? 'full' : 'partial';
}

type NameSort = 'asc' | 'desc' | null;

function sortRowsByLabel(rows: GroupedRow[], dir: 'asc' | 'desc'): GroupedRow[] {
  const sorted = [...rows].sort((a, b) =>
    dir === 'asc' ? a.rowLabel.localeCompare(b.rowLabel, 'tr') : b.rowLabel.localeCompare(a.rowLabel, 'tr'),
  );
  return sorted.map((row) => (row.children ? { ...row, children: sortRowsByLabel(row.children, dir) } : row));
}

function isHolidayColumn(column: PeriodColumn, holidayDateKeys: Set<string>): boolean {
  return column.startKey === column.endKey && holidayDateKeys.has(column.startKey);
}

/** Tam onaylı hücrelerde günün kendi rengini (tatil/hafta sonu/bugün/normal) koruyarak üzerine
 * ince diyagonal çizgiler bindiren arka plan — metni ezmeyecek şekilde düşük opaklık ve geniş
 * aralık kullanır. */
const APPROVED_STRIPE_STYLE: CSSProperties = {
  backgroundImage:
    'repeating-linear-gradient(45deg, rgba(13,148,136,0.3) 0px, rgba(13,148,136,0.3) 2px, transparent 2px, transparent 9px)',
};

function columnHeaderClass(column: PeriodColumn, holidayDateKeys: Set<string>, todayKey: string): string {
  const isWeekend = isWeekendColumn(column);
  const isCurrent = isCurrentColumn(column, todayKey);

  if (isCurrent) return 'bg-amber-50';
  if (isHolidayColumn(column, holidayDateKeys)) return 'bg-red-50';
  if (isWeekend) return 'bg-slate-100';
  return '';
}

interface RowsProps {
  rows: GroupedRow[];
  columns: PeriodColumn[];
  collapsed: Set<string>;
  onToggle: (path: string) => void;
  drag: DragState | null;
  onCellMouseDown: (row: GroupedRow, columnIndex: number) => void;
  onCellMouseEnter: (row: GroupedRow, columnIndex: number) => void;
  holidayDateKeys: Set<string>;
  todayKey: string;
  approvedRangesByEmployee?: Map<string, { start: string; end: string }[]>;
  leaveRangesByEmployee?: Map<string, LeaveRange[]>;
}

function TableRows({
  rows,
  columns,
  collapsed,
  onToggle,
  drag,
  onCellMouseDown,
  onCellMouseEnter,
  holidayDateKeys,
  todayKey,
  approvedRangesByEmployee,
  leaveRangesByEmployee,
}: RowsProps) {
  return (
    <>
      {rows.map((row) => {
        const hasChildren = !!row.children && row.children.length > 0;
        const isCollapsed = collapsed.has(row.path);

        return (
          <Fragment key={row.path}>
            <tr className="border-b border-slate-200 last:border-0 hover:bg-slate-50">
              <td
                className="sticky left-0 z-10 border-r border-slate-200 bg-white px-3 py-2 font-medium text-slate-700"
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
              {columns.map((column, index) => {
                const clickable = !hasChildren;
                const isSelected =
                  clickable &&
                  drag !== null &&
                  drag.row.path === row.path &&
                  index >= Math.min(drag.anchorIndex, drag.currentIndex) &&
                  index <= Math.max(drag.anchorIndex, drag.currentIndex);
                // İzin/tatil/onay renklendirmesi kasıtlı olarak 'clickable'a değil, satırın kendi
                // 'employeeId'sine bağlı: sadece Group by'daki gerçek Kişi (employee) satırında
                // görünsün istendi — Kişi satırı çocuklu (parent/non-leaf) olsa bile renklenir,
                // ama altındaki Proje/Müşteri gibi farklı boyuttaki satırlarda hiç görünmez.
                const hasEmployeeContext = !!row.employeeId;
                const approval = hasEmployeeContext
                  ? cellApprovalStatus(row.cellLogs[column.key], row.employeeId, column, approvedRangesByEmployee)
                  : 'none';
                const leave = hasEmployeeContext
                  ? cellLeaveStatus(row.employeeId, column, leaveRangesByEmployee)
                  : 'none';
                const isHoliday = isHolidayColumn(column, holidayDateKeys);

                const title =
                  leave === 'full'
                    ? 'İzinli (Tam Gün)'
                    : leave === 'partial'
                      ? 'İzinli (Kısmi/Saatlik)'
                      : approval === 'full'
                        ? 'Onaylandı — değiştirilemez'
                        : approval === 'partial'
                          ? 'Kısmen onaylı'
                          : undefined;

                // Tam onay: günün kendi rengi (tatil/hafta sonu/izin/bugün/normal) korunur,
                // üzerine diyagonal çizgi deseni bindirilir — izinli bir gün aynı zamanda
                // onaylı bir haftaya denk gelse bile onay durumu ayrıca görünür kalır.
                const showApprovedStripe = approval === 'full' && !isSelected;
                const baseBgClass = isSelected
                  ? 'bg-indigo-100'
                  : leave === 'full'
                    ? 'bg-violet-200'
                    : leave === 'partial'
                      ? 'bg-violet-100'
                      : isHoliday
                        ? 'bg-red-50'
                        : approval === 'partial'
                          ? 'bg-teal-50'
                          : columnHeaderClass(column, holidayDateKeys, todayKey);

                return (
                  <td
                    key={column.key}
                    onMouseDown={clickable ? () => onCellMouseDown(row, index) : undefined}
                    onMouseEnter={clickable ? () => onCellMouseEnter(row, index) : undefined}
                    title={title}
                    style={showApprovedStripe ? APPROVED_STRIPE_STYLE : undefined}
                    className={
                      'select-none border-r border-slate-200 px-2 py-2 text-right tabular-nums text-slate-600' +
                      (clickable ? ' cursor-pointer hover:bg-indigo-50' : '') +
                      ' ' +
                      baseBgClass
                    }
                  >
                    {row.cellHours[column.key] ? formatHours(row.cellHours[column.key]) : ''}
                  </td>
                );
              })}
              <td className="px-3 py-2 text-right font-semibold text-slate-800">{formatHours(row.total)}</td>
            </tr>
            {hasChildren && !isCollapsed && (
              <TableRows
                rows={row.children!}
                columns={columns}
                collapsed={collapsed}
                onToggle={onToggle}
                drag={drag}
                onCellMouseDown={onCellMouseDown}
                onCellMouseEnter={onCellMouseEnter}
                holidayDateKeys={holidayDateKeys}
                todayKey={todayKey}
                approvedRangesByEmployee={approvedRangesByEmployee}
                leaveRangesByEmployee={leaveRangesByEmployee}
              />
            )}
          </Fragment>
        );
      })}
    </>
  );
}

export function WorkLogTable({
  columns,
  rows,
  grandTotalByColumn,
  grandTotal,
  holidayDateKeys,
  approvedRangesByEmployee,
  leaveRangesByEmployee,
  onCellClick,
  onRangeSelect,
}: WorkLogTableProps) {
  const [collapsed, setCollapsed] = useState<Set<string>>(new Set());
  const [drag, setDrag] = useState<DragState | null>(null);
  // Varsayılan olarak her zaman A-Z sıralı başlar — kullanıcı sütun başlığına tıklayarak
  // sırayı değiştirebilir/kapatabilir.
  const [nameSort, setNameSort] = useState<NameSort>('asc');
  const todayKey = new Date().toISOString().slice(0, 10);
  const displayRows = nameSort ? sortRowsByLabel(rows, nameSort) : rows;

  const toggle = (path: string) => {
    setCollapsed((prev) => {
      const next = new Set(prev);
      if (next.has(path)) next.delete(path);
      else next.add(path);
      return next;
    });
  };

  const handleCellMouseDown = (row: GroupedRow, columnIndex: number) => {
    setDrag({ row, anchorIndex: columnIndex, currentIndex: columnIndex });
  };

  const handleCellMouseEnter = (row: GroupedRow, columnIndex: number) => {
    setDrag((prev) => (prev && prev.row.path === row.path ? { ...prev, currentIndex: columnIndex } : prev));
  };

  useEffect(() => {
    if (!drag) return;

    const handleMouseUp = () => {
      const minIndex = Math.min(drag.anchorIndex, drag.currentIndex);
      const maxIndex = Math.max(drag.anchorIndex, drag.currentIndex);

      if (minIndex === maxIndex) {
        onCellClick(drag.row, columns[minIndex]);
      } else {
        onRangeSelect(drag.row, columns[minIndex], columns[maxIndex]);
      }
      setDrag(null);
    };

    window.addEventListener('mouseup', handleMouseUp);
    return () => window.removeEventListener('mouseup', handleMouseUp);
  }, [drag, columns, onCellClick, onRangeSelect]);

  return (
    <div className="max-h-[60vh] overflow-auto rounded-xl border border-slate-200 bg-white">
      <table className="min-w-full border-collapse text-sm">
        <thead>
          <tr className="border-b border-slate-200 bg-slate-50">
            <th className="sticky left-0 top-0 z-30 min-w-[28rem] border-r border-b border-slate-200 bg-slate-50 px-3 py-2 text-left font-semibold text-slate-500">
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
                className={`sticky top-0 z-20 min-w-[3rem] border-r border-b border-slate-200 px-2 py-2 text-center font-semibold text-slate-500 ${columnHeaderClass(column, holidayDateKeys, todayKey) || 'bg-slate-50'}`}
              >
                <div>{column.label}</div>
                {column.sublabel && <div className="text-[10px] font-normal text-slate-400">{column.sublabel}</div>}
              </th>
            ))}
            <th className="sticky top-0 z-20 min-w-[4rem] border-b border-slate-200 bg-slate-50 px-3 py-2 text-center font-semibold text-slate-600">
              TOPLAM
            </th>
          </tr>
        </thead>
        <tbody>
          {rows.length === 0 && (
            <tr>
              <td colSpan={columns.length + 2} className="px-4 py-8 text-center text-slate-400">
                Bu dönem için kayıt bulunamadı.
              </td>
            </tr>
          )}
          <TableRows
            rows={displayRows}
            columns={columns}
            collapsed={collapsed}
            onToggle={toggle}
            drag={drag}
            onCellMouseDown={handleCellMouseDown}
            onCellMouseEnter={handleCellMouseEnter}
            holidayDateKeys={holidayDateKeys}
            todayKey={todayKey}
            approvedRangesByEmployee={approvedRangesByEmployee}
            leaveRangesByEmployee={leaveRangesByEmployee}
          />
        </tbody>
        <tfoot>
          <tr className="border-t border-slate-200 bg-slate-50">
            <td className="sticky left-0 z-10 border-r border-slate-200 bg-slate-50 px-3 py-2 font-semibold text-slate-700">
              GENEL TOPLAM
            </td>
            {columns.map((column) => (
              <td
                key={column.key}
                className={`border-r border-slate-200 px-2 py-2 text-right font-semibold text-slate-700 ${columnHeaderClass(column, holidayDateKeys, todayKey)}`}
              >
                {formatHours(grandTotalByColumn[column.key])}
              </td>
            ))}
            <td className="px-3 py-2 text-right font-bold text-indigo-700">{formatHours(grandTotal)}</td>
          </tr>
        </tfoot>
      </table>
    </div>
  );
}
