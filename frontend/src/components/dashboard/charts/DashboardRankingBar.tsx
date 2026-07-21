import { Bar, BarChart, CartesianGrid, Cell, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import { ChartTooltip } from './ChartTooltip';

interface DashboardRankingBarProps {
  data: { label: string; value: number }[];
  color: string;
  unit?: string;
  height?: number | `${number}%`;
}

/** Yatay sıralama çubuğu — en çok efor harcanan proje/çalışan/aktivite gibi Top-N listeleri için. */
export function DashboardRankingBar({ data, color, unit = 'h', height = 220 }: DashboardRankingBarProps) {
  if (data.length === 0) {
    return <div className="flex h-full items-center justify-center text-sm text-slate-400">Kayıt yok</div>;
  }

  return (
    <ResponsiveContainer width="100%" height={height}>
      <BarChart data={data} layout="vertical" margin={{ top: 4, right: 24, bottom: 0, left: 4 }}>
        <CartesianGrid horizontal={false} stroke="#eef0f5" strokeDasharray="3 3" />
        <XAxis type="number" hide />
        <YAxis
          dataKey="label"
          type="category"
          width={132}
          tick={{ fontSize: 12, fill: '#475569' }}
          tickLine={false}
          axisLine={false}
        />
        <Tooltip
          content={<ChartTooltip unit={unit} />}
          cursor={{ fill: 'rgba(15, 23, 42, 0.04)' }}
        />
        <Bar dataKey="value" radius={[0, 4, 4, 0]} isAnimationActive={false} barSize={16}>
          {data.map((_, index) => (
            <Cell key={index} fill={color} fillOpacity={0.55 + 0.45 * ((data.length - index) / data.length)} />
          ))}
        </Bar>
      </BarChart>
    </ResponsiveContainer>
  );
}
