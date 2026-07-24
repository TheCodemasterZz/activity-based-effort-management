import type { WorkLogDto } from '../api/types';
import type { PeriodColumn } from './dateUtils';

export type GroupByDimension = 'employee' | 'project' | 'activityL1' | 'activityL2';

export const GROUP_BY_OPTIONS: { value: GroupByDimension; label: string }[] = [
  { value: 'employee', label: 'Kişi' },
  { value: 'project', label: 'Proje' },
  { value: 'activityL1', label: 'Activity L1' },
  { value: 'activityL2', label: 'Activity L2' },
];

export interface GroupedRow {
  /** Kökten bu satıra kadar olan tüm ataların rowKey'leriyle oluşan benzersiz yol (React key + aç/kapa state için). */
  path: string;
  rowKey: string;
  rowLabel: string;
  depth: number;
  cellHours: Record<string, number>;
  /** Sadece yaprak satırlarda dolu — hücre tıklama (ekle/görüntüle/düzenle/sil) için ham kayıtlar. */
  cellLogs: Record<string, WorkLogDto[]>;
  total: number;
  children?: GroupedRow[];
  /** Bu satır 'employee' boyutuna göre oluşturulduysa doldurulur (o boyuttaki key, yani çalışan id'si) —
   * hiç kaydı olmayan ama onaylı bir haftaya denk gelen boş günleri de doğru renklendirebilmek için. */
  userId?: string;
}

export interface GroupedResult {
  rows: GroupedRow[];
  grandTotalByColumn: Record<string, number>;
  grandTotal: number;
}

export type ResolveDimension = (
  dimension: GroupByDimension,
  log: WorkLogDto,
) => { key: string; label: string } | null;

function findColumn(columns: PeriodColumn[], workDate: string): PeriodColumn | undefined {
  return columns.find((c) => workDate >= c.startKey && workDate <= c.endKey);
}

function buildLevel(
  logs: WorkLogDto[],
  dimensions: GroupByDimension[],
  levelIndex: number,
  columns: PeriodColumn[],
  resolveDimension: ResolveDimension,
  depth: number,
  parentPath: string,
  /** Sadece en üst seviyede (levelIndex 0) ve o seviyenin boyutu 'employee' ise kullanılır:
   * görüntülenen dönemde hiç kaydı olmayan çalışanlar da (boş/sıfır saatlik bir satır olarak)
   * listeye eklensin diye tüm çalışan kadrosu burada önceden gruplara tohumlanır — böylece
   * kullanıcı, o kişi için o dönemde hiç log girilmemiş olsa bile satırı görüp hücreye
   * tıklayarak yeni kayıt ekleyebilir. */
  userRoster?: { id: string; name: string }[],
): GroupedRow[] {
  const dimension = dimensions[levelIndex];
  const groups = new Map<string, { label: string; logs: WorkLogDto[] }>();

  if (userRoster && dimension === 'employee') {
    for (const user of userRoster) {
      groups.set(user.id, { label: user.name, logs: [] });
    }
  }

  for (const log of logs) {
    const resolved = resolveDimension(dimension, log);
    if (!resolved) continue;

    let group = groups.get(resolved.key);
    if (!group) {
      group = { label: resolved.label, logs: [] };
      groups.set(resolved.key, group);
    }
    group.logs.push(log);
  }

  const isLeafLevel = levelIndex === dimensions.length - 1;

  const rows: GroupedRow[] = Array.from(groups.entries()).map(([key, group]) => {
    const path = `${parentPath}/${key}`;
    const cellHours: Record<string, number> = {};
    const cellLogs: Record<string, WorkLogDto[]> = {};
    let total = 0;

    for (const log of group.logs) {
      const column = findColumn(columns, log.workDate);
      if (!column) continue;

      cellHours[column.key] = (cellHours[column.key] ?? 0) + log.hours;
      total += log.hours;

      if (isLeafLevel) {
        (cellLogs[column.key] ??= []).push(log);
      }
    }

    const row: GroupedRow = { path, rowKey: key, rowLabel: group.label, depth, cellHours, cellLogs, total };
    if (dimension === 'employee') row.userId = key;

    if (!isLeafLevel) {
      row.children = buildLevel(group.logs, dimensions, levelIndex + 1, columns, resolveDimension, depth + 1, path);
    }

    return row;
  });

  return rows.sort((a, b) => b.total - a.total);
}

export function groupWorkLogs(
  logs: WorkLogDto[],
  columns: PeriodColumn[],
  dimensions: GroupByDimension[],
  resolveDimension: ResolveDimension,
  /** Görüntülenen dönemde hiç kaydı olmasa bile satır listesinde görünmesi gereken çalışan
   * kadrosu — bkz. buildLevel'daki userRoster açıklaması. */
  userRoster?: { id: string; name: string }[],
): GroupedResult {
  const rows = dimensions.length > 0
    ? buildLevel(logs, dimensions, 0, columns, resolveDimension, 0, 'root', userRoster)
    : [];

  const grandTotalByColumn: Record<string, number> = {};
  let grandTotal = 0;

  for (const log of logs) {
    const column = findColumn(columns, log.workDate);
    if (!column) continue;

    grandTotalByColumn[column.key] = (grandTotalByColumn[column.key] ?? 0) + log.hours;
    grandTotal += log.hours;
  }

  return { rows, grandTotalByColumn, grandTotal };
}
