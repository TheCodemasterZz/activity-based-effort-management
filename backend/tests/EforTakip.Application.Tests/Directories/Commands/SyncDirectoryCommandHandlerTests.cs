using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Directories.Commands.SyncDirectory;
using EforTakip.Application.Directories.Ldap;
using EforTakip.Domain.Directories;
using EforTakip.Domain.Exceptions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Directory = EforTakip.Domain.Directories.Directory;

namespace EforTakip.Application.Tests.Directories.Commands;

public class SyncDirectoryCommandHandlerTests : IAsyncDisposable
{
    private readonly IRepository<Directory> _directoryRepository = Substitute.For<IRepository<Directory>>();
    private readonly ILdapService _ldapService = Substitute.For<ILdapService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly TestDbContext _db;

    public SyncDirectoryCommandHandlerTests()
    {
        _db = CreateDb();

        // IUnitOfWork gerçek context'e delege edilir; aksi halde handler'ın eklediği
        // varlıklar kaydedilmez ve DbSet sorgularında görünmez.
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(callInfo => _db.SaveChangesAsync(callInfo.Arg<CancellationToken>()));
    }

    public async ValueTask DisposeAsync() => await _db.DisposeAsync();

    private SyncDirectoryCommandHandler CreateHandler()
        => new(_db, _directoryRepository, _ldapService, _unitOfWork);

    private static TestDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase($"sync-tests-{Guid.NewGuid()}")
            .Options;
        return new TestDbContext(options);
    }

    private static Directory ValidAd() =>
        Directory.CreateActiveDirectory(
            "Kızılay AD", "Microsoft Active Directory", "kizilay.local", 389, false, "u", "p",
            "DC=kizilay,DC=local", null, null, DirectoryPermission.ReadOnly, "user", "(x)",
            "sAMAccountName", "cn", "givenName", "sn", "displayName", "mail", "objectGUID",
            SyncScheduleKind.Daily, 0);

    private static LdapUser LdapUserOf(
        string username, string guid, string? company = null, bool isEnabled = true,
        string? manager = null, string? distinguishedName = null) =>
        new(username, "Serkan", "Gültepe", "Serkan Gültepe", $"{username}@kizilay.org.tr", guid, isEnabled,
            BuildAttributes(company, manager),
            distinguishedName);

    private static Dictionary<string, string?> BuildAttributes(string? company, string? manager)
    {
        var attributes = new Dictionary<string, string?>();
        if (company is not null)
            attributes["company"] = company;
        if (manager is not null)
            attributes["manager"] = manager;
        return attributes;
    }

    [Fact]
    public async Task Handle_NewUsersFromLdap_AddsThem()
    {
        var directory = ValidAd();
        _directoryRepository.GetByIdAsync(directory.Id, Arg.Any<CancellationToken>()).Returns(directory);
        _ldapService.SearchUsersAsync(directory, Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<LdapUser> { LdapUserOf("serkan.gultepe", "guid-1") });

        var result = await CreateHandler().Handle(new SyncDirectoryCommand(directory.Id), CancellationToken.None);

        result.Added.Should().Be(1);
        result.Updated.Should().Be(0);
        result.Deactivated.Should().Be(0);
        result.TotalFromDirectory.Should().Be(1);
        _db.DirectoryUsers.Should().ContainSingle(u => u.Username == "serkan.gultepe");
    }

    [Fact]
    public async Task Handle_ExistingUser_UpdatesInsteadOfDuplicating()
    {
        var directory = ValidAd();
        var existing = DirectoryUser.CreateFromActiveDirectory(
            directory.Id, "serkan.gultepe", "Serkan", "Eski", "Eski Ad", "eski@x.com", "guid-1");
        _db.DirectoryUsers.Add(existing);
        await _db.SaveChangesAsync();

        _directoryRepository.GetByIdAsync(directory.Id, Arg.Any<CancellationToken>()).Returns(directory);
        _ldapService.SearchUsersAsync(directory, Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<LdapUser> { LdapUserOf("serkan.gultepe", "guid-1") });

        var result = await CreateHandler().Handle(new SyncDirectoryCommand(directory.Id), CancellationToken.None);

        result.Added.Should().Be(0);
        result.Updated.Should().Be(1);
        _db.DirectoryUsers.Should().ContainSingle();
        _db.DirectoryUsers.Single().LastName.Should().Be("Gültepe");
    }

    [Fact]
    public async Task Handle_UserMissingFromLdap_IsDeactivatedNotDeleted()
    {
        var directory = ValidAd();
        var stale = DirectoryUser.CreateFromActiveDirectory(
            directory.Id, "ayrilan.kullanici", "Ayrılan", "Kullanıcı", "Ayrılan", null, "guid-eski");
        _db.DirectoryUsers.Add(stale);
        await _db.SaveChangesAsync();

        _directoryRepository.GetByIdAsync(directory.Id, Arg.Any<CancellationToken>()).Returns(directory);
        _ldapService.SearchUsersAsync(directory, Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<LdapUser>());

        var result = await CreateHandler().Handle(new SyncDirectoryCommand(directory.Id), CancellationToken.None);

        result.Deactivated.Should().Be(1);
        _db.DirectoryUsers.Should().ContainSingle();
        _db.DirectoryUsers.Single().IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_UserDisabledInDirectory_IsStoredAsInactive()
    {
        var directory = ValidAd();
        _directoryRepository.GetByIdAsync(directory.Id, Arg.Any<CancellationToken>()).Returns(directory);
        _ldapService.SearchUsersAsync(directory, Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<LdapUser> { LdapUserOf("pasif.kullanici", "guid-2", isEnabled: false) });

        var result = await CreateHandler().Handle(new SyncDirectoryCommand(directory.Id), CancellationToken.None);

        result.Added.Should().Be(1);
        _db.DirectoryUsers.Single().IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ExistingUserDisabledInDirectory_BecomesInactive()
    {
        var directory = ValidAd();
        var existing = DirectoryUser.CreateFromActiveDirectory(
            directory.Id, "serkan.gultepe", "Serkan", "Gültepe", "Serkan Gültepe", null, "guid-1");
        _db.DirectoryUsers.Add(existing);
        await _db.SaveChangesAsync();

        _directoryRepository.GetByIdAsync(directory.Id, Arg.Any<CancellationToken>()).Returns(directory);
        _ldapService.SearchUsersAsync(directory, Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<LdapUser> { LdapUserOf("serkan.gultepe", "guid-1", isEnabled: false) });

        await CreateHandler().Handle(new SyncDirectoryCommand(directory.Id), CancellationToken.None);

        _db.DirectoryUsers.Single().IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithSyncedMapping_WritesAttributeValue()
    {
        var directory = ValidAd();
        var mapping = DirectoryAttributeMapping.Create("company", "Kurum", "text", isSynced: true, 0);
        var ignored = DirectoryAttributeMapping.Create("department", "Departman", "text", isSynced: false, 1);
        _db.DirectoryAttributeMappings.AddRange(mapping, ignored);
        await _db.SaveChangesAsync();

        _directoryRepository.GetByIdAsync(directory.Id, Arg.Any<CancellationToken>()).Returns(directory);
        _ldapService.SearchUsersAsync(directory, Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<LdapUser> { LdapUserOf("serkan.gultepe", "guid-1", company: "Kızılay") });

        await CreateHandler().Handle(new SyncDirectoryCommand(directory.Id), CancellationToken.None);

        var user = _db.DirectoryUsers.Include(u => u.Attributes).Single();
        user.Attributes.Should().ContainSingle();
        user.Attributes.Single().AttributeMappingId.Should().Be(mapping.Id);
        user.Attributes.Single().Value.Should().Be("Kızılay");
    }

    [Fact]
    public async Task Handle_UserFieldMatchingAnotherSyncedUser_ResolvesReference()
    {
        var directory = ValidAd();
        var mapping = DirectoryAttributeMapping.Create("manager", "Yönetici", "user", isSynced: true, 0);
        _db.DirectoryAttributeMappings.Add(mapping);
        await _db.SaveChangesAsync();

        const string managerDn = "CN=Gökhan Yetkin,OU=Yönetim,DC=kizilay,DC=local";

        _directoryRepository.GetByIdAsync(directory.Id, Arg.Any<CancellationToken>()).Returns(directory);
        _ldapService.SearchUsersAsync(directory, Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<LdapUser>
            {
                LdapUserOf("gokhan.yetkin", "guid-manager", distinguishedName: managerDn),
                LdapUserOf("baris.kalaycioglu", "guid-1", manager: managerDn)
            });

        await CreateHandler().Handle(new SyncDirectoryCommand(directory.Id), CancellationToken.None);

        var employee = _db.DirectoryUsers.Include(u => u.Attributes).Single(u => u.Username == "baris.kalaycioglu");
        var manager = _db.DirectoryUsers.Single(u => u.Username == "gokhan.yetkin");

        var attribute = employee.Attributes.Single();
        attribute.ReferencedDirectoryUserId.Should().Be(manager.Id);
        attribute.Value.Should().Be(manager.DisplayName);
    }

    [Fact]
    public async Task Handle_UserFieldWithNoMatchingUser_FallsBackToPlainNameFromDn()
    {
        var directory = ValidAd();
        var mapping = DirectoryAttributeMapping.Create("manager", "Yönetici", "user", isSynced: true, 0);
        _db.DirectoryAttributeMappings.Add(mapping);
        await _db.SaveChangesAsync();

        _directoryRepository.GetByIdAsync(directory.Id, Arg.Any<CancellationToken>()).Returns(directory);
        _ldapService.SearchUsersAsync(directory, Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<LdapUser>
            {
                LdapUserOf(
                    "baris.kalaycioglu", "guid-1",
                    manager: "CN=Gökhan Yetkin,OU=Yönetim,DC=kizilay,DC=local")
            });

        await CreateHandler().Handle(new SyncDirectoryCommand(directory.Id), CancellationToken.None);

        var employee = _db.DirectoryUsers.Include(u => u.Attributes).Single();
        var attribute = employee.Attributes.Single();

        attribute.ReferencedDirectoryUserId.Should().BeNull();
        attribute.Value.Should().Be("Gökhan Yetkin");
    }

    [Fact]
    public async Task Handle_OnlySyncedMappingsAreRequestedFromLdap()
    {
        var directory = ValidAd();
        _db.DirectoryAttributeMappings.AddRange(
            DirectoryAttributeMapping.Create("company", "Kurum", "text", isSynced: true, 0),
            DirectoryAttributeMapping.Create("department", "Departman", "text", isSynced: false, 1));
        await _db.SaveChangesAsync();

        _directoryRepository.GetByIdAsync(directory.Id, Arg.Any<CancellationToken>()).Returns(directory);
        _ldapService.SearchUsersAsync(directory, Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<LdapUser>());

        await CreateHandler().Handle(new SyncDirectoryCommand(directory.Id), CancellationToken.None);

        await _ldapService.Received(1).SearchUsersAsync(
            directory,
            Arg.Is<IReadOnlyCollection<string>>(names => names.Count == 1 && names.Contains("company")),
            Arg.Any<IReadOnlyCollection<string>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MarksDirectoryAsSynced()
    {
        var directory = ValidAd();
        _directoryRepository.GetByIdAsync(directory.Id, Arg.Any<CancellationToken>()).Returns(directory);
        _ldapService.SearchUsersAsync(directory, Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<LdapUser>());

        await CreateHandler().Handle(new SyncDirectoryCommand(directory.Id), CancellationToken.None);

        directory.LastSyncedUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_InternalDirectory_ThrowsBusinessRule()
    {
        var directory = Directory.CreateInternal("Internal Users", 0);
        _directoryRepository.GetByIdAsync(directory.Id, Arg.Any<CancellationToken>()).Returns(directory);

        var act = async () => await CreateHandler().Handle(
            new SyncDirectoryCommand(directory.Id), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleValidationException>();
    }

    [Fact]
    public async Task Handle_NonExistingDirectory_ThrowsNotFound()
    {
        _directoryRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Directory?)null);

        var act = async () => await CreateHandler().Handle(
            new SyncDirectoryCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
