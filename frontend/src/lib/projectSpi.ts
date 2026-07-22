import type { ProjectTaskDto } from '../api/types';

export interface ProjectEvmSummary {
  /** Planlanan Değer (PV) — raporlama tarihine kadar bitmesi gereken görevlerin baseline efor toplamı. */
  plannedValue: number;
  /** Kazanılmış Değer (EV) — 0/100 kuralına göre, raporlama tarihine kadar GERÇEKTEN "Bitti"
   * durumuna geçmiş görevlerin baseline efor toplamı (ne kadar saat harcandığı önemli değil). */
  earnedValue: number;
  /** SPI = EV / PV. PV sıfırsa (henüz hiçbir görevin bitiş tarihi gelmediyse) tanımsız (null). */
  spi: number | null;
  totalTaskCount: number;
  doneTaskCount: number;
  /** Tamamlanma yüzdesi — bitmiş görev sayısı / toplam görev sayısı (basit, EV'den bağımsız bir gösterge). */
  percentComplete: number;
}

/** Not 1'deki SPI/EVM örneğinin birebir uygulaması: PV = raporlama tarihine kadar bitmesi
 * PLANLANAN (baseline) görevlerin efor toplamı; EV = o tarihe kadar GERÇEKTEN bitmiş görevlerin
 * efor toplamı (0/100 — kısmen ilerlemiş ama bitmemiş bir görev hiç katkı vermez). Gerçekleşen
 * (actual/harcanan) efor kasıtlı olarak bu hesaba hiç dahil edilmez — EVM'in temel kuralı. */
export function computeProjectEvmSummary(tasks: ProjectTaskDto[], reportingDate: Date = new Date()): ProjectEvmSummary {
  const reportingKey = reportingDate.toISOString().slice(0, 10);
  const realTasks = tasks; // Kilometre taşları da (efor=0 olduğundan) toplamı bozmadan dahil edilebilir.

  let plannedValue = 0;
  let earnedValue = 0;
  let doneTaskCount = 0;

  for (const task of realTasks) {
    const isDueByNow = task.baselineEndDate <= reportingKey;
    if (isDueByNow) plannedValue += task.baselineEffortHours;

    const isDone = task.status === 'Done';
    if (isDone) doneTaskCount++;
    if (isDueByNow && isDone) earnedValue += task.baselineEffortHours;
  }

  return {
    plannedValue,
    earnedValue,
    spi: plannedValue > 0 ? Math.round((earnedValue / plannedValue) * 100) / 100 : null,
    totalTaskCount: realTasks.length,
    doneTaskCount,
    percentComplete: realTasks.length > 0 ? Math.round((doneTaskCount / realTasks.length) * 100) : 0,
  };
}

export type SpiHealthTier = 'good' | 'warning' | 'critical' | 'unknown';

/** SPI eşikleri: >=0.95 iyi (planında/yakınında), 0.8-0.95 dikkat, <0.8 kritik — endüstri
 * pratiğinde yaygın kullanılan aralıklar. */
export function spiHealthTier(spi: number | null): SpiHealthTier {
  if (spi === null) return 'unknown';
  if (spi >= 0.95) return 'good';
  if (spi >= 0.8) return 'warning';
  return 'critical';
}
