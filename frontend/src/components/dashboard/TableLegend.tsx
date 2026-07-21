import type { CSSProperties } from 'react';

interface LegendItem {
  swatchClass: string;
  label: string;
  swatchStyle?: CSSProperties;
}

/** Tablodaki tam onay hücreleriyle birebir aynı diyagonal çizgi deseni (bkz. WorkLogTable
 * APPROVED_STRIPE_STYLE) — lejantta da günün rengi korunuyormuş izlenimini vermek için
 * nötr bir zemin (slate-100) üzerinde gösterilir. */
const APPROVED_STRIPE_SWATCH_STYLE: CSSProperties = {
  backgroundImage:
    'repeating-linear-gradient(45deg, rgba(13,148,136,0.3) 0px, rgba(13,148,136,0.3) 2px, transparent 2px, transparent 9px)',
};

const LEGEND_ITEMS: LegendItem[] = [
  { swatchClass: 'bg-white border border-slate-300', label: 'Normal Gün' },
  { swatchClass: 'bg-amber-200 border border-amber-400', label: 'Bugün' },
  { swatchClass: 'bg-red-200 border border-red-400', label: 'Resmi Tatil' },
  { swatchClass: 'bg-slate-300 border border-slate-400', label: 'Hafta Sonu' },
  {
    swatchClass: 'bg-slate-100 border border-teal-500',
    label: 'Onaylı (gün rengi korunur, çizgili)',
    swatchStyle: APPROVED_STRIPE_SWATCH_STYLE,
  },
  { swatchClass: 'bg-teal-200 border border-teal-400', label: 'Kısmen Onaylı' },
  { swatchClass: 'bg-violet-300 border border-violet-500', label: 'İzinli (Tam Gün)' },
  { swatchClass: 'bg-violet-100 border border-violet-300', label: 'İzinli (Kısmi/Saatlik)' },
];

/** Efor tablosundaki hücre arka plan renklerinin ne anlama geldiğini gösteren yatay lejant. */
export function TableLegend() {
  return (
    <div className="mb-3 flex flex-wrap items-center gap-x-6 gap-y-2 text-sm font-medium text-slate-600">
      {LEGEND_ITEMS.map((item) => (
        <div key={item.label} className="flex items-center gap-2">
          <span
            className={`h-4 w-4 shrink-0 rounded ${item.swatchClass}`}
            style={item.swatchStyle}
          />
          <span>{item.label}</span>
        </div>
      ))}
    </div>
  );
}
