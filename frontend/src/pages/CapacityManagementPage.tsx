import { type CSSProperties, useEffect, useMemo, useRef, useState } from 'react';
import { useQueries } from '@tanstack/react-query';
import { PeriodModeSelect } from '../components/dashboard/PeriodModeSelect';
import { MonthNavigator } from '../components/dashboard/MonthNavigator';
import { MqlFilterInput } from '../components/dashboard/MqlFilterInput';
import { CapacityChart, type CapacityChartPoint, type CapacityChartView } from '../components/dashboard/charts/CapacityChart';
import {
  buildCustomDailyRange,
  dateKey,
  eachDateKeyInRange,
  getPeriodRange,
  navigatePeriod,
  shiftCustomRange,
  type CustomRange,
  type PeriodMode,
} from '../lib/dateUtils';
import { evaluateMql, type MqlNode } from '../lib/mql';
import { useWorkLogs } from '../hooks/useWorkLogs';
import { useUserRoster } from '../hooks/useUserRoster';
import { useLeaves } from '../hooks/useLeaves';
import { useProjects } from '../hooks/useProjects';
import { useAllActivities } from '../hooks/useActivities';
import { useHolidays } from '../hooks/useHolidays';
import { getWorkCalendarById } from '../api/workCalendars';
import { WORK_LOG_ENTRY_TYPE, type WorkLogDto, type WorkCalendarDetailDto } from '../api/types';
import type { UserRosterEntry } from '../hooks/useUserRoster';
import type { LeaveDto } from '../api/leaves';

type WorkloadMode = 'mixed' | 'actual' | 'planned';
type CellStatus = 'over' | 'full' | 'free' | 'none';

interface ColumnStat {
  capacityHours: number;
  actualHours: number;
  plannedHours: number;
  workloadHours: number;
  timeOffHours: number;
}

function toMinutes(time: string): number {
  const [h, m] = time.split(':').map(Number);
  return h * 60 + m;
}

function calendarDayHours(calendar: WorkCalendarDetailDto | undefined, dayOfWeek: number): number {
  const day = calendar?.days.find((d) => d.dayOfWeek === dayOfWeek);
  if (!day?.isWorkingDay || !day.startTime || !day.endTime) return 0;
  return (toMinutes(day.endTime) - toMinutes(day.startTime)) / 60;
}

function buildHoursByUserDate(logs: WorkLogDto[]): Map<string, Map<string, number>> {
  const map = new Map<string, Map<string, number>>();
  for (const log of logs) {
    const byDate = map.get(log.userId) ?? new Map<string, number>();
    byDate.set(log.workDate, (byDate.get(log.workDate) ?? 0) + log.hours);
    map.set(log.userId, byDate);
  }
  return map;
}

function buildLeaveHoursByUserDate(
  leaves: LeaveDto[],
  employeesList: UserRosterEntry[],
  calendarsById: Map<string, WorkCalendarDetailDto>,
): Map<string, Map<string, number>> {
  const calendarByUser = new Map(
    employeesList.map((e) => [e.id, e.workCalendarId ? calendarsById.get(e.workCalendarId) : undefined]),
  );
  const map = new Map<string, Map<string, number>>();
  for (const leave of leaves) {
    const calendar = calendarByUser.get(leave.userId);
    for (const day of eachDateKeyInRange(leave.startDate, leave.endDate)) {
      const dayOfWeek = new Date(`${day}T00:00:00`).getDay();
      const hours = leave.isFullDay
        ? calendarDayHours(calendar, dayOfWeek)
        : leave.startTime && leave.endTime
          ? (toMinutes(leave.endTime) - toMinutes(leave.startTime)) / 60
          : 0;
      const byDate = map.get(leave.userId) ?? new Map<string, number>();
      byDate.set(day, (byDate.get(day) ?? 0) + hours);
      map.set(leave.userId, byDate);
    }
  }
  return map;
}

function computeDayCapacity(
  employee: UserRosterEntry,
  dateKeyStr: string,
  calendarsById: Map<string, WorkCalendarDetailDto>,
  holidayDateKeys: Set<string>,
  leaveHoursByUserDate: Map<string, Map<string, number>>,
): number {
  if (holidayDateKeys.has(dateKeyStr)) return 0;
  // Takvimsiz kullanıcının beklenen kapasitesi bilinemez — 0 sayılır (hücre 'none'),
  // sayfa üstündeki uyarı bandı bu kullanıcıları ayrıca listeler.
  const calendar = employee.workCalendarId ? calendarsById.get(employee.workCalendarId) : undefined;
  const dayOfWeek = new Date(`${dateKeyStr}T00:00:00`).getDay();
  const raw = calendarDayHours(calendar, dayOfWeek);
  const leaveHours = leaveHoursByUserDate.get(employee.id)?.get(dateKeyStr) ?? 0;
  return Math.max(0, raw - leaveHours);
}

function computeCellStatus(workloadHours: number, capacityHours: number): { status: CellStatus; hours: number } {
  if (capacityHours <= 0.01) return { status: 'none', hours: 0 };
  const free = capacityHours - workloadHours;
  if (free < -0.05) return { status: 'over', hours: -free };
  if (free <= capacityHours * 0.1 + 0.05) return { status: 'full', hours: 0 };
  return { status: 'free', hours: free };
}

function formatHours(value: number): string {
  return `${value % 1 === 0 ? value : value.toFixed(1)}h`;
}

const TIME_OFF_STRIPE_STYLE: CSSProperties = {
  backgroundImage:
    'repeating-linear-gradient(45deg, rgba(139,92,246,0.32) 0px, rgba(139,92,246,0.32) 2px, transparent 2px, transparent 9px)',
};

function CapacityCell({ stat }: { stat: ColumnStat }) {
  const { status, hours } = computeCellStatus(stat.workloadHours, stat.capacityHours);
  const style = stat.timeOffHours > 0 ? TIME_OFF_STRIPE_STYLE : undefined;
  const title =
    status === 'none'
      ? 'Bu dönemde çalışma günü yok (hafta sonu/tatil/tam izinli)'
      : `Kapasite: ${formatHours(stat.capacityHours)} · İş yükü: ${formatHours(stat.workloadHours)}` +
        (stat.timeOffHours > 0 ? ` · İzin: ${formatHours(stat.timeOffHours)}` : '');

  if (status === 'none') {
    return (
      <div className="rounded-md bg-slate-50 px-2 py-2 text-center text-[11px] text-slate-300" style={style} title={title}>
        —
      </div>
    );
  }
  if (status === 'over') {
    return (
      <div
        className="rounded-md border-t-4 border-red-500 bg-slate-900 px-2 py-1.5 text-center text-[11px] font-semibold text-white"
        style={style}
        title={title}
      >
        {formatHours(hours)} aşım
      </div>
    );
  }
  if (status === 'full') {
    return (
      <div className="rounded-md bg-indigo-500 px-2 py-1.5 text-center text-[11px] font-semibold text-white" style={style} title={title}>
        Dolu
      </div>
    );
  }

  const ratio = stat.capacityHours > 0 ? hours / stat.capacityHours : 0;
  const freeClass = ratio > 0.66 ? 'bg-emerald-50 text-emerald-600' : ratio > 0.33 ? 'bg-emerald-100 text-emerald-700' : 'bg-emerald-200 text-emerald-800';

  return (
    <div className={`rounded-md px-2 py-1.5 text-center text-[11px] font-semibold ${freeClass}`} style={style} title={title}>
      {formatHours(hours)} boşta
    </div>
  );
}

const LEGEND_ITEMS: { swatchClass: string; swatchStyle?: CSSProperties; label: string }[] = [
  { swatchClass: 'bg-emerald-100 border border-emerald-300', label: 'Boşta (açık ton = daha fazla boş kapasite)' },
  { swatchClass: 'bg-indigo-500', label: 'Dolu' },
  { swatchClass: 'bg-slate-900 border-t-2 border-red-500', label: 'Aşım (kapasite üzeri)' },
  { swatchClass: 'bg-slate-50 border border-slate-200', label: 'Kapasite yok (hafta sonu/tatil)' },
  { swatchClass: 'bg-violet-100 border border-violet-300', swatchStyle: TIME_OFF_STRIPE_STYLE, label: 'İzinli gün (çizgili doku)' },
];

function CapacityLegend() {
  return (
    <div className="mb-3 flex flex-wrap items-center gap-x-6 gap-y-2 text-sm font-medium text-slate-600">
      {LEGEND_ITEMS.map((item) => (
        <div key={item.label} className="flex items-center gap-2">
          <span className={`h-4 w-4 shrink-0 rounded ${item.swatchClass}`} style={item.swatchStyle} />
          <span>{item.label}</span>
        </div>
      ))}
    </div>
  );
}

const WORKLOAD_MODE_OPTIONS: { value: WorkloadMode; label: string }[] = [
  { value: 'mixed', label: 'Karma (geçmiş: Gerçekleşen, gelecek: Planlanan)' },
  { value: 'actual', label: 'Sadece Gerçekleşen' },
  { value: 'planned', label: 'Sadece Planlanan' },
];

const CHART_VIEW_OPTIONS: { value: CapacityChartView; label: string }[] = [
  { value: 'capacity', label: 'Kapasite' },
  { value: 'availability', label: 'Müsaitlik' },
  { value: 'utilization', label: 'Kullanım' },
  { value: 'combined', label: 'Hepsi' },
];

// Grafiğin renk/çizgi dilini tek bakışta anlaşılır kılmak için görünüme göre değişen kısa açıklama.
const CHART_VIEW_CAPTIONS: Record<CapacityChartView, string> = {
  capacity:
    'Barlar: iş yükü (koyu = Gerçekleşen, açık = Planlanan, üst üste yığılı) · Mor çizgi: dönemin kapasite tavanı.',
  availability: 'Yeşil alan: Kapasite − iş yükü, yani o dönemde kalan boş kapasite.',
  utilization: 'Mor çizgi: (Gerçekleşen + Planlanan) / Kapasite × 100 · Kırmızı kesikli çizgi: %100 referansı.',
  combined:
    'Barlar: iş yükü (koyu = Gerçekleşen, açık = Planlanan) · Mor düz çizgi: Kapasite tavanı · Yeşil kesikli çizgi: kalan boş kapasite (Kapasite − iş yükü).',
};

/**
 * Plan Work (Planlanan) ve Log Work (Gerçekleşen) verilerini birlikte kullanarak, her çalışan
 * için seçili dönemdeki kapasite (mesai takvimi - izin) karşısında ne kadar dolu/boş/aşımda
 * olduğunu gösteren salt-okunur bir kaynak-kapasite görünümü. Group by ve onay bilerek yok —
 * bu ekranın amacı düzenleme değil, genel tabloyu görmek.
 */
export function CapacityManagementPage() {
  const [periodMode, setPeriodMode] = useState<PeriodMode>('daily');
  const [anchorDate, setAnchorDate] = useState(new Date());
  const [customRange, setCustomRange] = useState<CustomRange | null>(null);
  const [mqlAst, setMqlAst] = useState<MqlNode | null>(null);
  const [workloadMode, setWorkloadMode] = useState<WorkloadMode>('mixed');
  const [chartView, setChartView] = useState<CapacityChartView>('combined');
  const [nameSort, setNameSort] = useState<'asc' | 'desc'>('asc');

  // Tablo ile alttaki toplam kapasite grafiğinin sütunları aynı dikey çizgide hizalı kalsın diye
  // grafik, tablonun İsim ve ilk dönem sütununun GERÇEKTEN render edilen piksel genişliklerini
  // (min-w-[...] sadece bir alt sınır — hücre içeriği/border'lar bunu büyütebilir) ölçüp aynı
  // genişlikleri kullanır; yatay scroll konumları da karşılıklı senkronize edilir.
  const tableScrollRef = useRef<HTMLDivElement>(null);
  const chartScrollRef = useRef<HTMLDivElement>(null);
  const nameThRef = useRef<HTMLTableCellElement>(null);
  const dataThRef = useRef<HTMLTableCellElement>(null);
  const isSyncingScrollRef = useRef(false);
  const CHART_Y_AXIS_PX = 40;
  const [measuredCols, setMeasuredCols] = useState({ name: 256, column: 96 });

  useEffect(() => {
    const measure = () => {
      const name = nameThRef.current?.getBoundingClientRect().width;
      const column = dataThRef.current?.getBoundingClientRect().width;
      if (name && column) {
        setMeasuredCols((prev) => (prev.name === name && prev.column === column ? prev : { name, column }));
      }
    };
    measure();
    const observer = new ResizeObserver(measure);
    if (nameThRef.current) observer.observe(nameThRef.current);
    if (dataThRef.current) observer.observe(dataThRef.current);
    return () => observer.disconnect();
  });

  const syncScrollFrom = (source: 'table' | 'chart') => (e: React.UIEvent<HTMLDivElement>) => {
    if (isSyncingScrollRef.current) return;
    const target = source === 'table' ? chartScrollRef.current : tableScrollRef.current;
    if (!target) return;
    isSyncingScrollRef.current = true;
    target.scrollLeft = e.currentTarget.scrollLeft;
    isSyncingScrollRef.current = false;
  };

  const periodRange = useMemo(
    () => (customRange ? buildCustomDailyRange(customRange) : getPeriodRange(periodMode, anchorDate)),
    [periodMode, anchorDate, customRange],
  );

  const employees = useUserRoster();
  const actualLogs = useWorkLogs(periodRange.startKey, periodRange.endKey, WORK_LOG_ENTRY_TYPE.Actual);
  const plannedLogs = useWorkLogs(periodRange.startKey, periodRange.endKey, WORK_LOG_ENTRY_TYPE.Planned);
  const leaves = useLeaves();
  const holidays = useHolidays();
  const projects = useProjects();
  const activities = useAllActivities();

  const employeesById = useMemo(() => new Map(employees.data?.items.map((e) => [e.id, e.name])), [employees.data]);
  const projectsById = useMemo(() => new Map(projects.data?.items.map((p) => [p.id, p.name])), [projects.data]);
  const activitiesById = useMemo(() => new Map(activities.data?.items.map((a) => [a.id, a.name])), [activities.data]);

  const mqlFieldValues = useMemo(
    () => ({
      employee: employees.data?.items.map((e) => e.name) ?? [],
      project: projects.data?.items.map((p) => p.name) ?? [],
      activityL1: activities.data?.items.filter((a) => !a.parentActivityId).map((a) => a.name) ?? [],
      activityL2: activities.data?.items.filter((a) => a.parentActivityId).map((a) => a.name) ?? [],
    }),
    [employees.data, projects.data, activities.data],
  );

  const filterLogs = (logs: WorkLogDto[]) => {
    if (!mqlAst) return logs;
    return logs.filter((log) =>
      evaluateMql(mqlAst, {
        employee: employeesById.get(log.userId) ?? '',
        project: projectsById.get(log.projectId) ?? '',
        activityL1: activitiesById.get(log.activityL1Id) ?? '',
        activityL2: activitiesById.get(log.activityL2Id) ?? '',
        hours: log.hours,
        description: log.description,
        date: log.workDate,
      }),
    );
  };

  const filteredActualLogs = useMemo(
    () => filterLogs(actualLogs.data?.items ?? []),
    [actualLogs.data, mqlAst, employeesById, projectsById, activitiesById],
  );
  const filteredPlannedLogs = useMemo(
    () => filterLogs(plannedLogs.data?.items ?? []),
    [plannedLogs.data, mqlAst, employeesById, projectsById, activitiesById],
  );

  const holidayDateKeys = useMemo(() => new Set(holidays.data?.items.map((h) => h.date) ?? []), [holidays.data]);

  const calendarIds = useMemo(
    () => [
      ...new Set(
        (employees.data?.items ?? [])
          .map((e) => e.workCalendarId)
          .filter((id): id is string => id !== null),
      ),
    ],
    [employees.data],
  );

  const usersWithoutCalendar = useMemo(
    () => (employees.data?.items ?? []).filter((e) => e.workCalendarId === null),
    [employees.data],
  );
  const calendarQueries = useQueries({
    queries: calendarIds.map((id) => ({
      queryKey: ['workCalendars', id],
      queryFn: () => getWorkCalendarById(id),
    })),
  });
  const calendarsById = useMemo(() => {
    const map = new Map<string, WorkCalendarDetailDto>();
    calendarIds.forEach((id, index) => {
      const data = calendarQueries[index]?.data;
      if (data) map.set(id, data);
    });
    return map;
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [calendarIds, calendarQueries.map((q) => q.dataUpdatedAt).join(',')]);

  const actualHoursByUserDate = useMemo(() => buildHoursByUserDate(filteredActualLogs), [filteredActualLogs]);
  const plannedHoursByUserDate = useMemo(() => buildHoursByUserDate(filteredPlannedLogs), [filteredPlannedLogs]);
  const leaveHoursByUserDate = useMemo(
    () => buildLeaveHoursByUserDate(leaves.data?.items ?? [], employees.data?.items ?? [], calendarsById),
    [leaves.data, employees.data, calendarsById],
  );

  // MQL aktifken tablo/grafik sadece filtreye uyan en az bir Actual/Planned kaydı olan
  // çalışanları göstermeli — aksi halde (ReportPage'de daha önce düzeltilen aynı hata) tüm kadro
  // hep gösterilmeye devam eder ve MQL sadece hücre içindeki saatleri etkiler, kişi/toplamları
  // etkilemez; "Toplam Kapasite Görünümü" de filtre ne olursa olsun tüm şirketi toplar.
  const matchingUserIds = useMemo(() => {
    if (!mqlAst) return null;
    const ids = new Set<string>();
    for (const log of filteredActualLogs) ids.add(log.userId);
    for (const log of filteredPlannedLogs) ids.add(log.userId);
    return ids;
  }, [mqlAst, filteredActualLogs, filteredPlannedLogs]);

  const sortedUsers = useMemo(
    () =>
      (employees.data?.items ?? [])
        .filter((e) => !matchingUserIds || matchingUserIds.has(e.id))
        .sort((a, b) => (nameSort === 'asc' ? a.name.localeCompare(b.name, 'tr') : b.name.localeCompare(a.name, 'tr'))),
    [employees.data, nameSort, matchingUserIds],
  );

  const todayKey = dateKey(new Date());

  const userColumnStats = useMemo(() => {
    const result = new Map<string, Map<string, ColumnStat>>();
    for (const employee of sortedUsers) {
      const columnMap = new Map<string, ColumnStat>();
      for (const column of periodRange.columns) {
        const days = eachDateKeyInRange(column.startKey, column.endKey);
        let capacityHours = 0;
        let actualHours = 0;
        let plannedHours = 0;
        let workloadHours = 0;
        let timeOffHours = 0;

        for (const day of days) {
          capacityHours += computeDayCapacity(employee, day, calendarsById, holidayDateKeys, leaveHoursByUserDate);
          const dayActual = actualHoursByUserDate.get(employee.id)?.get(day) ?? 0;
          const dayPlanned = plannedHoursByUserDate.get(employee.id)?.get(day) ?? 0;
          actualHours += dayActual;
          plannedHours += dayPlanned;
          timeOffHours += leaveHoursByUserDate.get(employee.id)?.get(day) ?? 0;

          if (workloadMode === 'actual') workloadHours += dayActual;
          else if (workloadMode === 'planned') workloadHours += dayPlanned;
          else workloadHours += day <= todayKey ? dayActual : dayPlanned;
        }

        columnMap.set(column.key, { capacityHours, actualHours, plannedHours, workloadHours, timeOffHours });
      }
      result.set(employee.id, columnMap);
    }
    return result;
  }, [
    sortedUsers,
    periodRange.columns,
    calendarsById,
    holidayDateKeys,
    leaveHoursByUserDate,
    actualHoursByUserDate,
    plannedHoursByUserDate,
    workloadMode,
    todayKey,
  ]);

  const chartData = useMemo<CapacityChartPoint[]>(
    () =>
      periodRange.columns.map((column) => {
        let actualHours = 0;
        let plannedHours = 0;
        let capacityHours = 0;
        let timeOffHours = 0;
        for (const employee of sortedUsers) {
          const stat = userColumnStats.get(employee.id)?.get(column.key);
          if (!stat) continue;
          actualHours += stat.actualHours;
          plannedHours += stat.plannedHours;
          capacityHours += stat.capacityHours;
          timeOffHours += stat.timeOffHours;
        }
        return {
          label: column.sublabel ? `${column.label} ${column.sublabel}` : column.label,
          actualHours,
          plannedHours,
          capacityHours,
          timeOffHours,
        };
      }),
    [periodRange.columns, sortedUsers, userColumnStats],
  );

  return (
    <div className="flex flex-1 overflow-hidden bg-slate-50">
      <main className="flex flex-1 flex-col overflow-hidden p-6">
        {usersWithoutCalendar.length > 0 && (
          <div className="mb-3 shrink-0 rounded-md border border-amber-300 bg-amber-50 px-3 py-2 text-sm text-amber-800">
            {usersWithoutCalendar.length} kullanıcının mesai takvimi atanmamış — kapasiteleri 0 sayıldı.
            Yönetim &gt; Kullanıcılar ekranından takvim atayabilirsiniz.
          </div>
        )}
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
          <select
            value={workloadMode}
            onChange={(e) => setWorkloadMode(e.target.value as WorkloadMode)}
            className="shrink-0 rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm font-medium text-slate-600"
          >
            {WORKLOAD_MODE_OPTIONS.map((opt) => (
              <option key={opt.value} value={opt.value}>
                {opt.label}
              </option>
            ))}
          </select>
        </div>

        <div className="shrink-0">
          <CapacityLegend />
        </div>

        {employees.isLoading || actualLogs.isLoading || plannedLogs.isLoading ? (
          <div className="mt-3 min-h-0 flex-1 rounded-xl border border-slate-200 bg-white p-8 text-center text-slate-400">
            Yükleniyor…
          </div>
        ) : (
          <div
            ref={tableScrollRef}
            onScroll={syncScrollFrom('table')}
            className="mt-3 min-h-0 flex-1 overflow-auto rounded-xl border border-slate-200 bg-white"
          >
            <table className="min-w-full border-collapse text-sm">
              <thead>
                <tr className="border-b border-slate-200 bg-slate-50">
                  <th
                    ref={nameThRef}
                    className="sticky left-0 top-0 z-30 min-w-[16rem] border-r border-b border-slate-200 bg-slate-50 px-3 py-2 text-left font-semibold text-slate-500"
                  >
                    <button
                      type="button"
                      onClick={() => setNameSort((prev) => (prev === 'asc' ? 'desc' : 'asc'))}
                      className="flex items-center gap-1.5 hover:text-slate-700"
                      title="İsme göre sırala"
                    >
                      <span>İsim</span>
                      <span className="text-base leading-none font-bold text-indigo-600">
                        {nameSort === 'asc' ? '▲' : '▼'}
                      </span>
                    </button>
                  </th>
                  {periodRange.columns.map((column, index) => (
                    <th
                      key={column.key}
                      ref={index === 0 ? dataThRef : undefined}
                      className="sticky top-0 z-20 min-w-[6rem] border-r border-b border-slate-200 bg-slate-50 px-2 py-2 text-center font-semibold text-slate-500"
                    >
                      <div>{column.label}</div>
                      {column.sublabel && <div className="text-[10px] font-normal text-slate-400">{column.sublabel}</div>}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {sortedUsers.map((employee) => (
                  <tr key={employee.id} className="border-b border-slate-200 last:border-0 hover:bg-slate-50">
                    <td className="sticky left-0 z-10 border-r border-slate-200 bg-white px-3 py-2 font-medium text-slate-700">
                      {employee.name}
                    </td>
                    {periodRange.columns.map((column) => {
                      const stat = userColumnStats.get(employee.id)?.get(column.key);
                      return (
                        <td key={column.key} className="border-r border-slate-200 p-1">
                          {stat && <CapacityCell stat={stat} />}
                        </td>
                      );
                    })}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        <div className="mt-3 shrink-0 overflow-hidden rounded-xl border border-slate-200 bg-white pt-3 pb-1">
          <div className="mb-2 flex items-center justify-between px-4">
            <h2 className="text-sm font-semibold text-slate-700">Toplam Kapasite Görünümü</h2>
            <div className="flex gap-1.5">
              {CHART_VIEW_OPTIONS.map((opt) => (
                <button
                  key={opt.value}
                  type="button"
                  onClick={() => setChartView(opt.value)}
                  className={
                    'rounded-lg border px-3 py-1.5 text-xs font-medium ' +
                    (chartView === opt.value
                      ? 'border-indigo-600 bg-indigo-600 text-white'
                      : 'border-slate-200 text-slate-600 hover:bg-slate-50')
                  }
                >
                  {opt.label}
                </button>
              ))}
            </div>
          </div>
          <p className="mb-2 px-4 text-xs text-slate-400">{CHART_VIEW_CAPTIONS[chartView]}</p>
          <div ref={chartScrollRef} onScroll={syncScrollFrom('chart')} className="overflow-x-auto overflow-y-hidden">
            <div className="flex" style={{ width: measuredCols.name + periodRange.columns.length * measuredCols.column }}>
              <div className="shrink-0" style={{ width: Math.max(measuredCols.name - CHART_Y_AXIS_PX, 0) }} />
              <CapacityChart
                data={chartData}
                view={chartView}
                height={200}
                width={periodRange.columns.length * measuredCols.column + CHART_Y_AXIS_PX}
              />
            </div>
          </div>
        </div>
      </main>
    </div>
  );
}
