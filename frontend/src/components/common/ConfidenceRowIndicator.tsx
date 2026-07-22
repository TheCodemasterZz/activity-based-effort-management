import { useLayoutEffect, useRef, useState } from 'react';
import { createPortal } from 'react-dom';
import type { ConfidenceScoreResult, ConfidenceTier } from '../../lib/confidenceScore';

const TIER_BORDER_CLASS: Record<ConfidenceTier, string> = {
  veryLow: 'border-l-4 border-red-500 bg-red-50/50',
  low: 'border-l-4 border-orange-500 bg-orange-50/50',
  medium: 'border-l-4 border-amber-500 bg-amber-50/50',
  high: 'border-l-4 border-emerald-400 bg-emerald-50/40',
  veryHigh: 'border-l-4 border-emerald-600 bg-emerald-50/50',
};

const TIER_TEXT_CLASS: Record<ConfidenceTier, string> = {
  veryLow: 'text-red-700',
  low: 'text-orange-700',
  medium: 'text-amber-700',
  high: 'text-emerald-700',
  veryHigh: 'text-emerald-800',
};

const VIEWPORT_MARGIN = 8;

interface ConfidenceLiRowProps {
  result: ConfidenceScoreResult | null;
  /** Sınır/renklendirme dışındaki temel sınıflar (ör. "flex items-center justify-between gap-2 text-xs"). */
  baseClassName: string;
  children: React.ReactNode;
}

/** Efor Onayı listesindeki bir satırı (<li>) render eder. Sonuç varsa güven skoru tier'ına göre
 * sol kalın border + arkaplan tonu uygular. Üzerine gelince (hover), tüm sinyal dökümünü içeren
 * bir panel açılır. Panel `document.body`'ye portallanır ve `position: fixed` ile konumlanır ki
 * listenin kendi `overflow-y-auto` kapsayıcısı (ve modalın dış scroll alanı) tarafından
 * KIRPILMASIN. Konum, panel gerçekten render edildikten SONRA gerçek yüksekliği ölçülerek
 * hesaplanır (`useLayoutEffect`) — alttaki boşluk yetmezse panel satırın ÜSTÜNE açılır, o da
 * yetmezse viewport'un alt/üst kenarına yaslanır; böylece panel hiçbir zaman ekran dışına taşmaz. */
export function ConfidenceLiRow({ result, baseClassName, children }: ConfidenceLiRowProps) {
  const anchorRef = useRef<HTMLLIElement>(null);
  const panelRef = useRef<HTMLDivElement>(null);
  const [anchorRect, setAnchorRect] = useState<DOMRect | null>(null);
  const [pos, setPos] = useState<{ left: number; top: number } | null>(null);

  useLayoutEffect(() => {
    if (!anchorRect || !panelRef.current) {
      setPos(null);
      return;
    }
    const panelHeight = panelRef.current.offsetHeight;
    const panelWidth = panelRef.current.offsetWidth;
    const left = Math.min(
      Math.max(VIEWPORT_MARGIN, anchorRect.left),
      window.innerWidth - panelWidth - VIEWPORT_MARGIN,
    );
    let top = anchorRect.bottom + 4;
    if (top + panelHeight > window.innerHeight - VIEWPORT_MARGIN) {
      const aboveTop = anchorRect.top - 4 - panelHeight;
      top = aboveTop >= VIEWPORT_MARGIN ? aboveTop : Math.max(VIEWPORT_MARGIN, window.innerHeight - panelHeight - VIEWPORT_MARGIN);
    }
    setPos({ left, top });
  }, [anchorRect]);

  useLayoutEffect(() => {
    if (!anchorRect) return;
    const close = () => setAnchorRect(null);
    window.addEventListener('scroll', close, true);
    window.addEventListener('resize', close);
    return () => {
      window.removeEventListener('scroll', close, true);
      window.removeEventListener('resize', close);
    };
  }, [anchorRect]);

  if (!result) {
    return (
      <li ref={anchorRef} className={`${baseClassName} px-3 py-2.5`}>
        {children}
      </li>
    );
  }

  return (
    <li
      ref={anchorRef}
      className={`${baseClassName} ${TIER_BORDER_CLASS[result.tier]} py-2.5 pl-2.5 pr-3`}
      onMouseEnter={() => anchorRef.current && setAnchorRect(anchorRef.current.getBoundingClientRect())}
      onMouseLeave={() => setAnchorRect(null)}
    >
      {children}
      {anchorRect &&
        createPortal(
          <div
            ref={panelRef}
            className="fixed z-[100] max-h-[70vh] w-80 overflow-y-auto rounded-lg border border-slate-200 bg-white p-3 text-xs shadow-xl"
            style={{
              left: pos ? pos.left : -9999,
              top: pos ? pos.top : -9999,
              visibility: pos ? 'visible' : 'hidden',
            }}
          >
            <div className="mb-2 flex items-center justify-between">
              <span className={`font-semibold ${TIER_TEXT_CLASS[result.tier]}`}>
                Güvenilirlik: {result.tierLabel}
              </span>
              <span className="font-bold text-slate-800">{result.score}/100</span>
            </div>
            <div className="space-y-1.5">
              {result.signals.map((signal) => (
                <div key={signal.key} className="flex items-start justify-between gap-2">
                  <div className="min-w-0">
                    <div className="font-medium text-slate-600">{signal.label}</div>
                    <div className="text-[11px] text-slate-400">{signal.reason}</div>
                  </div>
                  <span
                    className={
                      'shrink-0 rounded px-1.5 py-0.5 text-[11px] font-semibold ' +
                      (signal.points === signal.maxPoints
                        ? 'bg-emerald-50 text-emerald-700'
                        : signal.points === 0
                          ? 'bg-red-50 text-red-600'
                          : 'bg-amber-50 text-amber-700')
                    }
                  >
                    {signal.points}/{signal.maxPoints}
                  </span>
                </div>
              ))}
            </div>
          </div>,
          document.body,
        )}
    </li>
  );
}
