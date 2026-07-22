# MS Project Fonksiyonları — Native Geliştirme Fikirleri (İleride Değerlendirilecek)

Microsoft Project ile entegrasyon (dosya import/senkron) yerine, MS Project'in bu sisteme gerçekten
karşılığı olan fonksiyonlarını **native** olarak eklemenin daha tutarlı olacağı değerlendirildi —
MS Project'in bağımlılık/kritik-yol tabanlı zamanlama modeli, bu sistemin gün bazlı saat-dağıtım
modeliyle örtüşmüyor. Şimdilik uygulanmıyor — ileride ele alınmak üzere burada saklanıyor.

## 1) Görev (Task)

**Açıklama**: Şu an Plan Work'te sadece "kişi + proje + aktivite + tarih + saat" var, bir görevin
*adı* yok. Yeni bir `Task` varlığı eklenir: isim (ör. "Login ekranı tasarımı"), başlangıç/bitiş
tarihi, tahmini süre, bir veya birden fazla kişiye atanabilme. Plan Work / Work Log kayıtları
isteğe bağlı olarak bir `TaskId`'ye bağlanabilir — böylece "bu görevin ne kadarı gerçekleşti"
sorusuna cevap verilebilir (Planlama Doğruluğu raporuyla da doğal olarak birleşir).

**Gerektirdiği değişiklik**: Yeni `Task` domain entity'si (Project'e bağlı), CQRS komut/sorguları,
`EmployeeWorkLog`'a opsiyonel `TaskId` alanı.

## 2) Gantt / Zaman Çizelgesi Görünümü

**Açıklama**: Task'ları yatay çubuklar olarak bir zaman ekseninde gösteren yeni bir görünüm —
proje bazında, her satır bir görev, çubuk = başlangıç-bitiş aralığı. Mevcut dönem/tarih
altyapısı (`MonthNavigator`, `dateUtils`) yeniden kullanılabilir.

**Gerektirdiği değişiklik**: (1) numaralı Task özelliği önkoşul. Yeni bir Gantt bileşeni
(muhtemelen recharts değil, özel bir SVG/div tabanlı zaman çizelgesi çizimi gerekir).

## 3) Kilometre Taşı (Milestone)

**Açıklama**: Süresi sıfır olan özel bir Task türü (başlangıç = bitiş), Gantt üzerinde elmas
ikonla gösterilen önemli bir tarih işareti (ör. "MVP Teslimi", "UAT Başlangıcı").

**Gerektirdiği değişiklik**: `Task` entity'sine `IsMilestone` (bool) alanı; Gantt görünümünde
özel render.

## 4) Bağımlılık Zinciri / Kritik Yol — Muhtemelen Gereksiz

**Açıklama**: "B görevi, A bitmeden başlayamaz" ilişkileri + bunlardan projenin en kısa süresini
hesaplayan algoritma (Critical Path Method).

**Neden kapsam dışı bırakıldı**:
- Tam bir zamanlama motoru gerektiriyor (topolojik sıralama + ileri/geri geçiş hesaplaması) —
  karmaşık, hataya açık, bakımı zor.
- Bu sistemin modeli "gün bazlı saat dağıtımı" üzerine kurulu, sıkı görev sıralaması üzerine değil.
- Şu ana kadar somut bir ihtiyaç belirtilmedi — MS Project'te olduğu için otomatik olarak gerekli
  sayılmadı.

**Karar**: 1-2-3 native olarak eklenebilir; 4 şimdilik uygulanmayacak, sadece gelecekte ihtiyaç
netleşirse yeniden değerlendirilecek.
