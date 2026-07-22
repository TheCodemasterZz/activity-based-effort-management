import { useMemo, useState } from 'react';
import { PeriodModeSelect } from '../components/dashboard/PeriodModeSelect';
import { MonthNavigator } from '../components/dashboard/MonthNavigator';
import { GroupByMultiSelect } from '../components/dashboard/GroupByMultiSelect';
import { MqlFilterInput } from '../components/dashboard/MqlFilterInput';
import { PlanningAccuracyTable, PLANNING_ACCURACY_LEGEND_ITEMS } from '../components/dashboard/PlanningAccuracyTable';
import { PlanningAccuracyVarianceWidgets } from '../components/dashboard/PlanningAccuracyVarianceWidgets';
import {
  buildCustomDailyRange,
  dateKey,
  getPeriodRange,
  navigatePeriod,
  shiftCustomRange,
  type CustomRange,
  type PeriodMode,
} from '../lib/dateUtils';
import { groupWorkLogsAccuracy } from '../lib/groupWorkLogsAccuracy';
import type { GroupByDimension } from '../lib/groupWorkLogs';
import { evaluateMql, type MqlNode } from '../lib/mql';
import { useWorkLogs } from '../hooks/useWorkLogs';
import { useEmployees } from '../hooks/useEmployees';
import { useProjects } from '../hooks/useProjects';
import { useCustomers } from '../hooks/useCustomers';
import { useAllActivities } from '../hooks/useActivities';
import { WORK_LOG_ENTRY_TYPE, type EmployeeWorkLogDto } from '../api/types';

function PlanningAccuracyLegend() {
  return (
    <div className="mb-3 flex flex-wrap items-center gap-x-6 gap-y-2 text-sm font-medium text-slate-600">
      {PLANNING_ACCURACY_LEGEND_ITEMS.map((item) => (
        <div key={item.label} className="flex items-center gap-2">
          <span className={`h-4 w-4 shrink-0 rounded ${item.swatchClass}`} />
          <span>{item.label}</span>
        </div>
      ))}
    </div>
  );
}

/** Geçmiş dönemler için Planlanan vs Gerçekleşen sapmasını gösteren salt-okunur rapor. Sadece
 * bugüne kadarki sütunlar dahil edilir — gelecek günler için henüz "gerçekleşen" olmadığından
 * karşılaştırma anlamsız olurdu. Grup-by/MQL/dönem araç çubuğu ReportPage ile birebir aynı
 * bileşenleri kullanır; hesaplama tarafı groupWorkLogsAccuracy ile Actual+Planned'ı aynı anda
 * gruplar (mevcut, tek-taraflı groupWorkLogs'a dokunmadan). */
export function PlanningAccuracyPage() {
  const [periodMode, setPeriodMode] = useState<PeriodMode>('daily');
  const [anchorDate, setAnchorDate] = useState(new Date());
  const [customRange, setCustomRange] = useState<CustomRange | null>(null);
  const [groupBy, setGroupBy] = useState<GroupByDimension[]>(['employee']);
  const [mqlAst, setMqlAst] = useState<MqlNode | null>(null);

  const periodRange = useMemo(
    () => (customRange ? buildCustomDailyRange(customRange) : getPeriodRange(periodMode, anchorDate)),
    [periodMode, anchorDate, customRange],
  );

  const todayKey = new Date().toISOString().slice(0, 10);
  // Sadece bugüne kadarki (geçmiş) sütunlar — gelecek günlerde henüz Actual olamayacağı için
  // Planlama Doğruluğu karşılaştırması bu sütunlarda anlamsız olur.
  const pastColumns = useMemo(
    () => periodRange.columns.filter((c) => c.startKey <= todayKey),
    [periodRange.columns, todayKey],
  );

  const actualLogsQuery = useWorkLogs(periodRange.startKey, periodRange.endKey, WORK_LOG_ENTRY_TYPE.Actual);
  const plannedLogsQuery = useWorkLogs(periodRange.startKey, periodRange.endKey, WORK_LOG_ENTRY_TYPE.Planned);

  const employees = useEmployees();
  const projects = useProjects();
  const customers = useCustomers();
  const activities = useAllActivities();

  const employeesById = useMemo(() => new Map(employees.data?.items.map((e) => [e.id, e.name])), [employees.data]);
  const projectsById = useMemo(() => new Map(projects.data?.items.map((p) => [p.id, p.name])), [projects.data]);
  const customersById = useMemo(() => new Map(customers.data?.items.map((c) => [c.id, c.name])), [customers.data]);
  const activitiesById = useMemo(() => new Map(activities.data?.items.map((a) => [a.id, a.name])), [activities.data]);

  const mqlFieldValues = useMemo(
    () => ({
      employee: employees.data?.items.map((e) => e.name) ?? [],
      project: projects.data?.items.map((p) => p.name) ?? [],
      customer: customers.data?.items.map((c) => c.name) ?? [],
      activityL1: activities.data?.items.filter((a) => !a.parentActivityId).map((a) => a.name) ?? [],
      activityL2: activities.data?.items.filter((a) => a.parentActivityId).map((a) => a.name) ?? [],
    }),
    [employees.data, projects.data, customers.data, activities.data],
  );

  const resolveDimension = useMemo(() => {
    return (dimension: GroupByDimension, log: EmployeeWorkLogDto) => {
      switch (dimension) {
        case 'employee':
          return { key: log.employeeId, label: employeesById.get(log.employeeId) ?? 'Bilinmeyen kişi' };
        case 'project':
          return { key: log.projectId, label: projectsById.get(log.projectId) ?? 'Bilinmeyen proje' };
        case 'customer':
          return { key: log.customerId, label: customersById.get(log.customerId) ?? 'Bilinmeyen müşteri' };
        case 'activityL1':
          return { key: log.activityL1Id, label: activitiesById.get(log.activityL1Id) ?? 'Bilinmeyen aktivite' };
        case 'activityL2':
          return { key: log.activityL2Id, label: activitiesById.get(log.activityL2Id) ?? 'Bilinmeyen aktivite' };
        default:
          return null;
      }
    };
  }, [employeesById, projectsById, customersById, activitiesById]);

  const filterLogs = (logs: EmployeeWorkLogDto[]) => {
    if (!mqlAst) return logs;
    return logs.filter((log) =>
      evaluateMql(mqlAst, {
        employee: employeesById.get(log.employeeId) ?? '',
        project: projectsById.get(log.projectId) ?? '',
        customer: customersById.get(log.customerId) ?? '',
        activityL1: activitiesById.get(log.activityL1Id) ?? '',
        activityL2: activitiesById.get(log.activityL2Id) ?? '',
        hours: log.hours,
        description: log.description,
        date: log.workDate,
      }),
    );
  };

  const filteredActualLogs = useMemo(
    () => filterLogs(actualLogsQuery.data?.items ?? []),
    [actualLogsQuery.data, mqlAst, employeesById, projectsById, customersById, activitiesById],
  );
  const filteredPlannedLogs = useMemo(
    () => filterLogs(plannedLogsQuery.data?.items ?? []),
    [plannedLogsQuery.data, mqlAst, employeesById, projectsById, customersById, activitiesById],
  );

  const accuracy = useMemo(
    () =>
      groupWorkLogsAccuracy(
        filteredActualLogs,
        filteredPlannedLogs,
        pastColumns,
        groupBy,
        resolveDimension,
        mqlAst ? undefined : employees.data?.items,
      ),
    [filteredActualLogs, filteredPlannedLogs, pastColumns, groupBy, resolveDimension, employees.data, mqlAst],
  );

  const isLoading = actualLogsQuery.isLoading || plannedLogsQuery.isLoading;

  return (
    <div className="flex flex-1 overflow-hidden bg-slate-50">
      <main className="flex flex-1 flex-col overflow-hidden p-6">
        <div className="mb-4 flex shrink-0 flex-wrap items-center gap-3">
          <div className="flex shrink-0 items-center gap-3">
            <MonthNavigator
              anchorDate={anchorDate}
              startKey={periodRange.startKey}
              endKey={periodRange.endKey}
              onPrev={() => {
                if (customRange) setCustomRange((r) => (r ? shiftCustomRange(r, -1) : r));
                else setAnchorDate((d) => navigatePeriod(periodMode, d, -1));
              }}
              onNext={() => {
                if (customRange) setCustomRange((r) => (r ? shiftCustomRange(r, 1) : r));
                else setAnchorDate((d) => navigatePeriod(periodMode, d, 1));
              }}
              onApplyRange={(result) => {
                if (result.kind === 'quick') {
                  setCustomRange(null);
                  setAnchorDate(result.anchor);
                } else {
                  setCustomRange({ startKey: dateKey(result.from), endKey: dateKey(result.to) });
                }
              }}
            />
            <PeriodModeSelect
              value={periodMode}
              onChange={(mode) => {
                setCustomRange(null);
                setPeriodMode(mode);
              }}
            />
          </div>
          <div className="min-w-[16rem] flex-1">
            <MqlFilterInput onApply={setMqlAst} fieldValues={mqlFieldValues} />
          </div>
          <div className="flex shrink-0 items-center gap-3">
            <GroupByMultiSelect value={groupBy} onChange={setGroupBy} />
          </div>
        </div>

        <div className="shrink-0">
          <PlanningAccuracyVarianceWidgets rows={accuracy.rows} columns={pastColumns} />
        </div>

        <div className="shrink-0">
          <PlanningAccuracyLegend />
        </div>

        {isLoading ? (
          <div className="min-h-0 flex-1 rounded-xl border border-slate-200 bg-white p-8 text-center text-slate-400">
            Yükleniyor…
          </div>
        ) : (
          <div className="min-h-0 flex-1">
            <PlanningAccuracyTable
              columns={pastColumns}
              rows={accuracy.rows}
              grandTotalActualByColumn={accuracy.grandTotalActualByColumn}
              grandTotalPlannedByColumn={accuracy.grandTotalPlannedByColumn}
              grandTotalActual={accuracy.grandTotalActual}
              grandTotalPlanned={accuracy.grandTotalPlanned}
            />
          </div>
        )}
      </main>
    </div>
  );
}
