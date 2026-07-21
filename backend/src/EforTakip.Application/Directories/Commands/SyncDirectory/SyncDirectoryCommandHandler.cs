using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Directories.Dtos;
using EforTakip.Application.Directories.Ldap;
using EforTakip.Domain.Directories;
using EforTakip.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Directory = EforTakip.Domain.Directories.Directory;

namespace EforTakip.Application.Directories.Commands.SyncDirectory;

public sealed class SyncDirectoryCommandHandler(
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
            .Where(m => m.IsSynced)
            .ToListAsync(cancellationToken);

        var extraAttributeNames = syncedMappings
            .Select(m => m.AdAttributeName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var ldapUsers = await ldapService.SearchUsersAsync(directory, extraAttributeNames, cancellationToken);

        var existingUsers = await db.DirectoryUsers
            .Include(u => u.Attributes)
            .Where(u => u.DirectoryId == directory.Id)
            .ToListAsync(cancellationToken);

        var byObjectGuid = existingUsers
            .Where(u => u.ObjectGuid is not null)
            .ToDictionary(u => u.ObjectGuid!, StringComparer.OrdinalIgnoreCase);

        var syncedAtUtc = DateTime.UtcNow;
        var seenObjectGuids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var added = 0;
        var updated = 0;

        foreach (var ldapUser in ldapUsers)
        {
            seenObjectGuids.Add(ldapUser.ObjectGuid);

            if (byObjectGuid.TryGetValue(ldapUser.ObjectGuid, out var existing))
            {
                existing.UpdateFromSync(
                    ldapUser.FirstName, ldapUser.LastName, ldapUser.DisplayName, ldapUser.Email,
                    ldapUser.IsEnabled, syncedAtUtc);
                ApplyAttributes(existing, ldapUser, syncedMappings);
                updated++;
            }
            else
            {
                var created = DirectoryUser.CreateFromActiveDirectory(
                    directory.Id, ldapUser.Username, ldapUser.FirstName, ldapUser.LastName,
                    ldapUser.DisplayName, ldapUser.Email, ldapUser.ObjectGuid);
                if (!ldapUser.IsEnabled)
                    created.Deactivate();
                ApplyAttributes(created, ldapUser, syncedMappings);
                db.DirectoryUsers.Add(created);
                added++;
            }
        }

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

    private static void ApplyAttributes(
        DirectoryUser user, LdapUser ldapUser, IReadOnlyCollection<DirectoryAttributeMapping> mappings)
    {
        foreach (var mapping in mappings)
        {
            ldapUser.Attributes.TryGetValue(mapping.AdAttributeName, out var value);
            user.SetAttribute(mapping.Id, value);
        }
    }
}
