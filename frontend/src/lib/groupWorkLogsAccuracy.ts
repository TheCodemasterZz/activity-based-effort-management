import type { WorkLogDto } from '../api/types';
import type { PeriodColumn } from './dateUtils';
import type { GroupByDimension, ResolveDimension } from './groupWorkLogs';

/** groupWorkLogs.ts'teki buildLevel ile aynı gruplama mantığı, ama tek bir `hours` alanı yerine
 * aynı anda hem Actual hem Planned kayıtları grupluyor — Planlama Doğruluğu raporu her hücrede
 * ikisini birden göstermek zorunda olduğu için ayrı bir dosyada tutuluyor (mevcut, test edilmiş
 * groupWorkLogs'a dokunmadan). */
export interface AccuracyRow {
  path: string;
  rowKey: string;
  rowLabel: string;
  depth: number;
  cellActual: Record<string, number>;
  cellPlanned: Record<string, number>;
  totalActual: number;
  totalPlanned: number;
  children?: AccuracyRow[];
  userId?: string;
}

export interface AccuracyResult {
  rows: AccuracyRow[];
  grandTotalActualByColumn: Record<string, number>;
  grandTotalPlannedByColumn: Record<string, number>;
  grandTotalActual: number;
  grandTotalPlanned: number;
}

function findColumn(columns: PeriodColumn[], workDate: string): PeriodColumn | undefined {
  return columns.find((c) => workDate >= c.startKey && workDate <= c.endKey);
}

function buildLevel(
  actualLogs: WorkLogDto[],
  plannedLogs: WorkLogDto[],
  dimensions: GroupByDimension[],
  levelIndex: number,
  columns: PeriodColumn[],
  resolveDimension: ResolveDimension,
  depth: number,
  parentPath: string,
  userRoster?: { id: string; name: string }[],
): AccuracyRow[] {
  const dimension = dimensions[levelIndex];
  const groups = new Map<string, { label: string; actual: WorkLogDto[]; planned: WorkLogDto[] }>();

  if (userRoster && dimension === 'employee') {
    for (const employee of userRoster) {
      groups.set(employee.id, { label: employee.name, actual: [], planned: [] });
    }
  }

  const ensureGroup = (log: WorkLogDto) => {
    const resolved = resolveDimension(dimension, log);
    if (!resolved) return null;
    let group = groups.get(resolved.key);
    if (!group) {
      group = { label: resolved.label, actual: [], planned: [] };
      groups.set(resolved.key, group);
    }
    return group;
  };

  for (const log of actualLogs) {
    const group = ensureGroup(log);
    group?.actual.push(log);
  }
  for (const log of plannedLogs) {
    const group = ensureGroup(log);
    group?.planned.push(log);
  }

  const isLeafLevel = levelIndex === dimensions.length - 1;

  const rows: AccuracyRow[] = Array.from(groups.entries()).map(([key, group]) => {
    const path = `${parentPath}/${key}`;
    const cellActual: Record<string, number> = {};
    const cellPlanned: Record<string, number> = {};
    let totalActual = 0;
    let totalPlanned = 0;

    for (const log of group.actual) {
      const column = findColumn(columns, log.workDate);
      if (!column) continue;
      cellActual[column.key] = (cellActual[column.key] ?? 0) + log.hours;
      totalActual += log.hours;
    }
    for (const log of group.planned) {
      const column = findColumn(columns, log.workDate);
      if (!column) continue;
      cellPlanned[column.key] = (cellPlanned[column.key] ?? 0) + log.hours;
      totalPlanned += log.hours;
    }

    const row: AccuracyRow = {
      path,
      rowKey: key,
      rowLabel: group.label,
      depth,
      cellActual,
      cellPlanned,
      totalActual,
      totalPlanned,
    };
    if (dimension === 'employee') row.userId = key;

    if (!isLeafLevel) {
      row.children = buildLevel(
        group.actual,
        group.planned,
        dimensions,
        levelIndex + 1,
        columns,
        resolveDimension,
        depth + 1,
        path,
      );
    }

    return row;
  });

  return rows.sort((a, b) => b.totalActual + b.totalPlanned - (a.totalActual + a.totalPlanned));
}

export function groupWorkLogsAccuracy(
  actualLogs: WorkLogDto[],
  plannedLogs: WorkLogDto[],
  columns: PeriodColumn[],
  dimensions: GroupByDimension[],
  resolveDimension: ResolveDimension,
  userRoster?: { id: string; name: string }[],
): AccuracyResult {
  const rows =
    dimensions.length > 0
      ? buildLevel(actualLogs, plannedLogs, dimensions, 0, columns, resolveDimension, 0, 'root', userRoster)
      : [];

  const grandTotalActualByColumn: Record<string, number> = {};
  const grandTotalPlannedByColumn: Record<string, number> = {};
  let grandTotalActual = 0;
  let grandTotalPlanned = 0;

  for (const log of actualLogs) {
    const column = findColumn(columns, log.workDate);
    if (!column) continue;
    grandTotalActualByColumn[column.key] = (grandTotalActualByColumn[column.key] ?? 0) + log.hours;
    grandTotalActual += log.hours;
  }
  for (const log of plannedLogs) {
    const column = findColumn(columns, log.workDate);
    if (!column) continue;
    grandTotalPlannedByColumn[column.key] = (grandTotalPlannedByColumn[column.key] ?? 0) + log.hours;
    grandTotalPlanned += log.hours;
  }

  return { rows, grandTotalActualByColumn, grandTotalPlannedByColumn, grandTotalActual, grandTotalPlanned };
}
