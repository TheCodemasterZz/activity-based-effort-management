using System.Text;
using System.Text.RegularExpressions;
using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Directories.Dtos;
using EforTakip.Application.Directories.Ldap;
using EforTakip.Domain.Directories;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.Notifications;
using EforTakip.Domain.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Directory = EforTakip.Domain.Directories.Directory;

namespace EforTakip.Application.Directories.Commands.SyncDirectory;

public sealed partial class SyncDirectoryCommandHandler(
    IApplicationDbContext db,
    IRepository<Directory> directoryRepository,
    ILdapService ldapService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<SyncDirectoryCommand, DirectorySyncResultDto>
{
    public async Task<DirectorySyncResultDto> Handle(
        SyncDirectoryCommand request, CancellationToken cancellationToken)
    {
        var directory = await directoryRepository.GetByIdAsync(request.DirectoryId, cancellationToken)
            ?? throw new NotFoundException(nameof(Directory), request.DirectoryId);

        if (directory.Source != DirectorySource.ActiveDirectory)
            throw new BusinessRuleValidationException("Yalnızca Active Directory dizinleri senkronize edilebilir.");

        var syncedMappings = await db.DirectoryAttributeMappings
            .Where(m => m.DirectoryId == directory.Id && m.IsSynced)
            .ToListAsync(cancellationToken);

        var extraAttributeNames = syncedMappings
            .Where(m => m.FieldType != DirectoryAttributeMapping.PhotoFieldType)
            .Select(m => m.AdAttributeName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var binaryAttributeNames = syncedMappings
            .Where(m => m.FieldType == DirectoryAttributeMapping.PhotoFieldType)
            .Select(m => m.AdAttributeName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var ldapUsers = await ldapService.SearchUsersAsync(
            directory, extraAttributeNames, binaryAttributeNames, cancellationToken);

        var existingUsers = await db.Users
            .Include(u => u.Attributes)
            .Where(u => u.DirectoryId == directory.Id)
            .ToListAsync(cancellationToken);

        var byObjectGuid = existingUsers
            .Where(u => u.ObjectGuid is not null)
            .ToDictionary(u => u.ObjectGuid!, StringComparer.OrdinalIgnoreCase);

        // "Kullanıcı" tipi alanların DN referanslarını (ör. manager) aynı taramadaki bir
        // kullanıcıyla eşleştirebilmek için: DN -> ObjectGuid haritası. AD, aynı DN'i bir
        // kullanıcının kendi "distinguishedName"inde ve bir başkasının "manager" alanında
        // virgülden sonra boşluklu/boşluksuz ya da farklı Unicode normalizasyonuyla
        // döndürebiliyor — bu yüzden anahtar/karşılaştırma normalize edilmiş DN üzerinden yapılır.
        var dnToObjectGuid = ldapUsers
            .Where(u => !string.IsNullOrWhiteSpace(u.DistinguishedName))
            .ToDictionary(u => NormalizeDn(u.DistinguishedName!), u => u.ObjectGuid, StringComparer.OrdinalIgnoreCase);

        var syncedAtUtc = DateTime.UtcNow;
        var seenObjectGuids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var added = 0;
        var updated = 0;
        var processed = new List<(LdapUser LdapUser, User User)>();

        // 1. geçiş: tüm kullanıcılar oluşturulur/güncellenir — attribute'lar henüz uygulanmaz.
        // Böylece "Yönetici" gibi başka bir kullanıcıya referans veren alanlar, referans verilen
        // kullanıcı bu taramada henüz işlenmemiş olsa bile (liste sırasına bakılmaksızın) çözülebilir.
        foreach (var ldapUser in ldapUsers)
        {
            seenObjectGuids.Add(ldapUser.ObjectGuid);

            if (byObjectGuid.TryGetValue(ldapUser.ObjectGuid, out var existing))
            {
                existing.UpdateFromSync(
                    ldapUser.FirstName, ldapUser.LastName, ldapUser.DisplayName, ldapUser.Email,
                    ldapUser.IsEnabled, syncedAtUtc);
                processed.Add((ldapUser, existing));
                updated++;
            }
            else
            {
                var created = User.CreateFromActiveDirectory(
                    directory.Id, ldapUser.Username, ldapUser.FirstName, ldapUser.LastName,
                    ldapUser.DisplayName, ldapUser.Email, ldapUser.ObjectGuid);
                if (!ldapUser.IsEnabled)
                    created.Deactivate();
                db.Users.Add(created);
                byObjectGuid[created.ObjectGuid!] = created;
                processed.Add((ldapUser, created));
                added++;
            }
        }

        // 2. geçiş: artık taramadaki tüm kullanıcılar mevcut, attribute'lar (ve varsa kullanıcı
        // referansları) güvenle çözülüp uygulanabilir.
        foreach (var (ldapUser, user) in processed)
            ApplyAttributes(db, user, ldapUser, syncedMappings, dnToObjectGuid, byObjectGuid);

        // Dizinde artık bulunmayan kullanıcılar silinmez, yalnızca pasife alınır.
        var deactivated = 0;
        foreach (var user in existingUsers)
        {
            if (user.ObjectGuid is not null && seenObjectGuids.Contains(user.ObjectGuid))
                continue;
            if (!user.IsActive)
                continue;

            user.Deactivate();
            deactivated++;
        }

        // byObjectGuid, senkron kapsamındaki hem yeni hem mevcut tüm kullanıcıları tutar — yeni
        // eklenenler bu noktada henüz kaydedilmediği (SaveChangesAsync aşağıda) için db.Users
        // sorgusu onları görmez; bellek içindeki bu koleksiyon üzerinden sayılır.
        var missingCalendarCount = byObjectGuid.Values.Count(u => u.IsActive && u.WorkCalendarId == null);

        if (missingCalendarCount > 0)
        {
            db.Notifications.Add(Notification.Create(
                $"'{directory.Name}' dizininde {missingCalendarCount} kullanıcının mesai takvimi atanmamış."));
        }

        directory.MarkSynced(syncedAtUtc);
        directoryRepository.Update(directory);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new DirectorySyncResultDto
        {
            DirectoryId = directory.Id,
            DirectoryName = directory.Name,
            Added = added,
            Updated = updated,
            Deactivated = deactivated,
            TotalFromDirectory = ldapUsers.Count,
            SyncedAtUtc = syncedAtUtc
        };
    }

    /// <summary>
    /// EF Core, boş bir koleksiyona senkronizasyon sırasında eklenen yeni öğeleri her zaman
    /// otomatik DetectChanges ile fark etmeyebilir (özellikle kullanıcının bu senkronizasyondan
    /// önce hiç attribute'u yoksa). Bu yüzden yeni oluşturulan attribute context'e açıkça eklenir.
    /// </summary>
    private static void ApplyAttributes(
        IApplicationDbContext db, User user, LdapUser ldapUser,
        IReadOnlyCollection<DirectoryAttributeMapping> mappings,
        IReadOnlyDictionary<string, string> dnToObjectGuid,
        IReadOnlyDictionary<string, User> byObjectGuid)
    {
        foreach (var mapping in mappings)
        {
            ldapUser.Attributes.TryGetValue(mapping.AdAttributeName, out var rawValue);

            string? value = rawValue;
            Guid? referencedUserId = null;

            if (mapping.FieldType == DirectoryAttributeMapping.UserReferenceFieldType && rawValue is not null)
            {
                if (dnToObjectGuid.TryGetValue(NormalizeDn(rawValue), out var referencedObjectGuid)
                    && byObjectGuid.TryGetValue(referencedObjectGuid, out var referencedUser))
                {
                    referencedUserId = referencedUser.Id;
                    value = referencedUser.DisplayName ?? referencedUser.Username;
                }
                else
                {
                    // Referans verilen kişi bu dizinin senkronizasyon kapsamına (ör. filtreye)
                    // girmiyorsa sistemde bir karşılığı yoktur — DN'den düz isim çıkarılır.
                    value = ExtractPlainNameFromDn(rawValue);
                }
            }

            var createdAttribute = user.SetAttribute(mapping.Id, value, referencedUserId);
            if (createdAttribute is not null)
                db.UserAttributes.Add(createdAttribute);
        }
    }

    /// <summary>
    /// Aynı DN, AD'den bir kullanıcının kendi "distinguishedName"inde ve bir başkasının "manager"
    /// alanında sözdizimsel olarak biraz farklı dönebiliyor (virgülden sonra boşluk olup olmaması,
    /// Türkçe karakterlerin farklı Unicode normalizasyon formu). Ham string eşitliği bu yüzden
    /// güvenilir değil — bileşenler arasındaki boşluk kaldırılıp NFC'ye normalize edilir.
    /// </summary>
    private static string NormalizeDn(string distinguishedName) =>
        DnComponentSeparatorPattern().Replace(distinguishedName.Trim(), ",").Normalize(NormalizationForm.FormC);

    [GeneratedRegex(@"\s*,\s*")]
    private static partial Regex DnComponentSeparatorPattern();

    /// <summary>Bir DN'in ilk RDN bileşenindeki değeri döner (ör. "CN=Ada Lovelace,OU=..." -> "Ada Lovelace").</summary>
    private static string ExtractPlainNameFromDn(string distinguishedName)
    {
        var match = DnLeadingCnPattern().Match(distinguishedName);
        return match.Success ? match.Groups[1].Value : distinguishedName;
    }

    [GeneratedRegex(@"^\s*CN=([^,]+)", RegexOptions.IgnoreCase)]
    private static partial Regex DnLeadingCnPattern();
}
