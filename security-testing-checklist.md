# Statik/Dinamik Kod Analizi — 0 Bulguyla Geçme Kontrol Listesi

Bu doküman, siber güvenlik ekibinin yapacağı statik (SAST) ve dinamik (DAST) kod analizi
taramalarından mümkün olduğunca az/sıfır bulguyla çıkmak için Mesainâme (efor_takip_yazilimi)
özelinde yapılması gerekenleri listeler.

## Durum özeti (bu doküman yazıldığı tarihte)

- Sistem şu an **Test Mode**'da çalışıyor (EF Core InMemory, gerçek Neon/PostgreSQL bağlantısı yok).
- **Authentication/Authorization katmanı henüz yok** — bu, hem SAST hem DAST için en büyük risk.
- SQL injection ve XSS'e karşı mimari zaten güçlü (aşağıda açıklanıyor) — bunlar korunmalı, bozulmamalı.

## Statik Analiz (SAST) Öncesi Yapılacaklar

- [ ] **Secrets taraması**: `appsettings*.json` dosyalarında gerçek connection string/API key
      olmadığından emin ol; prod'a geçerken bunlar ortam değişkenine veya bir Key Vault/Secrets
      Manager'a taşınmalı.
- [ ] **SQL injection kontrolü**: Backend'de raw SQL kullanılan yer olmadığını doğrula
      (`grep -r "FromSqlRaw\|ExecuteSqlRaw" backend/src`) — şu an tamamen EF Core LINQ üzerinden
      çalışıyor, bu paternin korunması SAST'in en sık yakaladığı bulgu türünü baştan eler.
- [ ] **XSS kontrolü**: Frontend'de `dangerouslySetInnerHTML` kullanılmadığını doğrula
      (`grep -r dangerouslySetInnerHTML frontend/src`) — React varsayılan olarak JSX çıktısını
      escape ediyor, bu korunmalı.
- [ ] **Dependency (bağımlılık) taraması CI'ya eklenmeli**:
  - [ ] Backend: `dotnet list package --vulnerable` CI pipeline'ında çalıştırılsın.
  - [ ] Frontend: `npm audit` CI pipeline'ında çalıştırılsın.
  - [ ] Dependabot veya Renovate açılsın (otomatik güncelleme PR'ları için).
- [ ] **Authentication/Authorization eklenmeli**: Her API endpoint'ine `[Authorize]` + rol bazlı
      yetkilendirme eklenmeden hem SAST ("broken access control" / OWASP #1) hem DAST bulguları
      kaçınılmaz olur.
- [ ] **CORS policy kontrolü**: `Program.cs`'teki CORS ayarının prod'da `AllowAnyOrigin()`
      olmadığından, açık bir domain listesiyle sınırlı olduğundan emin ol.
- [ ] **Loglama kontrolü**: Serilog çıktısında şifre/token gibi hassas veri loglanmadığından
      emin ol.
- [ ] **Statik analiz aracı CI'ya bağlansın**:
  - [ ] Backend: Roslyn analyzers (`<EnableNETAnalyzers>true</EnableNETAnalyzers>`) + isteğe
        bağlı `dotnet-sonarscanner`.
  - [ ] Frontend: `eslint-plugin-security` + mevcut ESLint config.

## Dinamik Analiz (DAST) Öncesi Yapılacaklar

- [ ] **Authentication/Authorization** (yukarıdaki madde) — DAST canlı API'ye saldırı senaryoları
      dener; auth yoksa "her şey herkese açık" tek bulgusu her taramayı kırmızıya boyar.
- [ ] **Rate limiting eklenmeli** — brute-force/DoS bulgusu için `Microsoft.AspNetCore.RateLimiting`
      middleware'i eklenmeli.
- [ ] **HTTPS zorunluluğu** — prod ortamında `UseHsts()` + `UseHttpsRedirection()` aktif olmalı.
- [ ] **Input validation genişletilmeli** — FluentValidation zaten her command'da var (iyi durumda);
      DAST özellikle path/query param'larda (`GetById`, sayfalama parametreleri) sınır-dışı/malformed
      değer dener. `PaginationParams` gibi yerlerdeki guard'lar genişletilmeli.
- [ ] **Generic error handling** — Development ortamı dışında `UseExceptionHandler` stack
      trace/iç detay sızdırmayan, generic bir hata mesajı dönmeli.

## Öncelik Sırası

1. Authentication/Authorization ekle (en kritik — diğer her şeyin önkoşulu).
2. Secrets'ı ortam değişkenine/Key Vault'a taşı.
3. CI'ya dependency taraması + SAST aracı ekle.
4. Rate limiting + HSTS ekle.
5. Generic error handling'i doğrula.

> Not: Mevcut kod kalitesi (parametreli sorgular, React'in otomatik escape'i, FluentValidation
> katmanı) zaten sağlam bir temel oluşturuyor — asıl büyük risk authentication eksikliği.
