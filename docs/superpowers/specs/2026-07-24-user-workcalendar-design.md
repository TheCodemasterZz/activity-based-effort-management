# WorkCalendarId'nin User'a Eklenmesi (Faz 2) — Tasarım

## Bağlam

[2026-07-24-directoryuser-to-user-rename-design.md](2026-07-24-directoryuser-to-user-rename-design.md)
dokümanında tanımlanan Employee/User birleştirme yol haritasının ikinci fazı. Faz 1'de
`DirectoryUser` → `User` yeniden adlandırması ve `Users` modülüne taşınması tamamlandı
(main'e merge edildi).

Bu faz, `Employee` entity'sinde bulunan `WorkCalendarId` (mesai takvimi ataması — hangi
günlerin çalışma günü sayılacağını belirler, kapasite/efor hesaplamalarının temeli) alanını
`User`'a taşır. Faz 3'te (`EmployeeId` → `UserId` geçişi) work log/kapasite hesaplamaları
`User.WorkCalendarId`'yi kullanmaya başlayacak; bu faz sadece alanın var olmasını ve doğru
şekilde yönetilebilmesini sağlar.

## Neden nullable, neden otomatik atama yok

`Employee` oluşturulurken `WorkCalendarId` zorunludur çünkü oluşturma admin tarafından elle
yapılan bir işlemdir. `User` ise çoğunlukla **AD senkronuyla otomatik** oluşur (200-300
kullanıcı tek seferde) — senkron sırasında kimse takvim seçmez. Sabit bir varsayılan takvim
otomatik atamak yanlış kapasite hesaplamalarına yol açabilir (ör. yarı zamanlı/vardiyalı bir
çalışan yanlışlıkla "Standart" takvime düşerse). Bu yüzden:

- `User.WorkCalendarId` **nullable**'dır; AD senkronu ve internal kullanıcı oluşturma bu alanı
  hep boş bırakır.
- Boş kalması sessizce göz ardı edilmez: senkron sonrası mevcut bildirim sistemine bir kayıt
  düşer, admin ekranında filtrelenip toplu/tekil atanabilir.
- **Faz 3'e bırakılan kapsam**: "takvimsiz kullanıcı efor/plan giremez" kuralı burada
  eklenmiyor — çünkü LogWork/PlanWork komutları hâlâ `Employee`'yi kullanıyor, `User`'ı değil
  (bkz. `WorkLogValidationHelper.ValidateAsync` hâlâ `employeeId` parametresi alıyor). Bu kural,
  Faz 3'te `EmployeeId` → `UserId` geçişi yapılırken doğal olarak eklenecek.

## Domain katmanı

`backend/src/EforTakip.Domain/Users/User.cs`:
- Yeni salt-okunur özellik: `public Guid? WorkCalendarId { get; private set; }`
- Yeni metod:
  ```csharp
  public void AssignWorkCalendar(Guid workCalendarId)
  {
      if (workCalendarId == Guid.Empty)
          throw new BusinessRuleValidationException("Mesai takvimi seçilmelidir.");

      WorkCalendarId = workCalendarId;
  }
  ```
- `CreateFromActiveDirectory` ve `CreateInternal` metodlarına parametre **eklenmez** —
  `WorkCalendarId` oluşturmada hep `null` başlar, sadece `AssignWorkCalendar` ile sonradan
  atanır.

## Persistence katmanı

- `UserConfiguration.cs`: `WorkCalendarId` nullable `uuid` kolonu, `WorkCalendars` tablosuna
  FK, `OnDelete(DeleteBehavior.Restrict)` — `EmployeeConfiguration`'daki desenle birebir aynı
  (bir mesai takvimi, ona atanmış kullanıcı varken silinemez).
- Yeni migration: `AddColumn` (nullable, default yok) + `AddForeignKey` + `CreateIndex`. Mevcut
  kullanıcı verisine dokunmaz, veri kaybı riski yok (sadece yeni nullable kolon ekleniyor).

## Application & API katmanı

Yeni `Users` modülü altında:

- **`AssignWorkCalendarCommand(Guid UserId, Guid WorkCalendarId)`** → handler, kullanıcıyı
  bulur, `AssignWorkCalendar` çağırır, kaydeder. Validator: her iki alan da boş olamaz.
- **`BulkAssignWorkCalendarCommand(IReadOnlyCollection<Guid> UserIds, Guid WorkCalendarId)`** →
  handler, belirtilen tüm kullanıcıları tek sorguda çeker, hepsine `AssignWorkCalendar` çağırıp
  tek `SaveChangesAsync` ile kaydeder. Validator: `UserIds` boş olamaz, `WorkCalendarId` boş
  olamaz.
- **`GetUsersQuery`**: yeni opsiyonel `OnlyMissingWorkCalendar` (`bool?`) filtresi —
  `true` ise `WorkCalendarId == null` olan kullanıcılar listelenir.
- **`UserDto` / `UserDetailDto`**: `WorkCalendarId` (`Guid?`) ve `WorkCalendarName` (`string?`,
  join ile doldurulur) alanları eklenir.

`UsersController`:
- `POST /api/v1/users/{id}/work-calendar` → `AssignWorkCalendarCommand`
  (`Permissions.User.Manage`)
- `POST /api/v1/users/work-calendar/bulk` → `BulkAssignWorkCalendarCommand`
  (`Permissions.User.Manage`)

## Bildirim tetikleme

`SyncDirectoryCommandHandler.Handle` sonunda, `SaveChangesAsync`'ten **önce** — o dizindeki
(hem yeni eklenen hem zaten var olan) `WorkCalendarId == null` olan aktif kullanıcı sayısı
hesaplanır. Sayı `> 0` ise:

```csharp
db.Notifications.Add(Notification.Create(
    $"'{directory.Name}' dizininde {missingCalendarCount} kullanıcının mesai takvimi atanmamış."));
```

Aynı `unitOfWork.SaveChangesAsync` çağrısıyla birlikte kaydedilir (ekstra round-trip yok).
Internal kullanıcı oluşturma (`CreateInternalUserCommandHandler`) tek kullanıcılık, bilinçli bir
admin işlemi olduğu için bildirim tetiklemez — admin isterse aynı ekrandan direkt atayabilir.

## Frontend

`UsersSection.tsx`:
- "Takvimsiz" filtre seçeneği (checkbox), `onlyMissingWorkCalendar` query param'ını kullanır.
- Tabloya "Mesai Takvimi" kolonu eklenir (`workCalendarName`, boşsa "Atanmamış" rozeti).
- Satır bazlı: tek kullanıcıya takvim atama aksiyonu (dropdown/modal, mevcut
  `WorkCalendarCard`/seçim bileşenleri varsa onlar kullanılır).
- Çoklu seçim (checkbox'lar) + "Seçilenlere takvim ata" toplu aksiyon butonu, seçilen takvimi
  `POST /work-calendar/bulk`'a gönderir.

`api/users.ts`: `assignWorkCalendar(userId, workCalendarId)` ve
`bulkAssignWorkCalendar(userIds, workCalendarId)` fonksiyonları eklenir.
`api/types.ts`: `UserDto`/`UserDetailDto`'ya `workCalendarId`/`workCalendarName` eklenir.

## Doğrulama

1. `dotnet build` + `dotnet test` (tüm suite) yeşil olmalı.
2. Bu worktree'deki gerçek PostgreSQL'e migration uygulanır; mevcut `admin` kullanıcısının
   `WorkCalendarId`'sinin `null` kaldığı doğrulanır (veri kaybı/varsayım yok).
3. Tarayıcıda: Kullanıcılar ekranında "Takvimsiz" filtresi, tekil ve toplu atama akışı manuel
   test edilir; senkron sonrası bildirim oluştuğu (mevcut AD dizini varsa) doğrulanır.
4. `npm run build` + `npm run lint` hatasız geçmeli.

## Kapsam dışı (bu fazda yapılmayacaklar)

- LogWork/PlanWork'te "takvimsiz kullanıcı giremez" kuralı — Faz 3'e ait.
- `Employee.WorkCalendarId`'ye hiç dokunulmuyor, `Employee` entity'si aynı kalıyor.
- Kullanıcı düzenleme ekranında başka alanların (email, isim vb.) güncellenmesi bu fazın
  kapsamında değil — sadece mesai takvimi ataması.
