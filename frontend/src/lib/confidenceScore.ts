import type { ConfidenceScoreSettingsDto } from '../api/types';

export interface ConfidenceScoreInput {
  userId: string;
  workDate: string; // yyyy-MM-dd
  hours: number;
  description: string;
  projectName?: string;
  activityL1Name?: string;
  activityL2Name?: string;
}

export interface ConfidenceScoreSiblingLog {
  id: string;
  workDate: string;
  hours: number;
  description: string;
}

export interface ConfidenceScoreContext {
  /** Aynı çalışanın DİĞER (skorlanan kayıt hariç) Actual kayıtları — baseline/tekrar/günlük
   * toplam sinyalleri bunlardan hesaplanır. Geniş bir pencerede (ör. son 90 gün) çekilip motor
   * içinde ayarların lookback değerlerine göre daraltılır. */
  siblingLogs: ConfidenceScoreSiblingLog[];
  holidayDateKeys: Set<string>;
  today?: Date;
}

export type ConfidenceTier = 'veryLow' | 'low' | 'medium' | 'high' | 'veryHigh';

export const CONFIDENCE_TIER_LABEL: Record<ConfidenceTier, string> = {
  veryLow: 'Çok Düşük',
  low: 'Düşük',
  medium: 'Orta',
  high: 'Yüksek',
  veryHigh: 'Çok Yüksek',
};

export interface ConfidenceSignalResult {
  key: string;
  label: string;
  points: number;
  maxPoints: number;
  reason: string;
}

export interface ConfidenceScoreResult {
  score: number;
  tier: ConfidenceTier;
  tierLabel: string;
  signals: ConfidenceSignalResult[];
}

function dateKeysAreWithinDays(dateKey: string, referenceKey: string, days: number): boolean {
  const date = new Date(`${dateKey}T00:00:00`).getTime();
  const reference = new Date(`${referenceKey}T00:00:00`).getTime();
  const diffDays = Math.abs(reference - date) / 86_400_000;
  return diffDays <= days;
}

function normalizeWords(text: string): Set<string> {
  return new Set(
    text
      .toLocaleLowerCase('tr')
      .replace(/[^\p{L}\p{N}\s]/gu, ' ')
      .split(/\s+/)
      .filter((w) => w.length >= 3),
  );
}

/** Jaccard benzerliği (kelime kümesi kesişimi / birleşimi) — tam bir NLP kütüphanesi olmadan
 * kopyala-yapıştır tekrarını yakalamak için basit ama etkili bir yöntem. */
function textSimilarity(a: string, b: string): number {
  const wordsA = normalizeWords(a);
  const wordsB = normalizeWords(b);
  if (wordsA.size === 0 && wordsB.size === 0) return 1;
  if (wordsA.size === 0 || wordsB.size === 0) return 0;
  let intersection = 0;
  for (const w of wordsA) if (wordsB.has(w)) intersection++;
  const union = wordsA.size + wordsB.size - intersection;
  return union === 0 ? 0 : intersection / union;
}

function clamp01(value: number): number {
  return Math.max(0, Math.min(1, value));
}

/** Efor kaydının "güvenilirlik skoru"nu (0-100) ve her sinyalin ayrı katkısını hesaplar.
 * Kural tabanlı, deterministik, tamamen mevcut veriyle (şema değişikliği yok) çalışır — skor
 * DB'de saklanmaz, her görüntülemede ayarlarla birlikte yeniden hesaplanır. */
export function computeConfidenceScore(
  input: ConfidenceScoreInput,
  context: ConfidenceScoreContext,
  settings: ConfidenceScoreSettingsDto,
): ConfidenceScoreResult {
  const today = context.today ?? new Date();
  const todayKey = today.toISOString().slice(0, 10);
  const description = input.description.trim();

  const sameUserLogs = context.siblingLogs;
  const sameDayLogs = sameUserLogs.filter((l) => l.workDate === input.workDate);
  const dailyTotal = sameDayLogs.reduce((sum, l) => sum + l.hours, 0) + input.hours;

  const signals: ConfidenceSignalResult[] = [];

  // A1 — Açıklama uzunluğu & detay.
  {
    const len = description.length;
    let ratio: number;
    if (len <= settings.shortDescriptionCharThreshold) ratio = len === 0 ? 0 : 0.2;
    else if (len >= settings.longDescriptionCharThreshold) ratio = 1;
    else
      ratio =
        0.2 +
        0.8 *
          ((len - settings.shortDescriptionCharThreshold) /
            (settings.longDescriptionCharThreshold - settings.shortDescriptionCharThreshold));
    signals.push({
      key: 'A1',
      label: 'Açıklama Uzunluğu',
      points: Math.round(ratio * settings.weightDescriptionLength),
      maxPoints: settings.weightDescriptionLength,
      reason: `Açıklama ${len} karakter (eşikler: ${settings.shortDescriptionCharThreshold}–${settings.longDescriptionCharThreshold}).`,
    });
  }

  // A2 — Spesifiklik: proje/aktivite adı geçiyor mu.
  const entityNames = [input.projectName, input.activityL1Name, input.activityL2Name].filter(
    (n): n is string => !!n && n.trim().length > 0,
  );
  {
    const descLower = description.toLocaleLowerCase('tr');
    const matchedEntities = entityNames.filter((name) => {
      const words = name
        .toLocaleLowerCase('tr')
        .replace(/[^\p{L}\p{N}\s]/gu, ' ')
        .split(/\s+/)
        .filter((w) => w.length >= 4);
      return words.some((w) => descLower.includes(w));
    });
    const ratio = clamp01(matchedEntities.length / 2);
    signals.push({
      key: 'A2',
      label: 'Spesifiklik',
      points: Math.round(ratio * settings.weightSpecificity),
      maxPoints: settings.weightSpecificity,
      reason:
        matchedEntities.length > 0
          ? `Açıklamada geçen: ${matchedEntities.join(', ')}.`
          : 'Açıklamada proje/aktivite adına dair bir ize rastlanmadı.',
    });
  }

  // A3 — Jenerik/boilerplate ifade cezası.
  {
    const phrases = settings.genericPhrasesCsv
      .split(',')
      .map((p) => p.trim().toLocaleLowerCase('tr'))
      .filter(Boolean);
    const descLower = description.toLocaleLowerCase('tr');
    const matchedPhrase = phrases.find((p) => p.length > 0 && descLower.includes(p));
    const hasSpecificity = signals.find((s) => s.key === 'A2')!.points > 0;
    let ratio = 1;
    if (matchedPhrase) ratio = hasSpecificity ? 0.5 : 0;
    signals.push({
      key: 'A3',
      label: 'Jenerik İfade',
      points: Math.round(ratio * settings.weightGenericPenalty),
      maxPoints: settings.weightGenericPenalty,
      reason: matchedPhrase
        ? `Jenerik ifade tespit edildi: "${matchedPhrase}"${hasSpecificity ? ' (ama başka spesifik bilgi de var).' : '.'}`
        : 'Jenerik/kalıp ifade tespit edilmedi.',
    });
  }

  // B1 — Tekrar tespiti (son N gün içindeki kayıtlarla metin benzerliği).
  {
    const recentLogs = sameUserLogs.filter((l) => dateKeysAreWithinDays(l.workDate, todayKey, settings.duplicateLookbackDays));
    let maxSimilarity = 0;
    for (const log of recentLogs) {
      const sim = textSimilarity(description, log.description);
      if (sim > maxSimilarity) maxSimilarity = sim;
    }
    const isDuplicate = maxSimilarity >= settings.duplicateSimilarityThreshold;
    const ratio = isDuplicate ? 0 : 1;
    signals.push({
      key: 'B1',
      label: 'Tekrar Tespiti',
      points: Math.round(ratio * settings.weightDuplicateDetection),
      maxPoints: settings.weightDuplicateDetection,
      reason: isDuplicate
        ? `Son ${settings.duplicateLookbackDays} gün içindeki bir kayıtla %${Math.round(maxSimilarity * 100)} benzer.`
        : `Son ${settings.duplicateLookbackDays} gün içinde belirgin bir tekrar bulunamadı (en yüksek benzerlik %${Math.round(maxSimilarity * 100)}).`,
    });
  }

  // C1 — Yuvarlak süre (tekil kayıt).
  {
    const isWhole = input.hours % 1 === 0;
    const isQuarter = Math.abs(input.hours * 4 - Math.round(input.hours * 4)) < 1e-9;
    const ratio = isWhole ? 0 : isQuarter ? 0.6 : 1;
    signals.push({
      key: 'C1',
      label: 'Yuvarlak Süre',
      points: Math.round(ratio * settings.weightRoundHoursSingle),
      maxPoints: settings.weightRoundHoursSingle,
      reason: isWhole
        ? `${input.hours}h tam saat — tahmini girilmiş olabilir.`
        : isQuarter
          ? `${input.hours}h 15 dakikalık bir katı.`
          : `${input.hours}h — ondalıklı, gerçekçi bir ölçüm izlenimi veriyor.`,
    });
  }

  // C2 — Süre-açıklama orantısı.
  {
    let ratio = 1;
    let reason = 'Süre ve açıklama uzunluğu orantılı.';
    if (input.hours >= settings.longDurationHoursThreshold && description.length <= settings.shortDescriptionCharThreshold) {
      ratio = 0;
      reason = `${input.hours}h gibi uzun bir süreye karşılık açıklama çok kısa (${description.length} karakter).`;
    } else if (input.hours <= settings.shortDurationHoursThreshold && description.length >= settings.longDescriptionCharThreshold) {
      ratio = 0.4;
      reason = `${input.hours}h gibi kısa bir süreye karşılık açıklama olağandışı uzun (${description.length} karakter).`;
    }
    signals.push({
      key: 'C2',
      label: 'Süre-Açıklama Orantısı',
      points: Math.round(ratio * settings.weightDurationDescriptionRatio),
      maxPoints: settings.weightDurationDescriptionRatio,
      reason,
    });
  }

  // C3 — Günlük toplamın da yuvarlak olması (parçalanmış round-saat paterni).
  {
    const entryCountToday = sameDayLogs.length + 1;
    const isWholeTotal = Math.abs(dailyTotal - Math.round(dailyTotal)) < 1e-9;
    const ratio = entryCountToday >= 2 && isWholeTotal ? 0 : 1;
    signals.push({
      key: 'C3',
      label: 'Günlük Toplam Yuvarlaklık',
      points: Math.round(ratio * settings.weightDailyRoundTotal),
      maxPoints: settings.weightDailyRoundTotal,
      reason:
        entryCountToday >= 2
          ? `Bugün ${entryCountToday} kayıt, toplam ${dailyTotal}h${isWholeTotal ? ' (tam sayı — parçalanmış round-saat paterni olabilir).' : '.'}`
          : 'Bugün tek kayıt var, günlük toplam yuvarlaklığı değerlendirilmedi.',
    });
  }

  // D1 — Günlük toplam saat makullüğü.
  {
    let ratio = 1;
    if (dailyTotal > 12) ratio = 0;
    else if (dailyTotal > settings.dailyTotalSuspiciousHours) ratio = 0.5;
    signals.push({
      key: 'D1',
      label: 'Günlük Toplam Makullüğü',
      points: Math.round(ratio * settings.weightDailyTotalReasonableness),
      maxPoints: settings.weightDailyTotalReasonableness,
      reason: `Bu gün için toplam ${dailyTotal.toFixed(2)}h (şüpheli eşik: ${settings.dailyTotalSuspiciousHours}h).`,
    });
  }

  // E1 — Kişi bazlı baseline sapması.
  {
    const historicalLogs = sameUserLogs.filter((l) => dateKeysAreWithinDays(l.workDate, todayKey, settings.baselineLookbackDays));
    if (historicalLogs.length < 3) {
      signals.push({
        key: 'E1',
        label: 'Kişisel Baseline Sapması',
        points: settings.weightBaselineDeviation,
        maxPoints: settings.weightBaselineDeviation,
        reason: 'Kişinin baseline oluşturacak kadar geçmiş kaydı yok, bu sinyal nötr sayıldı.',
      });
    } else {
      const avgLen = historicalLogs.reduce((sum, l) => sum + l.description.trim().length, 0) / historicalLogs.length;
      const currentLen = description.length;
      const relative = avgLen > 0 ? currentLen / avgLen : 1;
      let ratio = 1;
      if (relative < 0.3) ratio = 0.2;
      else if (relative < 0.6) ratio = 0.6;
      signals.push({
        key: 'E1',
        label: 'Kişisel Baseline Sapması',
        points: Math.round(ratio * settings.weightBaselineDeviation),
        maxPoints: settings.weightBaselineDeviation,
        reason: `Kişinin ortalama açıklama uzunluğu ${avgLen.toFixed(0)} karakter, bu kayıt ${currentLen} karakter.`,
      });
    }
  }

  // F1 — Hafta sonu/resmi tatil günü.
  {
    const dayOfWeek = new Date(`${input.workDate}T00:00:00`).getDay();
    const isWeekend = dayOfWeek === 0 || dayOfWeek === 6;
    const isHoliday = context.holidayDateKeys.has(input.workDate);
    const ratio = isWeekend || isHoliday ? 0 : 1;
    signals.push({
      key: 'F1',
      label: 'Hafta Sonu/Resmi Tatil',
      points: Math.round(ratio * settings.weightWeekendHoliday),
      maxPoints: settings.weightWeekendHoliday,
      reason: isHoliday ? 'Bu tarih resmi tatile denk geliyor.' : isWeekend ? 'Bu tarih hafta sonuna denk geliyor.' : 'Hafta içi, tatil değil.',
    });
  }

  const totalPoints = signals.reduce((sum, s) => sum + s.points, 0);
  const totalMax = signals.reduce((sum, s) => sum + s.maxPoints, 0);
  const score = totalMax > 0 ? Math.round((totalPoints / totalMax) * 100) : 100;

  const tier: ConfidenceTier =
    score < settings.thresholdVeryLow
      ? 'veryLow'
      : score < settings.thresholdLow
        ? 'low'
        : score < settings.thresholdMedium
          ? 'medium'
          : score < settings.thresholdHigh
            ? 'high'
            : 'veryHigh';

  return { score, tier, tierLabel: CONFIDENCE_TIER_LABEL[tier], signals };
}
