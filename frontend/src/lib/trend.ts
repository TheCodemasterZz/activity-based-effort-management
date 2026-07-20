export interface ChartPoint {
  label: string;
  value: number;
}

/** Serinin son iki noktası arasındaki yüzde değişimi hesaplar; kıyaslanacak veri yoksa null döner. */
export function computeTrendPercent(points: ChartPoint[]): number | null {
  if (points.length < 2) return null;
  const prev = points[points.length - 2].value;
  const last = points[points.length - 1].value;
  if (prev === 0) return last === 0 ? 0 : null;
  return ((last - prev) / prev) * 100;
}
