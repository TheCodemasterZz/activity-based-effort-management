using EforTakip.Application.Directories.Queries.GetOrgChart;
using EforTakip.Application.Tests.Directories.Commands;
using EforTakip.Domain.Directories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Directory = EforTakip.Domain.Directories.Directory;

namespace EforTakip.Application.Tests.Directories.Queries;

public sealed class GetOrgChartQueryHandlerTests : IAsyncDisposable
{
    private readonly TestDbContext _db;

    public GetOrgChartQueryHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase($"org-chart-tests-{Guid.NewGuid()}")
            .Options;
        _db = new TestDbContext(options);
    }

    public async ValueTask DisposeAsync() => await _db.DisposeAsync();

    private static Directory ValidAd() =>
        Directory.CreateActiveDirectory(
            "Kızılay AD", "Microsoft Active Directory", "kizilay.local", 389, false, "u", "p",
            "DC=kizilay,DC=local", null, null, DirectoryPermission.ReadOnly, "user", "(x)",
            "sAMAccountName", "cn", "givenName", "sn", "displayName", "mail", "objectGUID",
            SyncScheduleKind.Daily, 0);

    [Fact]
    public async Task Handle_WithoutManagerMapping_ReturnsHasManagerMappingFalse()
    {
        var directory = ValidAd();
        _db.Directories.Add(directory);
        await _db.SaveChangesAsync();

        var result = await new GetOrgChartQueryHandler(_db)
            .Handle(new GetOrgChartQuery(directory.Id), CancellationToken.None);

        result.HasManagerMapping.Should().BeFalse();
        result.Nodes.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithManagerMapping_ResolvesManagerId()
    {
        var directory = ValidAd();
        var mapping = DirectoryAttributeMapping.Create("manager", "Yönetici", "user", isSynced: true, 0);
        _db.Directories.Add(directory);
        _db.DirectoryAttributeMappings.Add(mapping);

        var manager = DirectoryUser.CreateFromActiveDirectory(
            directory.Id, "gokhan.yetkin", "Gökhan", "Yetkin", "Gökhan Yetkin", null, "guid-manager");
        var employee = DirectoryUser.CreateFromActiveDirectory(
            directory.Id, "baris.kalaycioglu", "Barış", "Kalaycıoğlu", "Barış Kalaycıoğlu", null, "guid-employee");
        employee.SetAttribute(mapping.Id, manager.DisplayName, manager.Id);
        _db.DirectoryUsers.AddRange(manager, employee);
        await _db.SaveChangesAsync();

        var result = await new GetOrgChartQueryHandler(_db)
            .Handle(new GetOrgChartQuery(directory.Id), CancellationToken.None);

        result.HasManagerMapping.Should().BeTrue();
        result.Nodes.Should().HaveCount(2);
        result.Nodes.Single(n => n.Username == "baris.kalaycioglu").ManagerId.Should().Be(manager.Id);
        result.Nodes.Single(n => n.Username == "gokhan.yetkin").ManagerId.Should().BeNull();
    }
}
