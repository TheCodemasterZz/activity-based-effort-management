import {
  Area,
  Bar,
  CartesianGrid,
  ComposedChart,
  Legend,
  Line,
  ReferenceLine,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts';

export interface CapacityChartPoint {
  label: string;
  actualHours: number;
  plannedHours: number;
  capacityHours: number;
  timeOffHours: number;
}

export type CapacityChartView = 'capacity' | 'availability' | 'utilization' | 'combined';

interface CapacityChartProps {
  data: CapacityChartPoint[];
  view: CapacityChartView;
  height?: number;
  /** Verildiğinde grafik "100%" yerine bu sabit piksel genişliğinde çizilir — Kapasite
   * Yönetimi sayfasındaki tabloyla sütun sütun hizalanabilmesi (bkz. sayfadaki NAME_COL_PX/
   * COLUMN_PX/CHART_Y_AXIS_PX sabitleri) için gereklidir. */
  width?: number;
}

const COLORS = {
  actual: '#4338ca', // indigo-700 — "Gerçekleşen" (Confirmed)
  planned: '#93c5fd', // blue-300 — "Planlanan" (Tentative)
  capacity: '#a855f7', // purple-500 — "Efektif Kapasite" çizgisi
  timeOff: '#8b5cf6', // violet-500 — izin dokusu
  available: '#10b981', // emerald-500 — "Müsaitlik"
  over: '#dc2626', // red-600 — %100 referans çizgisi (Kullanım görünümünde)
};

/** Varsayılan CartesianGrid dikey çizgileri her kategorinin MERKEZİNDEN geçer; tablodaki
 * hücre border'ı (`border-r`) ise her sütunun SAĞ KENARINDAN geçer. Tabloyla aynı hizada gün
 * ayrımı görünmesi için çizgileri kategori sınırlarına (merkez değil, kenar) taşıyan özel bir
 * `verticalCoordinatesGenerator` üretir. */
function makeColumnBoundaryGenerator(columnCount: number) {
  return ({ offset }: any) => {
    if (!offset || !columnCount) return [];
    const bandWidth = offset.width / columnCount;
    const points: number[] = [];
    for (let i = 1; i <= columnCount; i++) points.push(offset.left + i * bandWidth);
    return points;
  };
}

function CapacityTooltip({ active, payload, label }: any) {
  if (!active || !payload || payload.length === 0) return null;
  // 'combined'/'capacity' görünümlerinde Kapasite/Müsait çizgilerinin altına aynı dataKey'le bir
  // dolgu (Area, sadece görsel amaçlı) eklendiği için aynı seri tooltip payload'ında iki kez
  // görünür (Area'daki legendType="none" bunu sadece legend'dan gizler, tooltip'ten değil) —
  // burada dataKey'e göre tekilleştirilir.
  const seenDataKeys = new Set<string>();
  const uniquePayload = payload.filter((entry: any) => {
    if (seenDataKeys.has(entry.dataKey)) return false;
    seenDataKeys.add(entry.dataKey);
    return true;
  });

  return (
    <div className="rounded-md bg-slate-900 px-3 py-2 text-xs text-white shadow-lg">
      <div className="mb-1 text-[10px] font-semibold text-slate-300">{label}</div>
      {uniquePayload.map((entry: any) => (
        <div key={entry.dataKey} className="flex items-center gap-1.5 whitespace-nowrap">
          <span className="h-2 w-2 rounded-full" style={{ backgroundColor: entry.color }} />
          <span>
            {entry.name}: {entry.value.toFixed(1)}h
          </span>
        </div>
      ))}
    </div>
  );
}

/** Kapasite Yönetimi sayfasındaki agregat grafik — Kapasite/Müsaitlik/Kullanım görünümleri
 * arasında geçiş yapar; hepsi aynı ham veriden (per-sütun toplam saat) türetilir. */
export function CapacityChart({ data, view, height = 260, width }: CapacityChartProps) {
  const containerWidth = width ?? '100%';
  const yAxisWidth = width !== undefined ? 40 : 36;
  const chartMargin = width !== undefined ? { top: 8, right: 0, bottom: 0, left: 0 } : { top: 8, right: 12, bottom: 0, left: -12 };
  if (data.length === 0) {
    return <div className="flex h-full items-center justify-center text-sm text-slate-400">Yeterli veri yok</div>;
  }

  if (view === 'utilization') {
    const utilData = data.map((d) => ({
      label: d.label,
      Kullanım: d.capacityHours > 0 ? Math.round(((d.actualHours + d.plannedHours) / d.capacityHours) * 1000) / 10 : 0,
    }));
    return (
      <ResponsiveContainer width={containerWidth} height={height}>
        <ComposedChart data={utilData} margin={chartMargin}>
          <CartesianGrid horizontal vertical={false} stroke="#eef0f5" strokeDasharray="3 3" />
          <CartesianGrid
            horizontal={false}
            vertical
            stroke="#e2e8f0"
            verticalCoordinatesGenerator={makeColumnBoundaryGenerator(utilData.length)}
          />
          <XAxis dataKey="label" tick={{ fontSize: 11, fill: '#94a3b8' }} tickLine={false} axisLine={{ stroke: '#e2e8f0' }} />
          <YAxis
            domain={[0, 'dataMax']}
            allowDecimals={false}
            tick={{ fontSize: 11, fill: '#94a3b8' }}
            tickLine={false}
            axisLine={false}
            width={yAxisWidth}
            unit="%"
          />
          <Tooltip content={<CapacityTooltip />} />
          <ReferenceLine y={100} stroke={COLORS.over} strokeDasharray="4 4" label={{ value: '%100', fontSize: 10, fill: COLORS.over }} />
          <Line type="monotone" dataKey="Kullanım" stroke={COLORS.capacity} strokeWidth={2} dot={false} isAnimationActive={false} />
        </ComposedChart>
      </ResponsiveContainer>
    );
  }

  if (view === 'availability') {
    const availData = data.map((d) => ({
      label: d.label,
      Müsait: Math.max(0, d.capacityHours - d.actualHours - d.plannedHours),
    }));
    return (
      <ResponsiveContainer width={containerWidth} height={height}>
        <ComposedChart data={availData} margin={chartMargin}>
          <CartesianGrid horizontal vertical={false} stroke="#eef0f5" strokeDasharray="3 3" />
          <CartesianGrid
            horizontal={false}
            vertical
            stroke="#e2e8f0"
            verticalCoordinatesGenerator={makeColumnBoundaryGenerator(availData.length)}
          />
          <XAxis dataKey="label" tick={{ fontSize: 11, fill: '#94a3b8' }} tickLine={false} axisLine={{ stroke: '#e2e8f0' }} />
          <YAxis
            domain={[0, 'dataMax']}
            allowDecimals={false}
            tick={{ fontSize: 11, fill: '#94a3b8' }}
            tickLine={false}
            axisLine={false}
            width={yAxisWidth}
            unit="h"
          />
          <Tooltip content={<CapacityTooltip />} />
          <Area
            type="monotone"
            dataKey="Müsait"
            stroke={COLORS.available}
            fill={COLORS.available}
            fillOpacity={0.25}
            strokeWidth={2}
            isAnimationActive={false}
          />
        </ComposedChart>
      </ResponsiveContainer>
    );
  }

  if (view === 'combined') {
    const combinedData = data.map((d) => ({
      label: d.label,
      Gerçekleşen: d.actualHours,
      Planlanan: d.plannedHours,
      Kapasite: d.capacityHours,
      Müsait: Math.max(0, d.capacityHours - d.actualHours - d.plannedHours),
    }));
    return (
      <ResponsiveContainer width={containerWidth} height={height}>
        <ComposedChart data={combinedData} margin={chartMargin}>
          <CartesianGrid horizontal vertical={false} stroke="#eef0f5" strokeDasharray="3 3" />
          <CartesianGrid
            horizontal={false}
            vertical
            stroke="#e2e8f0"
            verticalCoordinatesGenerator={makeColumnBoundaryGenerator(combinedData.length)}
          />
          <XAxis dataKey="label" tick={{ fontSize: 11, fill: '#94a3b8' }} tickLine={false} axisLine={{ stroke: '#e2e8f0' }} />
          <YAxis tick={{ fontSize: 11, fill: '#94a3b8' }} tickLine={false} axisLine={false} width={yAxisWidth} unit="h" />
          <Tooltip content={<CapacityTooltip />} />
          <Legend wrapperStyle={{ fontSize: 13 }} height={26} iconType="plainline" iconSize={16} />
          {/* Dolgular kasıtlı olarak NÖTR gri — çizgilerle aynı renkte olsalardı (mor çizgi mor
              dolgu üstünde, yeşil çizgi yeşil dolgu üstünde) kontrast kaybolup okunmaz hale
              geliyordu. Nötr dolgu, veri kaybı olmadan sadece "kapasite tavanına kadarki alan"
              hissini korur, çizgilerin kendi rengiyle rekabet etmez. */}
          <Area
            type="monotone"
            dataKey="Kapasite"
            stroke="none"
            fill="#64748b"
            fillOpacity={0.07}
            isAnimationActive={false}
            legendType="none"
            tooltipType="none"
          />
          <Area
            type="monotone"
            dataKey="Müsait"
            stroke="none"
            fill="#64748b"
            fillOpacity={0.12}
            isAnimationActive={false}
            legendType="none"
            tooltipType="none"
          />
          <Bar dataKey="Gerçekleşen" stackId="workload" fill={COLORS.actual} radius={[0, 0, 0, 0]} isAnimationActive={false} />
          <Bar dataKey="Planlanan" stackId="workload" fill={COLORS.planned} radius={[3, 3, 0, 0]} isAnimationActive={false} />
          <Line type="monotone" dataKey="Kapasite" stroke={COLORS.capacity} strokeWidth={2.5} dot={false} isAnimationActive={false} />
          <Line
            type="monotone"
            dataKey="Müsait"
            stroke={COLORS.available}
            strokeWidth={2}
            strokeDasharray="4 3"
            dot={false}
            isAnimationActive={false}
          />
        </ComposedChart>
      </ResponsiveContainer>
    );
  }

  // 'capacity' — varsayılan görünüm: Gerçekleşen + Planlanan yığılmış bar, Efektif Kapasite çizgi.
  const capacityData = data.map((d) => ({
    label: d.label,
    Gerçekleşen: d.actualHours,
    Planlanan: d.plannedHours,
    Kapasite: d.capacityHours,
  }));

  return (
    <ResponsiveContainer width={containerWidth} height={height}>
      <ComposedChart data={capacityData} margin={chartMargin}>
        <CartesianGrid horizontal vertical={false} stroke="#eef0f5" strokeDasharray="3 3" />
        <CartesianGrid
          horizontal={false}
          vertical
          stroke="#e2e8f0"
          verticalCoordinatesGenerator={makeColumnBoundaryGenerator(capacityData.length)}
        />
        <XAxis dataKey="label" tick={{ fontSize: 11, fill: '#94a3b8' }} tickLine={false} axisLine={{ stroke: '#e2e8f0' }} />
        <YAxis tick={{ fontSize: 11, fill: '#94a3b8' }} tickLine={false} axisLine={false} width={yAxisWidth} unit="h" />
        <Tooltip content={<CapacityTooltip />} />
        <Legend wrapperStyle={{ fontSize: 13 }} height={26} iconType="plainline" iconSize={16} />
        <Area
          type="monotone"
          dataKey="Kapasite"
          stroke="none"
          fill="#64748b"
          fillOpacity={0.07}
          isAnimationActive={false}
          legendType="none"
          tooltipType="none"
        />
        <Bar dataKey="Gerçekleşen" stackId="workload" fill={COLORS.actual} radius={[0, 0, 0, 0]} isAnimationActive={false} />
        <Bar dataKey="Planlanan" stackId="workload" fill={COLORS.planned} radius={[3, 3, 0, 0]} isAnimationActive={false} />
        <Line type="monotone" dataKey="Kapasite" stroke={COLORS.capacity} strokeWidth={2.5} dot={false} isAnimationActive={false} />
      </ComposedChart>
    </ResponsiveContainer>
  );
}
