export type RiskSeverityTier = 'low' | 'medium' | 'high' | 'critical';

/** Olasılık x Etki (her biri 1-5) çarpımına göre 4 kademeli risk şiddeti — spiHealthTier ile
 * aynı "ham sayıyı bir kademeye indirger" deseni (bkz. lib/projectSpi.ts). */
export function riskSeverityTier(probability: number, impact: number): RiskSeverityTier {
  const score = probability * impact;
  if (score >= 15) return 'critical';
  if (score >= 8) return 'high';
  if (score >= 4) return 'medium';
  return 'low';
}
