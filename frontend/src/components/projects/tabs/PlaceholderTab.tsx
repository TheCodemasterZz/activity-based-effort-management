/** AdminPage'deki Placeholder ile aynı tasarım — henüz içeriği gelmemiş (Faz 2/3) sekmeler
 * için, sekme şeridinin tamamı Faz 1'de görünür kalsın diye. */
export function PlaceholderTab({ label }: { label: string }) {
  return (
    <div className="flex flex-col items-center justify-center rounded-xl border border-dashed border-slate-200 py-16 text-center">
      <div className="mb-2 text-3xl">🚧</div>
      <p className="text-sm font-medium text-slate-500">{label} sekmesi yakında eklenecek.</p>
    </div>
  );
}
