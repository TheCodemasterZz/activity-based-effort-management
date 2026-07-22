# Kod İçine Gömülü Kriterler — Yapılandırılabilir Hale Getirme Analizi

Sistemde onay süreleri gibi bazı kriterler doğrudan koda gömülü. Bu doküman, hangilerinin
sistemden (admin ekranı/ayar) yapılandırılabilir hale getirilebileceğinin analizidir.

## Kategori A — Zaten API var, sadece yönetim ekranı eksik (en düşük efor)

| Kriter | Nerede | Şu An | Not |
|---|---|---|---|
| Resmi Tatiller | `HolidaysController` (POST zaten var) | Admin sayfasında sadece **listeleniyor**, ekleme formu yok | Backend hazır, sadece bir form eklemek yeterli |
| Mesai Takvimleri (çalışma saatleri) | `WorkCalendarsController` (POST zaten var) | Admin sayfasında **backend'den bile çekilmiyor** — frontend'de sabit/hardcoded bir dizi olarak gösteriliyor | En kötü durum: hem UI hem gerçek veri bağlantısı eksik — bu aslında bir bug, ayar meselesinden önce düzeltilmeli |

## Kategori B — Backend'de kod içine gömülü iş kuralları (backend değişikliği gerekir)

| Kriter | Nerede | Şu An | Yapılandırılabilir Olursa |
|---|---|---|---|
| Onay dönemi tipi | `ApprovalPeriodType` enum'ı | Sadece `Weekly` değeri var, başka seçenek yok | Aylık/2 haftalık onay seçeneği eklenebilir hale gelir |
| Onay haftası Pazartesi'den başlamalı | `CreateWorkLogApprovalCommandValidator` | Sabit kural (`DayOfWeek.Monday`) | Kurum takvimine göre hafta başlangıcı (ör. Pazar) seçilebilir olur |
| Günlük efor saati üst sınırı | `LogWorkCommandValidator`, `UpdateWorkLogCommandValidator`, `EmployeeWorkLog.cs` | Sabit **24 saat** | Kurumun kendi "günlük maksimum" kuralını girebilmesi |
| Açıklama karakter sınırı | Aynı validator'lar | Sabit **1000 karakter** | Düşük öncelik, pek kritik değil |
| Sayfalama üst sınırı | `PaginationParams.MaxPageSize` | Sabit **5000** | Bu bir iş kriteri değil, teknik güvenlik sınırı — yapılandırılabilir olması **önerilmez** |

## Kategori C — Frontend'de gömülü renklendirme eşikleri

| Kriter | Nerede | Şu An | Yapılandırılabilir Olursa |
|---|---|---|---|
| Kapasite "Dolu" sayılma sınırı | `CapacityManagementPage.tsx` → `computeCellStatus` | Boşluk kapasitenin **%10**'unun altındaysa "Dolu" sayılıyor (sabit) | Kurumun kendi tolerans eşiğini girebilmesi |
| Planlama Doğruluğu sapma eşikleri | `PlanningAccuracyTable.tsx` → `varianceStatus` | **±%10** iyi, **%10-30** orta, **>%30** kötü (sabit) | Kurumun "kabul edilebilir sapma" tanımını kendi yapması |

## Öneri

Bunları tek tek "her biri ayrı bir ayar ekranı" yapmak yerine, Admin sayfasına yeni bir
**"Kriterler / Ayarlar"** sekmesi eklemek daha sürdürülebilir olur — backend'de tek bir
`SystemSettings` (anahtar-değer) tablosu, frontend'de tek bir form.

**Önerilen öncelik sırası:**

1. **Mesai Takvimleri'ni gerçek API'ye bağlamak** (şu an sahte veri gösteriyor — bir ayar
   meselesi değil, doğrudan bir bug fix).
2. **Resmi Tatil ekleme formu** (backend hazır, hızlı kazanım).
3. **Kapasite/Planlama Doğruluğu eşikleri** (yeni `SystemSettings` altyapısını ilk kullanan
   ayarlar).
4. **Onay dönemi tipi / hafta başlangıcı** (daha büyük bir domain değişikliği, en sona
   bırakılabilir).
