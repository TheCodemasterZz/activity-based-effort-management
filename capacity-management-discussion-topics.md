# Kapasite Yönetimi — Tartışılacak Konular (Sırayla Ele Alınacak)

Runn.io'nun kaynak kullanımı/kapasite raporu/kapasite tahminleme yazılarından çıkan öneri
listesinden, finansal olanlar (`capacity-management-financial-ideas.md`) hariç kalan maddeler.
Her biri uygulanmadan önce tasarımı tek tek konuşulacak — bu doküman sadece ne olduklarının
kaydı, henüz uygulama planı değil.

## 1) Kullanım (Utilization) Özet Kartı

**Açıklama**: Sayfanın üstüne "Toplam Kullanım %" özet kartı; %80-100 hedef bandına göre
yeşil/sarı/kırmızı renklendirme.

**İlham kaynağı**: Resource Utilization (formüller + optimal hedefler).
**Nereye eklenir**: Mevcut Kapasite Yönetimi sayfası, üst kısım.
**Efor**: Düşük.

## 2) Rol/Beceri Bazlı Kapasite Görünümü

**Açıklama**: Çalışana Rol/Beceri alanı eklenip, tabloyu kişi yerine rol bazında gruplayarak
(ör. "Backend Developer" satırı = o roldeki herkesin toplamı) gösterme seçeneği.

**İlham kaynağı**: Capacity Report (geniş açılı rapor, beceri filtreleri).
**Nereye eklenir**: Domain (Employee) + mevcut tablo + yeni Group-by.
**Efor**: Orta-Yüksek.

## 3) Ekip/Departman Roll-up Görünümü

**Açıklama**: Kişi bazlı "granüler" tablonun yanına, Runn'ın "wide-angle report" dediği
ekip/departman toplamlarını gösteren özet bir görünüm (işe alım/üst yönetim kararları için).

**İlham kaynağı**: Capacity Report.
**Nereye eklenir**: Yeni sekme/mod (mevcut tablonun üstünde toggle).
**Efor**: Orta.

## 4) Isı Haritası (Heat Map) Görünümü

**Açıklama**: Mevcut hücre-tablosunun alternatifi olarak, rol/ekip × dönem bazında yoğunluk
renk skalasıyla (koyu=aşırı dolu, açık=boş) sıkıştırılmış bir üst-düzey görünüm.

**İlham kaynağı**: Resource Utilization ("rol bazlı ısı haritaları").
**Nereye eklenir**: Kapasite sayfasında görünüm seçeneği.
**Efor**: Orta.

## 5) Senaryo/What-if Simülatörü

**Açıklama**: "Şu proje eklenirse", "şu kişi ayrılırsa/izne çıkarsa" gibi geçici/hayali
kayıtlarla kapasiteyi simüle edip DB'ye hiç yazmadan önizleme. Gerçek Actual/Planned/İzin
verisinin üzerine, sadece tarayıcı hafızasında yaşayan (backend'e gitmeyen) hayali kayıtlar
eklenir; mevcut kapasite hesaplama mantığı bunları gerçek veriyle birlikte işler. "Sıfırla"
denince hiçbir iz kalmadan silinir.

**Örnek senaryolar**:
- "Şu 3 kişi Ağustos'ta izne çıkarsa kapasitemiz nasıl görünür?"
- "Yeni bir proje eklenip 4 kişi haftada 15 saat ayırırsa kimde aşım oluşur?"
- "Şu kişi ayrılırsa boşta kalan iş kimde birikir?"

**İlham kaynağı**: Capacity Forecasting ("senaryo analizi yap").
**Nereye eklenir**: Yeni panel/modal.
**Efor**: Yüksek — listedeki en zor madde.
**Not**: "Yeni proje eklenirse" senaryosunu doğal şekilde denemek için önce **Proje Boru Hattı**
(madde 7) kavramının var olması gerekir — aksi halde denemek için bile gerçek bir Project kaydı
açmak gerekir, ki simülatörün önlemeye çalıştığı sürtünme tam da bu. "Kişi izni/ayrılması" gibi
senaryolar için ise Pipeline'a gerek yok, bağımsız çalışabilir.

## 6) Güven Seviyesi (Confidence) Etiketleme

**Açıklama**: Planlanan kayıtlara Kesin/Olası/Taslak gibi bir güven etiketi eklenip, grafikte
opaklık/doku ile ayırt edilmesi (Runn'ın "%60-70 kesinlikle planla" önerisi).

**İlham kaynağı**: Capacity Forecasting.
**Nereye eklenir**: Domain (WorkLog/Approval) + mevcut grafik dolgu mantığı.
**Efor**: Orta.

## 7) Proje Boru Hattı (Pipeline) Entegrasyonu

**Açıklama**: Henüz onaylanmamış/olası projeleri "Tentative" olarak işaretleyip kapasite
tahminine opsiyonel dahil etme — satış/proje boru hattıyla işbirliğini yansıtır.

**İlham kaynağı**: Capacity Forecasting ("satışla işbirliği yap").
**Nereye eklenir**: Domain (Project status) + Kapasite hesaplama.
**Efor**: Orta-Yüksek.
**Not**: Senaryo Simülatörü (madde 5) ile doğal olarak birleşiyor — sıralama tartışılırken önce
bunun ele alınması önerildi.

## 8) Kapasite Aşımı/Boşluk Bildirimleri

**Açıklama**: Bir kişi eşiği aşınca (Aşım) veya uzun süre boşta kalınca (düşük kullanım)
otomatik bildirim düşmesi — sistemde zaten bir bildirim zili altyapısı var.

**İlham kaynağı**: Resource Utilization (proaktif takip).
**Nereye eklenir**: Backend job + mevcut Notification sistemi.
**Efor**: Orta.

## 9) Veri Tazeliği Uyarısı

**Açıklama**: X gündür log girmemiş / planlaması güncellenmemiş kişileri tabloda küçük bir
ikonla işaretleme (tahminleme doğruluğunu artırmak için).

**İlham kaynağı**: Capacity Forecasting ("veri kalitesine yatırım yap").
**Nereye eklenir**: Mevcut tabloya küçük ekleme.
**Efor**: Düşük.

---

**Not**: "Planlama Doğruluğu Raporu" bu listede yer alan ilk madde olarak zaten tartışılıp
uygulandı (bkz. Planlama Doğruluğu sayfası) — burada tekrar listelenmiyor.
