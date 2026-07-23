using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Roles.Commands.GrantPermission;
using EforTakip.Application.Tests.Directories.Commands;
using EforTakip.Domain.Authorization;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.Roles;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace EforTakip.Application.Tests.Roles.Commands;

public class GrantPermissionCommandHandlerTests : IAsyncDisposable
{
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly TestDbContext _db;

    public GrantPermissionCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase($"grant-permission-tests-{Guid.NewGuid()}")
            .Options;
        _db = new TestDbContext(options);

        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(callInfo => _db.SaveChangesAsync(callInfo.Arg<CancellationToken>()));
    }

    public async ValueTask DisposeAsync() => await _db.DisposeAsync();

    private GrantPermissionCommandHandler CreateHandler() => new(_db, _unitOfWork);

    [Fact]
    public async Task Handle_ValidPermission_GrantsIt()
    {
        var role = Role.Create("Proje Yöneticisi", null, false);
        _db.Roles.Add(role);
        await _db.SaveChangesAsync();

        await CreateHandler().Handle(
            new GrantPermissionCommand(role.Id, Permissions.Project.Read), CancellationToken.None);

        var reloaded = await _db.Roles.Include(r => r.Permissions).FirstAsync(r => r.Id == role.Id);
        reloaded.Permissions.Should().ContainSingle(p => p.PermissionKey == Permissions.Project.Read);
    }

    [Fact]
    public async Task Handle_AlreadyGranted_DoesNotDuplicate()
    {
        var role = Role.Create("Proje Yöneticisi", null, false);
        _db.Roles.Add(role);
        await _db.SaveChangesAsync();

        var handler = CreateHandler();
        await handler.Handle(new GrantPermissionCommand(role.Id, Permissions.Project.Read), CancellationToken.None);
        await handler.Handle(new GrantPermissionCommand(role.Id, Permissions.Project.Read), CancellationToken.None);

        var reloaded = await _db.Roles.Include(r => r.Permissions).FirstAsync(r => r.Id == role.Id);
        reloaded.Permissions.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_InvalidPermissionKey_Throws()
    {
        var role = Role.Create("Proje Yöneticisi", null, false);
        _db.Roles.Add(role);
        await _db.SaveChangesAsync();

        var act = async () => await CreateHandler().Handle(
            new GrantPermissionCommand(role.Id, "olmayan:izin"), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleValidationException>();
    }
}
