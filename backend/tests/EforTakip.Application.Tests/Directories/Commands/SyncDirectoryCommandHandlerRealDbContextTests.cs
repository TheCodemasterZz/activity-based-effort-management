using EforTakip.Application.Directories.Commands.SyncDirectory;
using EforTakip.Application.Directories.Ldap;
using EforTakip.Domain.Directories;
using EforTakip.Persistence;
using EforTakip.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Directory = EforTakip.Domain.Directories.Directory;

namespace EforTakip.Application.Tests.Directories.Commands;

/// <summary>
/// TestDbContext (bkz. <see cref="TestDbContext"/>) EF model konfigürasyonunu elle, eksik olarak
/// kurduğu için (ör. DirectoryUserAttribute -> DirectoryAttributeMapping FK'i, unique index)
/// gerçek EforTakipDbContext'te ortaya çıkan bir izleme (change tracking) hatasını yakalayamıyordu.
/// Bu testler gerçek DbContext'i ve her istek için ayrı bir scope'u (ayrı DbContext örneği)
/// kullanarak üretim davranışını taklit eder.
/// </summary>
public sealed class SyncDirectoryCommandHandlerRealDbContextTests : IAsyncDisposable
{
    private readonly DbContextOptions<EforTakipDbContext> _options = new DbContextOptionsBuilder<EforTakipDbContext>()
        .UseInMemoryDatabase($"real-sync-tests-{Guid.NewGuid()}")
        .Options;

    public async ValueTask DisposeAsync()
    {
        await using var db = new EforTakipDbContext(_options);
        await db.Database.EnsureDeletedAsync();
    }

    private static Directory ValidAd() =>
        Directory.CreateActiveDirectory(
            "Kızılay AD", "Microsoft Active Directory", "kizilay.local", 389, false, "u", "p",
            "DC=kizilay,DC=local", null, null, DirectoryPermission.ReadOnly, "user", "(x)",
            "sAMAccountName", "cn", "givenName", "sn", "displayName", "mail", "objectGUID",
            SyncScheduleKind.Daily, 0);

    private static LdapUser LdapUserOf(string username, string guid, string? company = null) =>
        new(username, "Serkan", "Gültepe", "Serkan Gültepe", $"{username}@kizilay.org.tr", guid, true,
            company is null
                ? new Dictionary<string, string?>()
                : new Dictionary<string, string?> { ["company"] = company });

    [Fact]
    public async Task Handle_ExistingUserWithNoPriorAttributes_ThenNewSyncedMapping_DoesNotThrow()
    {
        Guid directoryId;

        // İstek 1: dizini oluştur.
        await using (var db = new EforTakipDbContext(_options))
        {
            var directory = ValidAd();
            directoryId = directory.Id;
            db.Directories.Add(directory);
            await db.SaveChangesAsync();
        }

        // İstek 2: ilk senkronizasyon, attribute mapping yok — kullanıcı attribute'suz eklenir.
        await using (var db = new EforTakipDbContext(_options))
        {
            var directory = await db.Directories.SingleAsync(d => d.Id == directoryId);
            var ldap = Substitute.For<ILdapService>();
            ldap.SearchUsersAsync(directory, Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
                .Returns(new List<LdapUser> { LdapUserOf("serkan.gultepe", "guid-1") });

            var handler = new SyncDirectoryCommandHandler(
                db, new RepositoryBase<Directory>(db), ldap, new UnitOfWork(db));

            var result = await handler.Handle(new SyncDirectoryCommand(directoryId), CancellationToken.None);
            result.Added.Should().Be(1);
        }

        // İstek 3: yönetici yeni bir senkronize edilecek alan eşlemesi ekler.
        await using (var db = new EforTakipDbContext(_options))
        {
            db.DirectoryAttributeMappings.Add(
                DirectoryAttributeMapping.Create(directoryId, "company", "Kurum", "text", isSynced: true, 0));
            await db.SaveChangesAsync();
        }

        // İstek 4: ikinci senkronizasyon — aynı kullanıcı artık bir attribute değeriyle güncellenir.
        // Regresyon: burada DbUpdateConcurrencyException fırlıyordu çünkü kullanıcının önceden
        // boş olan Attributes koleksiyonuna eklenen yeni öğe EF Core tarafından fark edilmiyordu.
        await using (var db = new EforTakipDbContext(_options))
        {
            var directory = await db.Directories.SingleAsync(d => d.Id == directoryId);
            var ldap = Substitute.For<ILdapService>();
            ldap.SearchUsersAsync(directory, Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
                .Returns(new List<LdapUser> { LdapUserOf("serkan.gultepe", "guid-1", company: "Kızılay") });

            var handler = new SyncDirectoryCommandHandler(
                db, new RepositoryBase<Directory>(db), ldap, new UnitOfWork(db));

            var act = async () => await handler.Handle(new SyncDirectoryCommand(directoryId), CancellationToken.None);
            await act.Should().NotThrowAsync();
        }

        // Değerin gerçekten kaydedildiğini doğrula.
        await using (var db = new EforTakipDbContext(_options))
        {
            var user = await db.DirectoryUsers.Include(u => u.Attributes).SingleAsync(u => u.DirectoryId == directoryId);
            user.Attributes.Should().ContainSingle(a => a.Value == "Kızılay");
        }
    }
}
