# Performans Testi Rehberi

Mesainâme (efor_takip_yazilimi) için performans testi nasıl yapılır — araç önerileri ve bu kod
tabanına özgü somut test senaryoları.

## Test Edilecek İki Farklı Yük Profili

- **API yükü**: Log Work/Plan Work sayfaları `pageSize: 1000` gibi büyük "tek seferde her şeyi
  çek" istekleri atıyor. Gerçek DB'ye (Neon/PostgreSQL) geçildiğinde bu sorgu paternleri kritik
  hale gelir.
- **Frontend render yükü**: `WorkLogTable` / `CapacityManagementPage` gibi büyük tabloların çok
  satır/sütunla (ör. 100+ çalışan × 31 gün) render performansı.

## Araç Önerileri

- [ ] **k6** (Grafana) veya **Apache JMeter** kurulup kritik endpoint'ler için script yazılsın
      (ör. `GET /api/v1/EmployeeWorkLogs?pageSize=1000`), artan eşzamanlı kullanıcı (10→50→200 VU)
      ile yük verilip p95/p99 latency ve hata oranı ölçülsün.
- [ ] **dotnet-counters** (`dotnet-counters monitor -p <pid>`) ile backend'de CPU/GC/thread pool
      doygunluğu gerçek zamanlı izlensin; prod'da Application Insights veya Prometheus+Grafana
      kalıcı çözüm olarak kurulsun.
- [ ] **Lighthouse** / **React DevTools Profiler** ile frontend sayfa yükleme süresi ve gereksiz
      re-render'lar (ör. `CapacityManagementPage`'deki `useMemo` bağımlılıkları doğru mu) tespit
      edilsin.

## Bu Kod Tabanına Özgü Test Senaryoları

- [ ] **25 → 200+ çalışan ölçeklendirme testi**: `TestDataSeeder.cs`'teki çalışan sayısı geçici
      olarak artırılıp (ör. 200), Kapasite Yönetimi sayfasının `employeeColumnStats`
      hesaplamasının (çalışan × gün bazlı iç içe döngü, O(çalışan×gün) karmaşıklığında) tarayıcıda
      donmaya yol açıp açmadığı gözlemlensin. Donma varsa hesaplama bir web worker'a taşınmalı.
- [ ] **Tam ay + çoklu-log senaryosu**: Mock veri artık günde tek büyük kayıt yerine 15dk-2sa'lık
      3-5 küçük log üretiyor (25 kişi × ~22 gün × ~4 log ≈ 2000+ satır/ay). Gerçek Postgres'e
      geçildiğinde `GetEmployeeWorkLogs` sorgusunun `EXPLAIN ANALYZE` ile index kullandığı
      doğrulansın — `(EmployeeId, EntryType, WorkDate)` composite index zaten tanımlı.
- [ ] **Eşzamanlı onay/log girişi**: Birden fazla kullanıcının aynı haftayı aynı anda onaylamaya
      çalışması (`WorkLogApprovalGuard` çakışma kontrolü) yük altında race condition'a yol açar mı
      — k6 ile eşzamanlı POST isteği gönderilip test edilsin.

## Yol Haritası

1. k6 ile baseline (mevcut Test Mode/InMemory) latency ölç.
2. Gerçek Postgres/Neon'a geçtikten sonra aynı testi tekrarlayıp DB gecikmesinin etkisini gör.
3. Darboğaz bulunursa (index eksik, N+1 sorgu, gereksiz `.Include()`) hedefli optimizasyon yap.

> Not: Performans testini "0 hata" hedefiyle değil, kabul edilebilir bir p95 latency eşiğiyle
> (ör. <500ms) planlamak daha gerçekçi olur.
