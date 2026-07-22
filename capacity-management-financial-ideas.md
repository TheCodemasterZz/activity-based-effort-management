# Kapasite Yönetimi — Finansal Konular (İleride Değerlendirilecek)

Runn.io'nun "Resource Utilization" yazısından ilham alan, kapasite yönetimi bölümüne finansal
boyut katacak iki öneri. Şimdilik uygulanmıyor — ileride ele alınmak üzere burada saklanıyor.

## 1) Faturalandırılabilir vs Faturalandırılamaz Kullanım Ayrımı

**Açıklama**: Projelere "Faturalandırılabilir mi?" (billable) alanı eklenip, Kapasite
Yönetimi'ndeki Kullanım (%) grafiğinde "Toplam Kullanım" ve "Faturalandırılabilir Kullanım"
olarak iki ayrı çizgi/metrik gösterilmesi.

**Neden**: Runn makalesine göre bu iki metrik farklı anlamlar taşır — toplam kullanım genel
doluluğu, faturalandırılabilir kullanım ise doğrudan gelire dönüşen kısmı gösterir. Optimal
hedefler de farklıdır: Toplam Kullanım %80-100, Faturalandırılabilir Kullanım %70-80.

**Gerektirdiği değişiklik**: `Project` domain entity'sine `IsBillable` (bool) alanı eklenmesi,
mevcut Kullanım (utilization) hesaplama mantığının bu alana göre filtrelenebilir hale gelmesi.

## 2) Mali Etki Paneli

**Açıklama**: Çalışanlara saatlik ücret (`HourlyRate`) alanı eklenip, Kapasite Yönetimi
sayfasında "kullanım %1 artarsa ek gelir ~X TL" gibi basit bir hesaplama/özet kartı gösterilmesi.

**Neden**: Runn makalesindeki örnek — "300 kişilik bir ekiple saatlik ücret $200 ise, kullanımda
tek puanlık artış 1.248.000 dolarlık ek gelir sağlayabilir" — kullanım metriklerini soyut bir
yüzdeden somut bir iş kararına dönüştürüyor.

**Gerektirdiği değişiklik**: `Employee` domain entity'sine `HourlyRate` (decimal, opsiyonel/
hassas veri — yetkilendirme gerektirir) alanı eklenmesi, yeni bir hesaplama kartı/bileşeni.

**Not**: Bu iki öneri de maaş/ücret gibi hassas veri içerdiğinden, uygulanmadan önce kimin bu
bilgiyi görebileceği (yetkilendirme/rol bazlı erişim) ayrıca düşünülmeli — sistemde şu an
authentication/authorization katmanı olmadığı için bu, `security-testing-checklist.md`'deki
"Authentication/Authorization eklenmeli" maddesiyle de bağlantılı.
