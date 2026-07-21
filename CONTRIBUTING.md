# Katkı Rehberi

Bu doküman, 2 kişilik geliştirme ekibinin `activity-based-effort-management` (Mesainâme)
reposunda birbirinin kodunu ezmeden, çakışmaları en aza indirerek çalışabilmesi için
gerekli kuralları ve alışkanlıkları tanımlar.

## Branch stratejisi

- **`main`'e doğrudan push serbest.** İki kişilik ekibin ikisi de PR açmadan doğrudan
  `main`'e push edebilir — GitHub'daki zorunlu PR/review kuralı kaldırıldı. Sadece şu iki
  koruma açık kalıyor (yanlışlıkla geçmişi bozmayı engellemek için):
  - Force-push `main` üzerinde kapalı.
  - Branch silme `main` üzerinde kapalı.
- Küçük/orta değişiklikler için doğrudan `main`'e commit+push yeterlidir. Büyük veya riskli
  bir işi (ör. Plan Work gibi çok dosyalı bir özellik) yine de bir **feature branch**'te
  yapıp PR ile merge etmek isterseniz bu hâlâ mümkün — PR artık zorunlu değil, seçime bağlı:
  ```
  feature/<kısa-açıklama>       ör. feature/plan-work-sayfasi
  fix/<kısa-açıklama>           ör. fix/onay-renklendirme
  chore/<kısa-açıklama>         ör. chore/gitignore-guncelle
  ```

## Çakışmayı önlemek — asıl kural

İki kişi aynı anda çalışırken en büyük risk **aynı dosyayı** paralel değiştirmek — PR zorunlu
olmadığı için bu artık daha kolay gerçekleşebilir, o yüzden aşağıdaki disiplin daha önemli:

1. **Push etmeden hemen önce** her zaman `main`'i çekin, sonra push edin — sırayla push eden
   ikinci kişi conflict'i hemen görür ve yerelde çözer:
   ```
   git pull origin main --rebase
   git push origin main
   ```
2. Büyük veya birden çok gün sürecek bir işi feature branch'te yapıyorsanız, **günde en az
   bir kez** `main`'in üzerine rebase edin — çakışmayı küçük parçalar halinde, erken görün:
   ```
   git fetch origin
   git rebase origin/main
   ```
3. **Sık ve küçük push'lar.** Değişiklik ne kadar uzun süre local'de/branch'te bekletilirse,
   `main` o kadar uzaklaşır ve çakışma riski o kadar büyür. Büyük bir işi (ör. Plan Work
   gibi) mantıklı alt-adımlara bölüp arka arkaya küçük commit'ler halinde push edin.
4. **Kim ne üzerinde çalışıyor, konuşun.** Aynı dosyayı (ör. `ReportPage.tsx`,
   `WorkLogTable.tsx` gibi paylaşılan/merkezi dosyalar) aynı anda iki kişi değiştirmeyecekse
   en azından haberdar olun — GitHub Issues'ta "şu an X üzerinde çalışıyorum" notu bırakmak
   yeterli.
5. **Migration çakışması ayrı bir risk.** EF Core migration'ları sırayla üretilir; iki kişi
   aynı anda migration eklerse ikinci kişi `main`'i çekip migration'ını **yeniden** üretmeli
   (elle birleştirmeye çalışmayın). Bkz. aşağıdaki "Backend'e özel kurallar".

## Commit alışkanlıkları

- Commit mesajları **neden** yapıldığını anlatır, sadece ne değiştiğini değil (kod diff'i
  zaten "ne"yi gösteriyor).
- Bir commit (veya push öncesi commit grubu), tek bir mantıksal değişikliği temsil eder.
  "Bu arada şunu da düzelttim" tarzı ilgisiz değişiklikleri ayrı bir commit'e taşıyın —
  geri almayı (revert) ve geçmişi okumayı kolaylaştırır.
- PR zorunlu olmadığı için kod review artık **isteğe bağlı** — ama riskli/karmaşık bir
  değişiklik yaptıysanız (ör. paylaşılan bir bileşeni veya migration'ı etkiliyorsa), push
  etmeden önce yine de bir PR açıp karşı taraftan hızlı bir göz atmasını istemek makul bir
  seçenek. Bunu yapmak isterseniz PR açıklamasına ne değişti, neden, nasıl test edildiğini
  yazın (bu projede genelde Playwright ile uçtan uca doğrulama yapılıyor).

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

- `npx tsc -b` **0 hata** vermeden `main`'e push etmeyin.
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
