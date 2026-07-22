interface ErrorStateProps {
  title?: string;
  description?: string;
}

/** Bir liste/tablonun veri çekme sorgusu başarısız olduğunda gösterilen tek, standart hata
 * kartı — daha önce sayfa başına farklı farklı (bazen sadece tek satır kırmızı metin, bazen
 * başlıksız) yazılmıştı; artık her ekran aynı şablonu kullanıyor (iş kuralı). */
export function ErrorState({
  title = 'Veriler yüklenemedi',
  description = 'Sunucudan yanıt alınamadı. Bağlantınızı kontrol edip tekrar deneyin.',
}: ErrorStateProps) {
  return (
    <div className="flex items-center gap-3 rounded-xl border border-red-200 bg-red-50 p-6 text-red-700">
      <span className="text-xl">⚠</span>
      <div>
        <div className="font-semibold">{title}</div>
        <div className="text-sm text-red-600">{description}</div>
      </div>
    </div>
  );
}
