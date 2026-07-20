import { PolarAngleAxis, RadialBar, RadialBarChart, Tooltip } from 'recharts';

interface RatioGaugeWidgetProps {
  percent: number;
  color: string;
  size?: number;
  label: string;
}

export function RatioGaugeWidget({ percent, color, size = 56, label }: RatioGaugeWidgetProps) {
  const clamped = Math.max(0, Math.min(100, percent));
  const data = [{ name: label, value: clamped, fill: color }];

  return (
    <div className="relative" style={{ width: size, height: size }}>
      <RadialBarChart
        width={size}
        height={size}
        innerRadius="72%"
        outerRadius="100%"
        data={data}
        startAngle={90}
        endAngle={-270}
        barSize={6}
      >
        <PolarAngleAxis type="number" domain={[0, 100]} angleAxisId={0} tick={false} />
        <RadialBar background={{ fill: '#f1f5f9' }} dataKey="value" cornerRadius={20} isAnimationActive={false} />
        <Tooltip
          content={({ active }) =>
            active ? (
              <div className="rounded-md bg-slate-900 px-2.5 py-1.5 text-xs font-medium whitespace-nowrap text-white shadow-lg">
                {`${label}: %${clamped.toFixed(0)}`}
              </div>
            ) : null
          }
          wrapperStyle={{ zIndex: 50, outline: 'none' }}
        />
      </RadialBarChart>
      <div className="pointer-events-none absolute inset-0 flex items-center justify-center text-[13px] font-semibold text-slate-700">
        {`${clamped.toFixed(0)}%`}
      </div>
    </div>
  );
}
