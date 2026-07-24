using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Roles.Commands.AssignRoleToUser;
using EforTakip.Application.Tests.Directories.Commands;
using EforTakip.Domain.Directories;
using EforTakip.Domain.Users;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.Roles;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Directory = EforTakip.Domain.Directories.Directory;

namespace EforTakip.Application.Tests.Roles.Commands;

public class AssignRoleToUserCommandHandlerTests : IAsyncDisposable
{
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly TestDbContext _db;

    public AssignRoleToUserCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase($"assign-role-tests-{Guid.NewGuid()}")
            .Options;
        _db = new TestDbContext(options);

        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(callInfo => _db.SaveChangesAsync(callInfo.Arg<CancellationToken>()));
    }

    public async ValueTask DisposeAsync() => await _db.DisposeAsync();

    private AssignRoleToUserCommandHandler CreateHandler() => new(_db, _unitOfWork);

    private async Task<(User User, Role Role)> SeedUserAndRoleAsync()
    {
        var directory = Directory.CreateInternal("Internal Users", 0);
        var user = User.CreateInternal(directory.Id, "kullanici", null, null, null, null, "HASH");
        var role = Role.Create("Proje Yöneticisi", null, false);
        _db.Directories.Add(directory);
        _db.Users.Add(user);
        _db.Roles.Add(role);
        await _db.SaveChangesAsync();
        return (user, role);
    }

    [Fact]
    public async Task Handle_ValidUserAndRole_Assigns()
    {
        var (user, role) = await SeedUserAndRoleAsync();

        await CreateHandler().Handle(new AssignRoleToUserCommand(user.Id, role.Id), CancellationToken.None);

        var reloaded = await _db.Users.Include(u => u.Roles).FirstAsync(u => u.Id == user.Id);
        reloaded.Roles.Should().ContainSingle(r => r.RoleId == role.Id);
    }

    [Fact]
    public async Task Handle_AlreadyAssigned_DoesNotDuplicate()
    {
        var (user, role) = await SeedUserAndRoleAsync();
        var handler = CreateHandler();

        await handler.Handle(new AssignRoleToUserCommand(user.Id, role.Id), CancellationToken.None);
        await handler.Handle(new AssignRoleToUserCommand(user.Id, role.Id), CancellationToken.None);

        var reloaded = await _db.Users.Include(u => u.Roles).FirstAsync(u => u.Id == user.Id);
        reloaded.Roles.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_UnknownRole_ThrowsNotFound()
    {
        var directory = Directory.CreateInternal("Internal Users", 0);
        var user = User.CreateInternal(directory.Id, "kullanici", null, null, null, null, "HASH");
        _db.Directories.Add(directory);
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var act = async () => await CreateHandler().Handle(
            new AssignRoleToUserCommand(user.Id, Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
