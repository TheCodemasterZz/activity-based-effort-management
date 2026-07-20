import { Bar, BarChart, Tooltip } from 'recharts';
import { ChartTooltip } from './ChartTooltip';

interface TrendBarWidgetProps {
  data: { label: string; value: number }[];
  color: string;
  unit?: string;
  width?: number;
  height?: number;
}

export function TrendBarWidget({ data, color, unit, width = 110, height = 44 }: TrendBarWidgetProps) {
  if (data.length === 0) return null;

  return (
    <BarChart width={width} height={height} data={data} margin={{ top: 4, right: 2, bottom: 0, left: 2 }}>
      <Tooltip
        content={<ChartTooltip unit={unit} />}
        cursor={{ fill: 'rgba(15, 23, 42, 0.06)' }}
        wrapperStyle={{ zIndex: 50, outline: 'none' }}
        isAnimationActive={false}
      />
      <Bar dataKey="value" fill={color} radius={[3, 3, 0, 0]} isAnimationActive={false} />
    </BarChart>
  );
}
