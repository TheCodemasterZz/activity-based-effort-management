import { useMemo, useState } from 'react';
import { ErrorState } from '../components/common/ErrorState';
import { PeriodModeSelect } from '../components/dashboard/PeriodModeSelect';
import { MonthNavigator } from '../components/dashboard/MonthNavigator';
import { GroupByMultiSelect } from '../components/dashboard/GroupByMultiSelect';
import { MqlFilterInput } from '../components/dashboard/MqlFilterInput';
import { SummaryCards } from '../components/dashboard/SummaryCards';
import { TableLegend } from '../components/dashboard/TableLegend';
import { WorkLogTable, type LeaveRange } from '../components/dashboard/WorkLogTable';
import { WorkLogFormModal, type WorkLogFormInitialValues } from '../components/logentry/WorkLogFormModal';
import { CellWorkLogsModal } from '../components/logentry/CellWorkLogsModal';
import { WorkLogApprovalModal } from '../components/dashboard/WorkLogApprovalModal';
import {
  buildCustomDailyRange,
  dateKey,
  eachDateKeyInRange,
  getPeriodRange,
  navigatePeriod,
  shiftCustomRange,
  type CustomRange,
  type PeriodColumn,
  type PeriodMode,
} from '../lib/dateUtils';
import { groupWorkLogs, type GroupByDimension, type GroupedRow } from '../lib/groupWorkLogs';
import { evaluateMql, type MqlNode } from '../lib/mql';
import { pushErrorNotification } from '../lib/notifications';
import { useWorkLogs } from '../hooks/useWorkLogs';
import { useWorkLogApprovals } from '../hooks/useWorkLogApprovals';
import { useEmployeeLeaves } from '../hooks/useEmployeeLeaves';
import { useEmployees } from '../hooks/useEmployees';
import { useProjects } from '../hooks/useProjects';
import { useAllActivities } from '../hooks/useActivities';
import { useHolidays } from '../hooks/useHolidays';
import { WORK_LOG_ENTRY_TYPE, type EmployeeWorkLogDto } from '../api/types';

/**
 * Log Work (ReportPage) ekranının birebir aynısı — sadece EntryType.Planned kayıtlarla çalışır.
 * MQL, Group by, tarih aralığı gezinmesi, Günlük/Haftalık/Aylık gösterim, hücreye tıklayarak
 * ekle/düzenle/sil ve onay akışı tamamen aynı; tek fark her yerde geçirilen `entryType` ve
 * "Plan Ekle" metni. Gerçekleşen (Actual) verilerden backend'de tamamen izole çalışır — bu
 * sayfadaki onay/kayıt işlemleri Log Work'ü hiç etkilemez.
 */
export function PlanWorkPage() {
  const [periodMode, setPeriodMode] = useState<PeriodMode>('daily');
  const [anchorDate, setAnchorDate] = useState(new Date());
  const [customRange, setCustomRange] = useState<CustomRange | null>(null);
  const [groupBy, setGroupBy] = useState<GroupByDimension[]>(['employee']);
  const [mqlAst, setMqlAst] = useState<MqlNode | null>(null);

  const [createModal, setCreateModal] = useState<{ initial: WorkLogFormInitialValues } | null>(null);
  const [isApprovalModalOpen, setIsApprovalModalOpen] = useState(false);
  const [cellModal, setCellModal] = useState<{
    logs: EmployeeWorkLogDto[];
    date: string;
    prefill: WorkLogFormInitialValues;
  } | null>(null);

  const periodRange = useMemo(
    () => (customRange ? buildCustomDailyRange(customRange) : getPeriodRange(periodMode, anchorDate)),
    [periodMode, anchorDate, customRange],
  );
  const workLogs = useWorkLogs(periodRange.startKey, periodRange.endKey, WORK_LOG_ENTRY_TYPE.Planned);
  const workLogApprovals = useWorkLogApprovals(WORK_LOG_ENTRY_TYPE.Planned);
  const employeeLeaves = useEmployeeLeaves();

  const employees = useEmployees();
  const projects = useProjects();
  const activities = useAllActivities();
  const holidays = useHolidays();

  const holidayDateKeys = useMemo(
    () => new Set(holidays.data?.items.map((h) => h.date) ?? []),
    [holidays.data],
  );

  const employeesById = useMemo(() => new Map(employees.data?.items.map((e) => [e.id, e.name])), [employees.data]);
  const projectsById = useMemo(() => new Map(projects.data?.items.map((p) => [p.id, p.name])), [projects.data]);
  const activitiesById = useMemo(() => new Map(activities.data?.items.map((a) => [a.id, a.name])), [activities.data]);

  const approvedRangesByEmployee = useMemo(() => {
    const map = new Map<string, { start: string; end: string }[]>();
    for (const approval of workLogApprovals.data?.items ?? []) {
      const list = map.get(approval.employeeId) ?? [];
      list.push({ start: approval.periodStart, end: approval.periodEnd });
      map.set(approval.employeeId, list);
    }
    return map;
  }, [workLogApprovals.data]);

  const leaveRangesByEmployee = useMemo(() => {
    const map = new Map<string, LeaveRange[]>();
    for (const leave of employeeLeaves.data?.items ?? []) {
      const list = map.get(leave.employeeId) ?? [];
      list.push({ start: leave.startDate, end: leave.endDate, isFullDay: leave.isFullDay });
      map.set(leave.employeeId, list);
    }
    return map;
  }, [employeeLeaves.data]);

  const mqlFieldValues = useMemo(
    () => ({
      employee: employees.data?.items.map((e) => e.name) ?? [],
      project: projects.data?.items.map((p) => p.name) ?? [],
      activityL1: activities.data?.items.filter((a) => !a.parentActivityId).map((a) => a.name) ?? [],
      activityL2: activities.data?.items.filter((a) => a.parentActivityId).map((a) => a.name) ?? [],
    }),
    [employees.data, projects.data, activities.data],
  );

  const resolveDimension = useMemo(() => {
    return (dimension: GroupByDimension, log: EmployeeWorkLogDto) => {
      switch (dimension) {
        case 'employee':
          return { key: log.employeeId, label: employeesById.get(log.employeeId) ?? 'Bilinmeyen kişi' };
        case 'project':
          return { key: log.projectId, label: projectsById.get(log.projectId) ?? 'Bilinmeyen proje' };
        case 'activityL1':
          return { key: log.activityL1Id, label: activitiesById.get(log.activityL1Id) ?? 'Bilinmeyen aktivite' };
        case 'activityL2':
          return { key: log.activityL2Id, label: activitiesById.get(log.activityL2Id) ?? 'Bilinmeyen aktivite' };
        default:
          return null;
      }
    };
  }, [employeesById, projectsById, activitiesById]);

  const logs = workLogs.data?.items ?? [];

  const filteredLogs = useMemo(() => {
    if (!mqlAst) return logs;
    return logs.filter((log) =>
      evaluateMql(mqlAst, {
        employee: employeesById.get(log.employeeId) ?? '',
        project: projectsById.get(log.projectId) ?? '',
        activityL1: activitiesById.get(log.activityL1Id) ?? '',
        activityL2: activitiesById.get(log.activityL2Id) ?? '',
        hours: log.hours,
        description: log.description,
        date: log.workDate,
      }),
    );
  }, [logs, mqlAst, employeesById, projectsById, activitiesById]);

  const grouped = useMemo(
    () =>
      groupWorkLogs(
        filteredLogs,
        periodRange.columns,
        groupBy,
        resolveDimension,
        mqlAst ? undefined : employees.data?.items,
      ),
    [filteredLogs, periodRange.columns, groupBy, resolveDimension, employees.data, mqlAst],
  );

  const totalHours = filteredLogs.reduce((sum, l) => sum + l.hours, 0);
  const approvedHours = filteredLogs.filter((l) => l.isApproved).reduce((sum, l) => sum + l.hours, 0);
  const activePeopleCount = new Set(filteredLogs.map((l) => l.employeeId)).size;
  const avgDailyHours = activePeopleCount > 0 ? totalHours / activePeopleCount : 0;

  const todayKey = new Date().toISOString().slice(0, 10);
  const elapsedColumns = periodRange.columns.filter((c) => c.startKey <= todayKey);
  const trendColumns = (elapsedColumns.length > 0 ? elapsedColumns : periodRange.columns).slice(-10);

  const columnLabel = (c: PeriodColumn) => (c.sublabel ? `${c.label} ${c.sublabel}` : c.label);
  const hoursSeries = trendColumns.map((c) => ({ label: columnLabel(c), value: grouped.grandTotalByColumn[c.key] ?? 0 }));
  const countSeries = trendColumns.map((c) => ({
    label: columnLabel(c),
    value: filteredLogs.filter((l) => l.workDate >= c.startKey && l.workDate <= c.endKey).length,
  }));
  const avgSeries = hoursSeries.map((point) => ({
    label: point.label,
    value: activePeopleCount > 0 ? point.value / activePeopleCount : 0,
  }));
  const approvedHoursSeries = trendColumns.map((c) => ({
    label: columnLabel(c),
    value: filteredLogs
      .filter((l) => l.isApproved && l.workDate >= c.startKey && l.workDate <= c.endKey)
      .reduce((sum, l) => sum + l.hours, 0),
  }));
  const totalEmployeeCount = employees.data?.items.length ?? 0;

  const resolveEmployee = (id: string) => employeesById.get(id) ?? 'Bilinmeyen kişi';
  const resolveProject = (id: string) => projectsById.get(id) ?? 'Bilinmeyen proje';
  const resolveActivity = (id: string) => activitiesById.get(id) ?? 'Bilinmeyen aktivite';

  const buildPrefillFromRow = (row: GroupedRow): WorkLogFormInitialValues => {
    const keys = row.path.split('/').slice(1);
    const prefill: WorkLogFormInitialValues = {};

    groupBy.forEach((dimension, index) => {
      const key = keys[index];
      if (!key) return;

      switch (dimension) {
        case 'employee':
          prefill.employeeId = key;
          prefill.employeeLabel = resolveEmployee(key);
          break;
        case 'project':
          prefill.projectId = key;
          prefill.projectLabel = resolveProject(key);
          break;
        case 'activityL1':
          prefill.activityL1Id = key;
          break;
        case 'activityL2':
          prefill.activityL2Id = key;
          break;
      }
    });

    return prefill;
  };

  const isRangeApprovedForRow = (row: GroupedRow, startKey: string, endKey: string): boolean => {
    if (!row.employeeId) return false;
    const ranges = approvedRangesByEmployee.get(row.employeeId);
    if (!ranges || ranges.length === 0) return false;
    return eachDateKeyInRange(startKey, endKey).some((d) => ranges.some((r) => d >= r.start && d <= r.end));
  };

  const handleCellClick = (row: GroupedRow, column: PeriodColumn) => {
    const cellLogs = row.cellLogs[column.key] ?? [];
    const prefill = buildPrefillFromRow(row);

    if (cellLogs.length > 0) {
      setCellModal({ logs: cellLogs, date: column.startKey, prefill });
      return;
    }

    if (isRangeApprovedForRow(row, column.startKey, column.endKey)) {
      pushErrorNotification('Bu gün onaylı bir plan dönemine denk geliyor, yeni kayıt eklenemez.');
      return;
    }

    setCreateModal({ initial: { ...prefill, date: column.startKey } });
  };

  const handleRangeSelect = (row: GroupedRow, startColumn: PeriodColumn, endColumn: PeriodColumn) => {
    if (isRangeApprovedForRow(row, startColumn.startKey, endColumn.endKey)) {
      pushErrorNotification('Seçilen aralığın bir kısmı onaylı bir plan dönemine denk geliyor, yeni kayıt eklenemez.');
      return;
    }

    const prefill = buildPrefillFromRow(row);
    setCreateModal({ initial: { ...prefill, date: startColumn.startKey, endDate: endColumn.endKey } });
  };

  const isCreateModalRange = !!(
    createModal &&
    (!createModal.initial.date ||
      (createModal.initial.endDate && createModal.initial.endDate !== createModal.initial.date))
  );

  return (
    <div className="flex flex-1 overflow-hidden bg-slate-50">
      <main className="flex-1 overflow-y-auto p-6">
        <div className="mb-4 flex flex-wrap items-center gap-3">
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
            <button
              type="button"
              onClick={() => setIsApprovalModalOpen(true)}
              className="rounded-lg border border-emerald-200 bg-emerald-50 px-4 py-2 text-sm font-semibold text-emerald-700 hover:bg-emerald-100"
            >
              🔒 Onayla
            </button>
            <button
              type="button"
              onClick={() => setCreateModal({ initial: {} })}
              className="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-700"
            >
              + Plan Ekle
            </button>
          </div>
        </div>

        <div className="mb-4">
          <SummaryCards
            totalHours={totalHours}
            totalCount={filteredLogs.length}
            activePeopleCount={activePeopleCount}
            totalEmployeeCount={totalEmployeeCount}
            avgDailyHours={avgDailyHours}
            approvedHours={approvedHours}
            periodLabel={periodRange.label}
            hoursSeries={hoursSeries}
            countSeries={countSeries}
            avgSeries={avgSeries}
            approvedHoursSeries={approvedHoursSeries}
          />
        </div>

        <TableLegend />

        {workLogs.isLoading ? (
          <div className="rounded-xl border border-slate-200 bg-white p-8 text-center text-slate-400">
            Yükleniyor…
          </div>
        ) : workLogs.isError ? (
          <ErrorState />
        ) : (
          <WorkLogTable
            columns={periodRange.columns}
            rows={grouped.rows}
            grandTotalByColumn={grouped.grandTotalByColumn}
            grandTotal={grouped.grandTotal}
            holidayDateKeys={holidayDateKeys}
            approvedRangesByEmployee={approvedRangesByEmployee}
            leaveRangesByEmployee={leaveRangesByEmployee}
            onCellClick={handleCellClick}
            onRangeSelect={handleRangeSelect}
          />
        )}
      </main>

      {createModal && (
        <WorkLogFormModal
          mode="create"
          allowDateRange={isCreateModalRange}
          initialValues={createModal.initial}
          entryType={WORK_LOG_ENTRY_TYPE.Planned}
          onClose={() => setCreateModal(null)}
        />
      )}

      {isApprovalModalOpen && (
        <WorkLogApprovalModal
          onClose={() => setIsApprovalModalOpen(false)}
          resolveProject={resolveProject}
          resolveActivity={resolveActivity}
          entryType={WORK_LOG_ENTRY_TYPE.Planned}
        />
      )}

      {cellModal && (
        <CellWorkLogsModal
          logs={cellModal.logs}
          date={cellModal.date}
          resolveEmployee={resolveEmployee}
          resolveProject={resolveProject}
          resolveActivity={resolveActivity}
          addPrefill={cellModal.prefill}
          entryType={WORK_LOG_ENTRY_TYPE.Planned}
          onClose={() => setCellModal(null)}
        />
      )}
    </div>
  );
}
