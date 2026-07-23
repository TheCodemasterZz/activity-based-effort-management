# Kurumsal Yazılım Geliştirme Standartları

Bu proje, kurumsal ölçekte sürdürülebilir, ölçeklenebilir, güvenli ve bakım yapılabilir bir
efor takip / planlama / yönetim yazılımıdır. Kod üretirken aşağıdaki kurallar esas alınır.

## Genel Yaklaşım

- Kod yazmadan önce mevcut solution yapısını analiz et.
- Mevcut mimariyi bozmadan geliştirme yap; yeni yapı önermeden önce mevcut yapıya uyum sağla.
- Üretilecek tüm kod production kalitesinde olmalı — geçici/kısa vadeli çözüm üretme.
- Gereksiz karmaşıklık oluşturma; okunabilirlik ve sürdürülebilirlik önceliklidir.

## Mimari Yaklaşım

Uygun olduğu durumlarda: Clean Architecture, Domain Driven Design (DDD), Katmanlı Mimari,
CQRS, Event Driven Architecture, Dependency Injection / Inversion of Control. Modüler
Monolith ile başlanır; mikroservise sadece gerçek bir gereksinim doğduğunda geçilir.
Farklı mimari yaklaşımlar gereksiz şekilde bir arada kullanılmaz.

## Yazılım Prensipleri

SOLID, DRY, KISS, YAGNI, Separation of Concerns, High Cohesion / Low Coupling,
Composition over Inheritance, Convention over Configuration, Fail Fast, Defensive Programming.

## Solution Yapısı

Katmanlar (yalnızca ihtiyaç doğduğunda eklenir — YAGNI):

- **Domain** — Entity, Value Object, Aggregate Root, Domain Event, Domain Service,
  Repository arayüzleri, Specification. İş kuralları mümkün olduğunca burada modellenir.
- **Application** — CQRS Command/Query + Handler (MediatR), DTO, Validation (FluentValidation),
  Application Service arayüzleri.
- **Infrastructure** — Dış sistem entegrasyonları, kimlik doğrulama altyapısı, dosya/eposta/
  mesajlaşma servisleri.
- **Persistence** — EF Core DbContext, Migration, Repository/Unit of Work implementasyonu.
- **API** — Controller, Middleware, versioning, Swagger. Controller içinde iş kuralı yazılmaz.
- **Contracts / Shared** — Katmanlar arası ortak sözleşmeler, sabitler.
- **Tests** — Unit ve Integration testler.

Katman ihlali yapılmaz; her sınıf doğru projede yer alır. Identity, Worker, Integration,
Reporting gibi projeler yalnızca gerçek bir ihtiyaç ortaya çıktığında eklenir.

## Tasarım Kalıpları

İhtiyaç halinde: Repository, Unit of Work, Specification, Factory, Builder, Strategy,
Mediator, Decorator, Adapter, Facade, Observer. Gereksiz soyutlama oluşturulmaz.

## API Standartları

REST standartları, doğru HTTP metod/durum kodu kullanımı, API versiyonlama, pagination,
filtreleme, sıralama desteği, Swagger/OpenAPI dokümantasyonu. Controller içinde iş kuralı yok.

## Doğrulama (Validation)

Çok katmanlı doğrulama: FluentValidation ile Request/Input Validation + Domain içinde
Business Rule Validation. Yalnızca istemci tarafı doğrulamasına güvenilmez.

## Hata Yönetimi

Merkezi hata yönetimi: Global Exception Middleware, RFC 7807 Problem Details, özel
Exception sınıfları, anlamlı hata mesajları. İç sistem bilgisi kullanıcıya gösterilmez.

## Loglama

Yapısal loglama (Serilog), Correlation Id, Audit Log, performans logları. Şifre, token ve
kişisel veriler loglanmaz.

## Güvenlik

Authentication/Authorization (JWT, OAuth2, OpenID Connect), Role/Claims Based Authorization.
SQL Injection, XSS, CSRF, Command Injection, bilgi sızdırma riskleri önlenir. Şifre ve
bağlantı bilgileri kod içinde tutulmaz — appsettings / environment variable / secret
management üzerinden yönetilir.

## Veri Erişimi

EF Core, Repository Pattern, Unit of Work, Migration, Optimistic Concurrency. Gereksiz
sorgu ve N+1 problemlerinden kaçınılır.

## Performans

Async/Await, CancellationToken, Caching (Redis varsa), Background Worker, batch işlemler —
okunabilirlik bozulmadan.

## Konfigürasyon

appsettings, Environment Variable, Options Pattern, Secret Management. Hiçbir gizli bilgi
kaynak kodunda tutulmaz.

## Kod Kalitesi ve İsimlendirme

Okunabilir, basit, test edilebilir, genişletilebilir, tutarlı kod. Kısa metotlar, tek
sorumluluklu sınıflar. Standart .NET isimlendirme (PascalCase/camelCase, Async son eki,
DTO/Request/Response/Command/Query/Handler). Anlamsız kısaltma kullanılmaz. Kod ile
açıklanabilecek durumlarda yorum satırı yazılmaz.

## Test

Kod test edilebilir olmalı: Unit Test, Integration Test, gerektiğinde Mock kullanımı.
İş kuralları bağımsız test edilebilir olmalıdır.

## Gözlemlenebilirlik ve DevOps

Health Check, Metrics, Distributed Tracing (OpenTelemetry), Structured Logging. Docker,
CI/CD (GitHub Actions) desteği — gerçek ihtiyaç doğduğunda eklenir.

## Kod Üretmeden Önce

1. İhtiyacı analiz et.
2. Mevcut solution yapısını incele.
3. Doğru katmanı belirle.
4. Gerekli tasarım kalıbını seç.
5. Gereksiz karmaşıklık oluşturma.
6. Production seviyesinde kod üret.
7. Mevcut mimariyi bozma.
8. Gerekiyorsa alternatif çözüm önerilerini belirt.

## Yeni Feature Eklerken İzin Kuralı

Yeni bir API endpoint'i veya işlem eklerken şu iki adım zorunludur:

1. `backend/src/EforTakip.Domain/Authorization/Permissions.cs` içinde ilgili modül sınıfına
   (yoksa yeni bir modül sınıfı açarak) bir izin sabiti ekle: `public const string X = "modul:x";`
2. Controller action'ına `[RequirePermission(Permissions.Modül.X)]` ekle.

Bunun dışında hiçbir adım (migration, seed, DB kaydı) gerekmez — izin kataloğu kodda yaşar.
`Permissions.All` reflection ile otomatik günceldir. Bir role `"modul:*"` (wildcard) izni
verilmişse o moduldeki her mevcut ve gelecekteki izin otomatik kapsanır; `IsSystemAdmin` rolü
her izni otomatik geçer.
