interface ChartTooltipPayload {
  value: number;
}

interface ChartTooltipProps {
  active?: boolean;
  label?: string;
  payload?: ChartTooltipPayload[];
  unit?: string;
}

/** Tüm mini widget grafiklerinde paylaşılan koyu, kart üzerine binen (overlay) tooltip kutusu. */
export function ChartTooltip({ active, label, payload, unit = '' }: ChartTooltipProps) {
  if (!active || !payload || payload.length === 0) return null;

  return (
    <div className="rounded-md bg-slate-900 px-2.5 py-1.5 text-xs font-medium whitespace-nowrap text-white shadow-lg">
      {label && <div className="text-[10px] text-slate-300">{label}</div>}
      <div>
        {payload[0].value.toFixed(1)}
        {unit}
      </div>
    </div>
  );
}
