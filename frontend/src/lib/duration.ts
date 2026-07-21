const UNIT_PATTERN = /(\d+(?:[.,]\d+)?)\s*(h|m)/gi;

/**
 * Jira tarzı kısayol süre girişini ondalık saate çevirir:
 * "1h 30m" -> 1.5, "2h" -> 2, "45m" -> 0.75, "1h30m" -> 1.5.
 * "h"/"m" eki yoksa düz sayı doğrudan saat olarak kabul edilir (ör. "2" -> 2).
 * Geçersiz girişte null döner.
 */
export function parseDuration(input: string): number | null {
  const trimmed = input.trim();
  if (trimmed.length === 0) return null;

  let totalHours = 0;
  let matchedAny = false;
  let match: RegExpExecArray | null;

  UNIT_PATTERN.lastIndex = 0;
  while ((match = UNIT_PATTERN.exec(trimmed)) !== null) {
    matchedAny = true;
    const value = parseFloat(match[1].replace(',', '.'));
    if (Number.isNaN(value)) return null;

    totalHours += match[2].toLowerCase() === 'h' ? value : value / 60;
  }

  if (matchedAny) {
    const leftover = trimmed.replace(/(\d+(?:[.,]\d+)?)\s*(h|m)/gi, '').replace(/\s+/g, '');
    if (leftover.length > 0) return null;
    return Math.round(totalHours * 100) / 100;
  }

  // parseFloat() burada kullanılmıyor çünkü baştaki geçerli sayısal kısmı ayrıştırıp sondaki
  // anlamsız karakterleri sessizce yok sayar (ör. parseFloat("5dfjksdjfsd") === 5) — tüm girdi
  // tam olarak bir sayıya eşleşmezse geçersiz sayılmalı.
  const normalized = trimmed.replace(',', '.');
  if (!/^\d+(\.\d+)?$/.test(normalized)) return null;
  return Math.round(parseFloat(normalized) * 100) / 100;
}

/** Ondalık saati "1h 30m" gibi Jira tarzı kısayol formatına çevirir (düzenleme modunda geri göstermek için). */
export function formatDuration(hours: number): string {
  const totalMinutes = Math.round(hours * 60);
  const h = Math.floor(totalMinutes / 60);
  const m = totalMinutes % 60;

  if (h > 0 && m > 0) return `${h}h ${m}m`;
  if (h > 0) return `${h}h`;
  return `${m}m`;
}
