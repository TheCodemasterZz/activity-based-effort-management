import { useMemo, useState } from 'react';
import { PeriodModeSelect } from '../components/dashboard/PeriodModeSelect';
import { MonthNavigator } from '../components/dashboard/MonthNavigator';
import { GroupByMultiSelect } from '../components/dashboard/GroupByMultiSelect';
import { MqlFilterInput } from '../components/dashboard/MqlFilterInput';
import { SummaryCards } from '../components/dashboard/SummaryCards';
import { WorkLogTable } from '../components/dashboard/WorkLogTable';
import { WorkLogFormModal, type WorkLogFormInitialValues } from '../components/logentry/WorkLogFormModal';
import { CellWorkLogsModal } from '../components/logentry/CellWorkLogsModal';
import { WorkLogApprovalModal } from '../components/dashboard/WorkLogApprovalModal';
import {
  buildCustomDailyRange,
  dateKey,
  getPeriodRange,
  navigatePeriod,
  shiftCustomRange,
  type CustomRange,
  type PeriodColumn,
  type PeriodMode,
} from '../lib/dateUtils';
import { groupWorkLogs, type GroupByDimension, type GroupedRow } from '../lib/groupWorkLogs';
import { evaluateMql, type MqlNode } from '../lib/mql';
import { useWorkLogs } from '../hooks/useWorkLogs';
import { useWorkLogApprovals } from '../hooks/useWorkLogApprovals';
import { useEmployees } from '../hooks/useEmployees';
import { useProjects } from '../hooks/useProjects';
import { useCustomers } from '../hooks/useCustomers';
import { useAllActivities } from '../hooks/useActivities';
import { useHolidays } from '../hooks/useHolidays';
import type { EmployeeWorkLogDto } from '../api/types';

export function ReportPage() {
  const [periodMode, setPeriodMode] = useState<PeriodMode>('daily');
  const [anchorDate, setAnchorDate] = useState(new Date());
  // Tarih aralığı seçiciden elle bir [from, to] seçildiğinde dolar — doluyken periyot modundan
  // bağımsız olarak birebir o günleri gösterir; period tab'larından biri seçilince temizlenir.
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
  const workLogs = useWorkLogs(periodRange.startKey, periodRange.endKey);
  const workLogApprovals = useWorkLogApprovals();

  const employees = useEmployees();
  const projects = useProjects();
  const customers = useCustomers();
  const activities = useAllActivities();
  const holidays = useHolidays();

  const holidayDateKeys = useMemo(
    () => new Set(holidays.data?.items.map((h) => h.date) ?? []),
    [holidays.data],
  );

  const employeesById = useMemo(() => new Map(employees.data?.items.map((e) => [e.id, e.name])), [employees.data]);
  const projectsById = useMemo(() => new Map(projects.data?.items.map((p) => [p.id, p.name])), [projects.data]);
  const customersById = useMemo(() => new Map(customers.data?.items.map((c) => [c.id, c.name])), [customers.data]);
  const activitiesById = useMemo(() => new Map(activities.data?.items.map((a) => [a.id, a.name])), [activities.data]);

  // Çalışan bazlı onaylı [start,end] dönemleri — tabloda kaydı olmayan ama onaylı bir haftaya
  // denk gelen boş günleri de doğru renklendirebilmek için (bkz. WorkLogTable.cellApprovalStatus).
  const approvedRangesByEmployee = useMemo(() => {
    const map = new Map<string, { start: string; end: string }[]>();
    for (const approval of workLogApprovals.data?.items ?? []) {
      const list = map.get(approval.employeeId) ?? [];
      list.push({ start: approval.periodStart, end: approval.periodEnd });
      map.set(approval.employeeId, list);
    }
    return map;
  }, [workLogApprovals.data]);

  // MQL otomatik tamamlama için alan bazlı bilinen değerler — mevcut work log'larla sınırlı
  // değil, tüm çalışan/proje/müşteri/aktivite kataloğunu kapsar.
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

  const logs = workLogs.data?.items ?? [];

  // MQL (Mesainâme Query Language) — JQL mantığında serbest metin sorgusu; tanımlıysa
  // tablonun ve tüm özet hesaplamaların girdisi bu filtrelenmiş alt küme olur.
  const filteredLogs = useMemo(() => {
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
  }, [logs, mqlAst, employeesById, projectsById, customersById, activitiesById]);

  const grouped = useMemo(
    () => groupWorkLogs(filteredLogs, periodRange.columns, groupBy, resolveDimension),
    [filteredLogs, periodRange.columns, groupBy, resolveDimension],
  );

  const totalHours = filteredLogs.reduce((sum, l) => sum + l.hours, 0);
  const approvedHours = filteredLogs.filter((l) => l.isApproved).reduce((sum, l) => sum + l.hours, 0);
  const activePeopleCount = new Set(filteredLogs.map((l) => l.employeeId)).size;
  const avgDailyHours = activePeopleCount > 0 ? totalHours / activePeopleCount : 0;

  // Widget trend grafikleri sadece bugüne kadar geçmiş olan sütunları kullanır — aksi halde
  // henüz gelmemiş (ileri tarihli) sıfır dolgulu sütunlar grafiği anlamsızca düzleştirir.
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
  const resolveCustomer = (id: string) => customersById.get(id) ?? 'Bilinmeyen müşteri';
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
        case 'customer':
          prefill.customerId = key;
          prefill.customerLabel = resolveCustomer(key);
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

  const handleCellClick = (row: GroupedRow, column: PeriodColumn) => {
    const cellLogs = row.cellLogs[column.key] ?? [];
    const prefill = buildPrefillFromRow(row);

    if (cellLogs.length > 0) {
      setCellModal({ logs: cellLogs, date: column.startKey, prefill });
    } else {
      setCreateModal({ initial: { ...prefill, date: column.startKey } });
    }
  };

  const handleRangeSelect = (row: GroupedRow, startColumn: PeriodColumn, endColumn: PeriodColumn) => {
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
              + Work Log Ekle
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

        {workLogs.isLoading ? (
          <div className="rounded-xl border border-slate-200 bg-white p-8 text-center text-slate-400">
            Yükleniyor…
          </div>
        ) : workLogs.isError ? (
          <div className="flex items-center gap-3 rounded-xl border border-red-200 bg-red-50 p-6 text-red-700">
            <span className="text-xl">⚠</span>
            <div>
              <div className="font-semibold">Veriler yüklenemedi</div>
              <div className="text-sm text-red-600">
                Sunucudan yanıt alınamadı. Bağlantınızı kontrol edip tekrar deneyin.
              </div>
            </div>
          </div>
        ) : (
          <WorkLogTable
            columns={periodRange.columns}
            rows={grouped.rows}
            grandTotalByColumn={grouped.grandTotalByColumn}
            grandTotal={grouped.grandTotal}
            holidayDateKeys={holidayDateKeys}
            approvedRangesByEmployee={approvedRangesByEmployee}
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
          onClose={() => setCreateModal(null)}
        />
      )}

      {isApprovalModalOpen && (
        <WorkLogApprovalModal onClose={() => setIsApprovalModalOpen(false)} />
      )}

      {cellModal && (
        <CellWorkLogsModal
          logs={cellModal.logs}
          date={cellModal.date}
          resolveEmployee={resolveEmployee}
          resolveProject={resolveProject}
          resolveCustomer={resolveCustomer}
          resolveActivity={resolveActivity}
          addPrefill={cellModal.prefill}
          onClose={() => setCellModal(null)}
        />
      )}
    </div>
  );
}
