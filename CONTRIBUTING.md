# Katkı Rehberi

Bu doküman, 2 kişilik geliştirme ekibinin `activity-based-effort-management` (Mesainâme)
reposunda birbirinin kodunu ezmeden, çakışmaları en aza indirerek çalışabilmesi için
gerekli kuralları ve alışkanlıkları tanımlar.

## Branch stratejisi

- **`main` korumalı.** Doğrudan push kapalı — GitHub'da branch protection kuruldu:
  - PR olmadan `main`'e push edilemez.
  - Merge edilmeden önce en az **1 onay (review)** zorunlu.
  - Yeni commit push edildiğinde eski onaylar otomatik düşer (`dismiss_stale_reviews`).
  - Force-push ve branch silme `main` üzerinde kapalı.
- Her iş bir **feature branch**'te yapılır, `main`'den açılır:
  ```
  feature/<kısa-açıklama>       ör. feature/plan-work-sayfasi
  fix/<kısa-açıklama>           ör. fix/onay-renklendirme
  chore/<kısa-açıklama>         ör. chore/gitignore-guncelle
  ```
- Bir branch **tek bir konuya** odaklanır. "Plan Work sayfası" ve "SSO entegrasyonu" aynı
  branch'te olmaz — ayrı PR'lar, ayrı review'lar, ayrı merge'ler.

## Çakışmayı önlemek — asıl kural

İki kişi aynı anda çalışırken en büyük risk **aynı dosyayı** paralel değiştirmek. Bunun için:

1. **İşe başlamadan önce** `main`'i güncelleyin ve yeni branch'i oradan açın:
   ```
   git checkout main
   git pull origin main
   git checkout -b feature/ne-yapiyorsan
   ```
2. **Günde en az bir kez** kendi branch'inizi `main`'in üzerine rebase edin (özellikle
   diğer kişinin PR'ı merge olduysa) — çakışmayı küçük parçalar halinde, erken görün:
   ```
   git fetch origin
   git rebase origin/main
   ```
3. **Kısa ömürlü branch'ler.** Bir PR ne kadar uzun açık kalırsa, `main` o kadar uzaklaşır
   ve çakışma riski o kadar büyür. Büyük bir işi (ör. Plan Work gibi) tek dev'de bile
   mantıklı alt-adımlara bölüp arka arkaya küçük PR'lar halinde merge edin.
4. **Kim ne üzerinde çalışıyor, konuşun.** Aynı dosyayı (ör. `ReportPage.tsx`,
   `WorkLogTable.tsx` gibi paylaşılan/merkezi dosyalar) aynı anda iki kişi değiştirmeyecekse
   en azından haberdar olun — GitHub Issues'ta "şu an X üzerinde çalışıyorum" notu bırakmak
   yeterli.
5. **Migration çakışması ayrı bir risk.** EF Core migration'ları sırayla üretilir; iki kişi
   aynı anda migration eklerse ikinci kişi `main`'i çekip migration'ını **yeniden** üretmeli
   (elle birleştirmeye çalışmayın). Bkz. aşağıdaki "Backend'e özel kurallar".

## Commit ve PR alışkanlıkları

- Commit mesajları **neden** yapıldığını anlatır, sadece ne değiştiğini değil (kod diff'i
  zaten "ne"yi gösteriyor).
- Bir PR, tek bir mantıksal değişikliği temsil eder. "Bu arada şunu da düzelttim" tarzı
  ilgisiz değişiklikleri ayrı PR'a taşıyın — review'ı zorlaştırıyor ve geri almayı
  (revert) riskli hale getiriyor.
- PR açıklamasına şunları yazın: ne değişti, neden, nasıl test edildi (bu projede genelde
  Playwright ile uçtan uca doğrulama yapılıyor — ekran görüntüsü/adım listesi eklemek
  review'ı hızlandırır).
- Review isteyen kişi, karşı taraftan **gerçek bir okuma** bekler — sadece "LGTM" değil,
  en azından değişen dosyaları gözden geçirin. 2 kişilik bir ekipte review'ın asıl amacı
  budur: birbirinizin kör noktalarını yakalamak.
- Merge yöntemi: **Squash and merge** önerilir (her PR, `main`'de tek bir temiz commit
  olarak görünür; feature branch içindeki "wip", "fix typo" gibi ara commit'ler `main`
  geçmişini kirletmez).

## Backend'e özel kurallar

- **Neon'a (gerçek PostgreSQL) asla bağlanılmaz, migration asla uygulanmaz.** Sistem
  `UseTestMode: true` ile EF Core InMemory sağlayıcısı üzerinde çalışır
  (`appsettings.Development.json`). Migration dosyaları yalnızca **üretilir** (`dotnet ef
  migrations add`), gerçek bir veritabanına `dotnet ef database update` ile **uygulanmaz**.
- Migration eklerken: `main`'i güncelleyip en son migration'ın üzerine kendi migration'ınızı
  ekleyin. İki kişi paralel migration ürettiyse, sonradan merge eden kişi kendi
  migration'ını silip `main`'deki son migration üzerine yeniden üretir.
- Yeni bir CQRS özelliği eklerken bu repodaki **mevcut deseni birebir takip edin**
  (ör. `WorkLogs/` veya `Projects/` klasör yapısı) — bkz. kök dizindeki `CLAUDE.md`.

## Frontend'e özel kurallar

- `npx tsc -b` **0 hata** vermeden PR açmayın.
- Paylaşılan bileşenleri (`WorkLogTable.tsx`, `SummaryCards.tsx`, `MqlFilterInput.tsx` gibi)
  değiştirirken, bu bileşeni kullanan **her sayfayı** (Work Log, Plan Work) gözden geçirin
  — bir sayfa için yapılan düzeltme diğerini görünmez şekilde bozabilir.
- Yeni bir sayfa/özellik eklerken gerçek tarayıcıda (Playwright veya elle) doğrulayın;
  sadece `tsc` geçmesi özelliğin çalıştığı anlamına gelmez.

## Kurulum

```
# Backend
cd backend/src/EforTakip.Api
dotnet run --urls http://0.0.0.0:5298

# Frontend
cd frontend
npm install
npm run dev -- --host 0.0.0.0 --port 5180
```

`frontend/.env.development` (git'e dahil değil, kendiniz oluşturun):
```
VITE_API_BASE_URL=http://localhost:5298
```
