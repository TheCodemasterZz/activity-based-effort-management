import { Area, AreaChart, Tooltip } from 'recharts';
import { ChartTooltip } from './ChartTooltip';

interface TrendAreaWidgetProps {
  data: { label: string; value: number }[];
  color: string;
  unit?: string;
  width?: number;
  height?: number;
}

export function TrendAreaWidget({ data, color, unit, width = 110, height = 44 }: TrendAreaWidgetProps) {
  if (data.length < 2) return null;
  const gradientId = `area-grad-${color.replace('#', '')}`;

  return (
    <AreaChart width={width} height={height} data={data} margin={{ top: 4, right: 2, bottom: 0, left: 2 }}>
      <defs>
        <linearGradient id={gradientId} x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor={color} stopOpacity={0.35} />
          <stop offset="100%" stopColor={color} stopOpacity={0} />
        </linearGradient>
      </defs>
      <Tooltip
        content={<ChartTooltip unit={unit} />}
        cursor={{ stroke: color, strokeWidth: 1, strokeDasharray: '2 2' }}
        wrapperStyle={{ zIndex: 50, outline: 'none' }}
        isAnimationActive={false}
      />
      <Area
        type="monotone"
        dataKey="value"
        stroke={color}
        strokeWidth={2}
        fill={`url(#${gradientId})`}
        dot={false}
        activeDot={{ r: 4, fill: color, stroke: '#fff', strokeWidth: 1.5 }}
        isAnimationActive={false}
      />
    </AreaChart>
  );
}
