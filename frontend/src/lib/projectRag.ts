import type { ProjectDetailDto, ProjectDto, ProjectIssueDto, ProjectRiskDto, ProjectTaskDto } from '../api/types';
import { riskSeverityTier } from './projectRisk';
import { spiHealthTier, type ProjectEvmSummary } from './projectSpi';

export type RagTier = 'green' | 'amber' | 'red' | 'gray';

export interface RagResult {
  tier: RagTier;
  label: string;
  detail: string;
}

/** RAG (Red/Amber/Green) hücrelerinin dolgu arkaplan sınıfı — "hücrenin arkaplanını
 * renklendirerek" isteğine uygun, solid/trafik ışığı tarzı (soft pill değil). */
export const RAG_CELL_CLASS: Record<RagTier, string> = {
  green: 'bg-emerald-500 text-white',
  amber: 'bg-amber-400 text-white',
  red: 'bg-red-600 text-white',
  gray: 'bg-slate-200 text-slate-500',
};

const HEALTH_STATUS_RAG: Record<string, { tier: RagTier; label: string }> = {
  OnTrack: { tier: 'green', label: 'ON TRACK' },
  AtRisk: { tier: 'amber', label: 'AT RISK' },
  NeedsHelp: { tier: 'red', label: 'NEEDS HELP' },
};

/** 1) Genel Sağlık RAG — proje yöneticisinin elle belirlediği HealthStatus'un doğrudan RAG
 * karşılığı (OnTrack/AtRisk/NeedsHelp zaten 3'lü bir RAG ölçeği). */
export function healthRag(project: Pick<ProjectDto, 'healthStatus'>): RagResult {
  const info = HEALTH_STATUS_RAG[project.healthStatus] ?? { tier: 'gray' as RagTier, label: project.healthStatus };
  return { tier: info.tier, label: info.label, detail: `Genel sağlık: ${info.label}` };
}

/** 2) Zaman (Schedule) RAG — tamamlanma % ile geçen zaman % karşılaştırması. ProjectsTable'daki
 * completionColor mantığıyla aynı eşikler, RAG hücresine dönüştürülmüş hali. */
export function scheduleRag(elapsedPercent: number | null, percentComplete: number): RagResult {
  if (elapsedPercent === null) {
    return { tier: 'gray', label: '—', detail: 'Tarih aralığı tanımlı değil.' };
  }
  if (percentComplete >= elapsedPercent) {
    return { tier: 'green', label: 'Takvimde', detail: `Tamamlanma %${percentComplete}, geçen süre %${elapsedPercent.toFixed(0)}.` };
  }
  if (percentComplete >= elapsedPercent - 15) {
    return { tier: 'amber', label: 'Hafif Geride', detail: `Tamamlanma %${percentComplete}, geçen süre %${elapsedPercent.toFixed(0)}.` };
  }
  return { tier: 'red', label: 'Geride', detail: `Tamamlanma %${percentComplete}, geçen süre %${elapsedPercent.toFixed(0)}.` };
}

const SPI_TIER_RAG: Record<string, RagTier> = { good: 'green', warning: 'amber', critical: 'red', unknown: 'gray' };

/** 3) Performans (SPI) RAG — mevcut spiHealthTier eşiklerinin RAG karşılığı. */
export function performanceRag(evm: ProjectEvmSummary): RagResult {
  const tier = SPI_TIER_RAG[spiHealthTier(evm.spi)];
  return {
    tier,
    label: evm.spi !== null ? `SPI ${evm.spi}` : 'SPI —',
    detail: evm.spi !== null ? `Planlanan Değer/Kazanılmış Değer oranı: ${evm.spi}.` : 'Henüz hesaplanabilir görev yok.',
  };
}

/** 4) Risk RAG — açık/azaltılıyor durumundaki risklerin en yüksek şiddetine göre. */
export function riskRag(risks: ProjectRiskDto[]): RagResult {
  const openRisks = risks.filter((r) => r.status !== 'Closed');
  const hasCritical = openRisks.some((r) => riskSeverityTier(r.probability, r.impact) === 'critical');
  const hasHigh = openRisks.some((r) => riskSeverityTier(r.probability, r.impact) === 'high');

  if (hasCritical) return { tier: 'red', label: 'Kritik Risk', detail: 'En az bir açık kritik şiddetli risk var.' };
  if (hasHigh) return { tier: 'amber', label: 'Yüksek Risk', detail: 'En az bir açık yüksek şiddetli risk var.' };
  if (openRisks.length > 0) return { tier: 'green', label: `${openRisks.length} Açık Risk`, detail: 'Açık riskler düşük/orta şiddette.' };
  return { tier: 'green', label: 'Risk Yok', detail: 'Açık risk kaydı yok.' };
}

/** 5) Sorun (Issue) RAG — süresi geçmiş veya kritik öncelikli açık sorun varsa kırmızı. */
export function issueRag(issues: ProjectIssueDto[], todayKey: string): RagResult {
  const openIssues = issues.filter((i) => i.status !== 'Closed' && i.status !== 'Resolved');
  const overdue = openIssues.filter((i) => i.dueDate && i.dueDate < todayKey);
  const hasCritical = openIssues.some((i) => i.priority === 'Critical');

  if (overdue.length > 0 || hasCritical) {
    return { tier: 'red', label: overdue.length > 0 ? `${overdue.length} Süresi Geçmiş` : 'Kritik Sorun', detail: 'Süresi geçmiş veya kritik öncelikli açık sorun var.' };
  }
  if (openIssues.some((i) => i.priority === 'High')) {
    return { tier: 'amber', label: 'Yüksek Öncelikli', detail: 'Açık yüksek öncelikli sorun var.' };
  }
  if (openIssues.length > 0) {
    return { tier: 'amber', label: `${openIssues.length} Açık Sorun`, detail: 'Düşük/orta öncelikli açık sorunlar var.' };
  }
  return { tier: 'green', label: 'Sorun Yok', detail: 'Açık sorun kaydı yok.' };
}

/** 6) Veri Kalitesi (Data Quality) RAG — "bilgi girişi yapılıyor mu, tutarlı mı" sorusuna cevap:
 * Overview alanlarının doluluğu + görevlerin kişi ataması tutarlılığı + yakın zamanda efor
 * girişi aktivitesi olup olmadığının birleşik bir kontrol listesi. */
export function dataQualityRag(
  project: Pick<ProjectDto | ProjectDetailDto, 'description' | 'sponsor' | 'projectManagerEmployeeId' | 'strategicGoal' | 'startDate' | 'endDate'>,
  tasks: ProjectTaskDto[],
  hasRecentActualActivity: boolean,
): RagResult {
  const checks: { label: string; passed: boolean }[] = [
    { label: 'Açıklama girilmiş', passed: !!project.description },
    { label: 'Sponsor tanımlı', passed: !!project.sponsor },
    { label: 'Proje yöneticisi tanımlı', passed: !!project.projectManagerEmployeeId },
    { label: 'Stratejik hedef tanımlı', passed: !!project.strategicGoal },
    { label: 'Başlangıç/bitiş tarihi girilmiş', passed: !!project.startDate && !!project.endDate },
    { label: 'En az bir görev var', passed: tasks.length > 0 },
    { label: 'Görevler kişiye atanmış', passed: tasks.length > 0 && tasks.every((t) => !!t.assignedEmployeeId) },
    { label: 'Yakın zamanda efor girişi var', passed: hasRecentActualActivity },
  ];

  const passedCount = checks.filter((c) => c.passed).length;
  const ratio = passedCount / checks.length;
  const failedLabels = checks.filter((c) => !c.passed).map((c) => c.label);

  const tier: RagTier = ratio >= 0.85 ? 'green' : ratio >= 0.5 ? 'amber' : 'red';
  return {
    tier,
    label: `${passedCount}/${checks.length}`,
    detail:
      failedLabels.length > 0
        ? `Eksik: ${failedLabels.join(', ')}.`
        : 'Tüm veri kalitesi kontrolleri geçildi.',
  };
}

export function timeElapsedPercent(start: string | null, end: string | null): number | null {
  if (!start || !end) return null;
  const startMs = new Date(`${start}T00:00:00`).getTime();
  const endMs = new Date(`${end}T00:00:00`).getTime();
  const nowMs = Date.now();
  if (endMs <= startMs) return null;
  return Math.min(100, Math.max(0, ((nowMs - startMs) / (endMs - startMs)) * 100));
}
