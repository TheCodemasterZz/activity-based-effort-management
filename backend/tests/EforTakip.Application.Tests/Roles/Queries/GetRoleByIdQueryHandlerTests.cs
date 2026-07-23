using EforTakip.Application.Roles.Queries.GetRoleById;
using EforTakip.Application.Tests.Directories.Commands;
using EforTakip.Domain.Directories;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.Roles;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Directory = EforTakip.Domain.Directories.Directory;

namespace EforTakip.Application.Tests.Roles.Queries;

public class GetRoleByIdQueryHandlerTests : IAsyncDisposable
{
    private readonly TestDbContext _db;

    public GetRoleByIdQueryHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase($"get-role-tests-{Guid.NewGuid()}")
            .Options;
        _db = new TestDbContext(options);
    }

    public async ValueTask DisposeAsync() => await _db.DisposeAsync();

    private GetRoleByIdQueryHandler CreateHandler() => new(_db);

    [Fact]
    public async Task Handle_ExistingRole_ReturnsPermissionsAndAssignedUsers()
    {
        var directory = Directory.CreateInternal("Internal Users", 0);
        var user = DirectoryUser.CreateInternal(directory.Id, "kullanici", null, null, "Kullanıcı", null, "HASH");
        var role = Role.Create("Proje Yöneticisi", "Açıklama", false);
        role.GrantPermission("project:read");
        var assignment = user.AssignRole(role.Id);

        _db.Directories.Add(directory);
        _db.DirectoryUsers.Add(user);
        _db.Roles.Add(role);
        _db.DirectoryUserRoles.Add(assignment!);
        await _db.SaveChangesAsync();

        var result = await CreateHandler().Handle(new GetRoleByIdQuery(role.Id), CancellationToken.None);

        result.Name.Should().Be("Proje Yöneticisi");
        result.Permissions.Should().ContainSingle(p => p == "project:read");
        result.AssignedUsers.Should().ContainSingle(u => u.Username == "kullanici");
    }

    [Fact]
    public async Task Handle_UnknownRole_ThrowsNotFound()
    {
        var act = async () => await CreateHandler().Handle(new GetRoleByIdQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
