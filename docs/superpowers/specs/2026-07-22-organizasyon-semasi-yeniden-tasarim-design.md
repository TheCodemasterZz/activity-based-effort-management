# Organizasyon Şeması — Yeniden Tasarım

## Bağlam

Organizasyon şeması özelliği ilk halinde `Ayarlar → Kullanıcı Yönetimi → Kullanıcı Klasörü →
[Dizin] → Kullanıcılar → Organizasyon Şeması` şeklinde, üç seviye derinlikte, seçili bir dizine
bağlı bir alt görünüm olarak eklenmişti. Kullanıcı geri bildirimi iki sorun ortaya koydu:

1. Konum: Kullanıcı Klasörü akışının içine gömülü olması yerine, Ayarlar sayfasının sol
   menüsünde ("Kullanıcı Klasörü", "Alan Eşlemeleri" ile aynı seviyede) kendi başına bir bölüm
   olması isteniyor. Dizin seçimi bu bölümün içinde yapılmalı.
2. Görsel: Mevcut yatay kutu-ağacı (`OrgChart.tsx`) düzeni geniş organizasyonlarda sayfanın
   yana doğru taşmasına neden oluyor.

## Kapsam

- `AdminPage.tsx`'in sol menüsüne yeni bir bölüm eklenmesi.
- Dizin seçici içeren yeni bir `OrgChartSection.tsx` bileşeni.
- Mevcut yatay kutu-ağacı yerine dikey/girintili, daraltılabilir bir ağaç görünümü.
- Bir düğüme tıklandığında kullanıcı kartının modal içinde açılması.
- Backend'de değişiklik yok — `GET /api/v1/directories/{id}/org-chart` endpoint'i ve
  `useOrgChart` hook'u aynen kullanılmaya devam eder.

## Kapsam Dışı

- Şema üzerinde sürükle-bırak ile yeniden düzenleme.
- Yazdırma / dışa aktarma (PDF, PNG vb.).
- Birden fazla dizinin şemasının tek ekranda birleştirilmesi.

## Tasarım

### 1. Navigasyon — AdminPage sol menüsü

`ADMIN_TABS` içindeki `users` sekmesinin `KULLANICI YÖNETİMİ` grubuna yeni bir `AdminSection`
eklenir:

```ts
{ key: 'orgChart', label: 'Organizasyon Şeması', kind: 'orgChart' }
```

`SectionContent` switch'ine `case 'orgChart': return <OrgChartSection />;` eklenir.

Kaldırılanlar:
- `UserDirectorySection.tsx` içindeki `{ kind: 'orgChart' }` view state'i ve buna bağlı geçişler.
- `DirectoryUserList.tsx`'teki "Organizasyon Şeması" linki (`onViewOrgChart` prop'u ve kullanımı).
- `OrgChart.tsx`'in mevcut yatay kutu-ağacı implementasyonu (yerine yeni ağaç bileşeni gelecek;
  dosya yeniden yazılır, `useOrgChart` hook'u ve backend değişmez).

### 2. `OrgChartSection.tsx` (yeni bileşen)

- `useDirectories()` ile dizin listesi çekilir, `source === 1` (Active Directory) olanlar
  filtrelenir. Internal Users dizini listelenmez (organizasyon şeması kavramı ona uygun değil).
- Üstte bir `<select>` — seçenekler filtrelenmiş AD dizinleri. Hiç AD dizini yoksa açıklayıcı bir
  boş durum mesajı gösterilir ("Önce Kullanıcı Klasörü'nden bir Active Directory dizini
  tanımlayın."). Varsayılan seçim: listedeki ilk AD dizini.
- Seçili dizin state olarak tutulur (`useState<string | null>`); değiştikçe alttaki ağaç bileşeni
  yeniden mount/fetch olur (`key={selectedDirectoryId}` ile `OrgChartTree`'ye).
- Seçili kullanıcı için modal state'i (`selectedUserId: string | null`) bu bileşende tutulur;
  ağaçtaki bir düğüme tıklanınca burada set edilir, modal `onClose` ile `null`'a döner.

### 3. Ağaç görünümü — dikey/girintili, daraltılabilir liste

`OrgChart.tsx` yeniden yazılır (mevcut `buildForest` mantığı — `managerId`'e göre gruplama,
döngü/yetim koruması — aynen korunur, sadece render katmanı değişir):

- Her düğüm bir satır: `[▸/▾ veya boşluk] [avatar] [görünen ad] [kullanıcı adı, soluk]`.
- Alt çalışanı olan düğümlerde daraltma oku (▸ kapalı / ▾ açık) solda görünür; tıklanınca yalnızca
  o dalın açık/kapalı durumu değişir (`Set<string>` collapsed-node-id state'i, component içinde).
- Varsayılan: tüm düğümler açık (collapsed set boş) — kullanıcı istediği dalı daraltır.
- Girinti: her derinlik seviyesinde `pl-6` (24px) artar; solda ince bir dikey kılavuz çizgisi
  (`border-l border-slate-200`) hiyerarşiyi görsel olarak belirginleştirir.
- Satırın (ok hariç) herhangi bir yerine tıklamak `onSelectUser(node.id)` çağırır — mevcut
  `AttributeRow`'daki "Kullanıcı" referans tıklaması ile aynı etkileşim hissi.
- Kapsayıcıda yatay taşma olmaz; genişlik ebeveyn kapsayıcıya (`w-full`) sabittir, gerekiyorsa
  yalnızca dikey scroll (`overflow-y-auto`) olur.
- Birden fazla kök (yetim yönetici — senkron filtresi dışında kalan) varsa, mevcut uyarı notu
  ("Birden fazla kök görünüyor…") korunur.

### 4. Kullanıcı kartı modalı

Yeni `DirectoryUserCardModal.tsx` (veya `OrgChartSection.tsx` içine gömülü küçük bir sarmalayıcı):
projedeki yerleşik modal deseni kullanılır (bkz. `ProjectDetailModal.tsx`):
`fixed inset-0 z-50 flex items-center justify-center bg-slate-900/40 p-4` overlay +
`max-w-lg rounded-xl bg-white p-6 shadow-xl` panel + sağ üstte ✕ kapatma butonu.

İçerik olarak mevcut `DirectoryUserCard` bileşeni kullanılır, ancak:
- `onBack` prop'u modalı kapatan `onClose`'a bağlanır (buton metni "← Kullanıcılara dön" yerine
  modalda gösterilmez; kapama sadece ✕ ile olur — `onBack`'i olmayan bir varyant için
  `DirectoryUserCard`'a `onBack?: () => void` opsiyonel hale getirilip başlık satırındaki geri
  butonu `onBack` varsa gösterilir).
- `onSelectUser` prop'u modaldaki `selectedUserId` state'ini günceller — böylece modal içinde
  "Yönetici" referansına tıklanınca modal kapanmadan içerik değişir (kullanıcılar arasında
  modal içi gezinme).

### Hata/Boş Durumlar

- Hiç AD dizini yok → seçici yerine açıklayıcı mesaj.
- Seçili dizinde "Kullanıcı" tipinde alan eşlemesi yok → mevcut `hasManagerMapping === false`
  mesajı korunur.
- Seçili dizinde hiç kullanıcı yok → mevcut "Bu dizinde henüz kullanıcı yok." mesajı korunur.

### Test Planı

- `OrgChart` ağaç kurma mantığı (`buildForest`) zaten saf bir fonksiyon — mevcut davranış
  (döngü/yetim koruması) değişmediği için yeniden test yazmaya gerek yok, sadece render katmanı
  değişiyor.
- Backend'de değişiklik olmadığı için mevcut `GetOrgChartQueryHandlerTests` yeterli.
- Frontend: bu projede component-level otomatik test altyapısı yok (mevcut org chart özelliği de
  test edilmemişti) — doğrulama, önceki fazlarda olduğu gibi tarayıcıda uçtan uca yapılacak.
