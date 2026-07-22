# Mesainame Yönetim Paneli — Kullanıcı Yönetimi Menüsü Yeniden Yapılandırma

## Bağlam

`AdminPage.tsx`'teki "Kullanıcı Yönetimi" sekmesinin sol menüsü ("KULLANICI YÖNETİMİ" grubu) şu an
5 öğeyi tek düz liste halinde gösteriyor: Çalışanlar, Kullanıcı Klasörü, Alan Eşlemeleri,
Organizasyon Şeması, Roller ve İzinler. Bu, kavramsal olarak birbirinden farklı üç grubu
(sistemin kendi çalışan verisi / Active Directory entegrasyonuna ait bileşenler / yetkilendirme)
aynı görsel seviyede karıştırıyor ve "Alan Eşlemeleri" ile "Organizasyon Şeması"nın aslında bir AD
dizinine bağımlı alt-detaylar olduğunu gizliyor.

Ayrıca iki bağımsız iyileştirme isteği bu çalışmaya dahil edildi:
1. Alan eşlemeleri (adı artık **"AD Attributes"**) şu an backend'de tamamen dizin-bağımsız
   (global) — her AD dizini için ayrı ayrı tanımlanabilir olmalı ve tanımı ilgili dizinin
   içinden yapılmalı.
2. Dizine özel "Kullanıcılar" listesi (`Kullanıcı Klasörü → [Dizin] → İşlemler → Kullanıcılar`),
   tüm dizinlerdeki kullanıcıları tek ekranda gösteren bağımsız bir "Kullanıcılar" menü öğesine
   taşınmalı; her satırda kullanıcının hangi dizinden geldiği görünmeli.

## Kapsam

- `AdminPage.tsx` sol menüsünün "KULLANICI YÖNETİMİ" grubunun ikiye ayrılması.
- `DirectoryAttributeMapping` domain entity'sine `DirectoryId` eklenmesi (backend: domain,
  persistence/migration, application/CQRS, API route değişikliği).
- AD Attributes ekranının sol menüden kaldırılıp dizin detay ekranına taşınması, yalnızca
  `Source === ActiveDirectory` dizinlerde gösterilmesi.
- Tüm dizinlerdeki kullanıcıları listeleyen yeni bağımsız "Kullanıcılar" bölümü; dizin filtresi ve
  "Dizin" sütunu.
- `DirectoryList.tsx`'teki dizine-özel "Kullanıcılar" linkinin kaldırılması.

## Kapsam Dışı

- Aynı fiziksel kişinin birden fazla AD dizininde görünmesi durumunda kayıtları birleştirme
  (identity resolution / deduplication). Sistemde bu mekanizma hiç yok; her `DirectoryUser` kaydı
  bağımsız bir dizine ait, kendi `Id`'siyle var olur. Bu çalışma kapsamında **birleştirme
  yapılmaz** — aynı kişi 2 dizinde tanımlıysa yeni "Kullanıcılar" listesinde 2 ayrı satır olarak
  görünür, her biri kendi "Dizin" sütun değeriyle ayırt edilir.
- Mevcut (global) AD Attributes verisinin dizinlere kopyalanması / veri taşıma — şu an sistemde
  tanımlı hiçbir AD Attributes kaydı olmadığı için bu konu tartışmasız kapanmıştır; migration
  sadece yeni `DirectoryId` sütununu ekler, backfill gerekmez.
- Roller ve İzinler placeholder'ının gerçek implementasyonu.

## Tasarım

### 1. Sol menü — `AdminPage.tsx`

`users` tab'ının tek `KULLANICI YÖNETİMİ` grubu ikiye ayrılır:

```ts
{
  key: 'users',
  label: 'Kullanıcı Yönetimi',
  groups: [
    {
      header: 'KULLANICI YÖNETİMİ',
      sections: [
        { key: 'employees', label: 'Çalışanlar', kind: 'employees' },
        { key: 'users', label: 'Kullanıcılar', kind: 'users' },
        { key: 'orgChart', label: 'Organizasyon Şeması', kind: 'orgChart' },
        { key: 'roles', label: 'Roller ve İzinler', kind: 'placeholder' },
      ],
    },
    {
      header: 'ACTIVE DIRECTORY',
      sections: [
        { key: 'userDirectory', label: 'Kullanıcı Klasörü', kind: 'userDirectory' },
      ],
    },
  ],
},
```

`SectionKind` union'ına `'users'` eklenir, `'attributeMappings'` kind'i tamamen kaldırılır (artık
sol menüden erişilmiyor — bkz. bölüm 3). `SectionContent`'e `case 'users': return <UsersSection />;`
eklenir, `case 'attributeMappings'` kaldırılır. `AttributeMappingsSection` importu `AdminPage.tsx`'ten
silinir (artık `UserDirectorySection.tsx` içinden kullanılacak — bkz. bölüm 3).

### 2. `DirectoryAttributeMapping` — dizine özel hale getirme (backend)

**Domain** (`DirectoryAttributeMapping.cs`): `DirectoryId` (Guid) property eklenir. `Create`
factory metodu `directoryId` parametresi alır ve `Guid.Empty` için `BusinessRuleValidationException`
fırlatır (mevcut `ValidateDirectoryId` desenine benzer şekilde, `DirectoryUser.cs`'teki gibi).

**Persistence**: `DirectoryAttributeMappingConfiguration.cs`'e `DirectoryId` sütunu ve
`Directory`'ye FK eklenir; `(DirectoryId, AdAttributeName)` üzerinde unique index tanımlanır (aynı
dizinde aynı AD alanının iki kez eşlenmesini engellemek için). Yeni bir EF Core migration
oluşturulur. Şu an tabloda hiç kayıt olmadığından backfill/veri taşıma adımı gerekmez.

**Application (CQRS)**:
- `GetAttributeMappingsQuery`'e `Guid DirectoryId` eklenir; handler `Where(m => m.DirectoryId ==
  request.DirectoryId)` filtresi uygular.
- `CreateAttributeMappingCommand`'e `DirectoryId` eklenir; handler `DirectoryAttributeMapping.Create(...)`
  çağrısına iletir.
- `UpdateAttributeMappingCommand`: `DirectoryId` değişmez (bir eşleme başka bir dizine
  taşınamaz), sadece mevcut alanlar güncellenir — değişiklik gerekmez.
- `DeleteAttributeMappingCommand`: değişiklik gerekmez (zaten sadece `Id` ile siliyor).

**API**: `DirectoryAttributeMappingsController`'ın route'u `api/v1/directoryattributemappings`
yerine `api/v1/directories/{directoryId}/attribute-mappings` olur (nested); tüm action'lar
route'tan gelen `directoryId`'yi command/query'ye iletir.

**Frontend**: `useAttributeMappings.ts` hook'u `useAttributeMappings(directoryId: string)` imzasına
geçer; query key `['directoryAttributeMappings', directoryId]` olur; mutation'lar
`directoryId`'yi de parametre olarak alır ve aynı query key'i invalidate eder.
`directoryAttributeMappings.ts` (api client) fonksiyonları nested route'a göre güncellenir.

### 3. AD Attributes ekranının taşınması

`AttributeMappingsSection.tsx` içinde:
- Metinlerde "Alan eşlemesi/eşlemeleri" ifadeleri **"AD Attribute"** olarak değiştirilir (başlık,
  boş durum mesajı, hata mesajları, form etiketleri gerektiği kadar — örn. "AD Attribute
  eklenemedi", "Henüz AD Attribute tanımlanmamış").
- "Bu eşlemeler tüm dizinler için ortaktır" açıklama cümlesi kaldırılır (artık dizine özel).
- Bileşen `{ directoryId: string }` prop'u alacak şekilde güncellenir, `useAttributeMappings(directoryId)`
  çağırır.

`UserDirectorySection.tsx`'teki `View` union'ına yeni bir varyant eklenir:

```ts
type View =
  | { kind: 'list' }
  | { kind: 'form'; directory: DirectoryDto | null }
  | { kind: 'userDetail'; directory: DirectoryDto; userId: string }
  | { kind: 'attributeMappings'; directory: DirectoryDto };
```

(`{ kind: 'users' }` — dizine özel kullanıcı listesi — kaldırılır, bkz. bölüm 4.)

`DirectoryList.tsx`'in "İşlemler" sütununa, yalnızca `directory.source === 1` (Active Directory)
olan satırlarda "AD Attributes" linki eklenir (mevcut "Senkronize Et" linkiyle aynı koşullu
render deseni): `onClick={() => onViewAttributeMappings(directory)}`. Internal Users dizininde bu
link hiç gösterilmez.

### 4. "Kullanıcılar" — tüm dizinler için tek liste

Yeni `UsersSection.tsx` bileşeni, mevcut `DirectoryUserList.tsx`'in tablo/arama/sayfalama
mantığını temel alır ama:
- `directoryId` prop'u almaz; `useDirectoryUsers({ directoryId: selectedDirectoryId ?? undefined, ... })`
  şeklinde çağrılır (backend `GetDirectoryUsersQuery.DirectoryId` zaten nullable — **backend
  değişikliği gerekmez**, `GetDirectoryUsersQueryHandler.cs` zaten `DirectoryName`'i join'leyip
  `DirectoryUserDto.DirectoryName`'e dolduruyor).
- Üstte, arama kutusunun yanına bir "Dizin" `<select>` filtresi eklenir: `useDirectories()` ile
  çekilen tüm dizinler + en başta "Tüm Dizinler" (`value=""`) seçeneği. Seçim değiştikçe
  `selectedDirectoryId` state'i güncellenir ve sayfa 1'e döner (mevcut arama kutusu davranışıyla
  tutarlı).
- Tabloya, "Kullanıcı Adı" sütunundan hemen sonra yeni bir **"Dizin"** sütunu eklenir:
  `<td>{user.directoryName}</td>` (`DirectoryUserDto.directoryName` zaten API'den geliyor).
- Satıra tıklayınca aynı `DirectoryUserCard` detay görünümü açılır; bileşen içinde yerel view
  state (`{ kind: 'list' } | { kind: 'detail'; userId: string }`) ile "listeye dön" gezinmesi
  korunur — mevcut `UserDirectorySection.tsx`'teki desenle aynı yapı, ama dizin bağımsız.
- `AdminPage.tsx`'e `case 'users': return <UsersSection />;` eklenir (bölüm 1'de tanımlandı).

`DirectoryList.tsx`'teki `onViewUsers` prop'u ve "Kullanıcılar" linki tamamen kaldırılır (dizine
özel kullanıcı görünümü artık yok); `UserDirectorySection.tsx`'teki `{ kind: 'users' }` view'ı
kaldırılır. `DirectoryUserList.tsx` dosyası silinir — mantığı, dizinden bağımsız hale getirilerek
`UsersSection.tsx`'e taşınır (aynı tabloyu iki ayrı yerde tutmamak için tek bileşene indirgenir).

### 5. Aynı kullanıcının birden fazla dizinde görünmesi

Kapsam Dışı bölümünde belirtildiği gibi: sistemde dizinler arası kullanıcı eşleştirme yok. Yeni
"Kullanıcılar" listesinde aynı kişi birden fazla AD'de tanımlıysa, her dizin için ayrı bir satır
görünür; "Dizin" sütunu bu satırları ayırt eder. Kullanıcı bir satıra tıkladığında açılan
`DirectoryUserCard`, yalnızca o **DirectoryUser** kaydının (yani o dizindeki hesabın) bilgilerini
gösterir — diğer dizindeki kaydı birleştirmez.

### Hata/Boş Durumlar

- "Kullanıcılar" sayfasında hiç dizin/kullanıcı yoksa mevcut `DirectoryUserList`'teki boş durum
  mesajları (arama sonucu yok / henüz kullanıcı yok) aynı mantıkla korunur, sadece dizin-özel
  ifade ("Bu dizinde…") "Sistemde…" şeklinde genelleştirilir.
- Dizin detayında "AD Attributes" linkine tıklanan bir dizin silinirse (edge case, aynı anda iki
  sekme senaryosu) mevcut genel hata yönetimi (`ApiError` mesajı) aynen kullanılır — özel bir
  durum eklenmez.

### Test Planı

- Backend: `DirectoryAttributeMapping` domain testine (varsa) `directoryId` doğrulaması eklenir;
  `CreateAttributeMappingCommandHandler` / `GetAttributeMappingsQueryHandler` testleri
  `directoryId` filtrelemesini kapsayacak şekilde güncellenir.
- Frontend: proje genelinde component-level otomatik test altyapısı yok; doğrulama `npx tsc
  --noEmit` ve tarayıcıda uçtan uca yapılır (önceki fazlarla tutarlı).
