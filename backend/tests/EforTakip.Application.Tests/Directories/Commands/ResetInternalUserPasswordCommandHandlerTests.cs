using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Directories.Commands.ResetInternalUserPassword;
using EforTakip.Domain.Directories;
using EforTakip.Domain.Exceptions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace EforTakip.Application.Tests.Directories.Commands;

public class ResetInternalUserPasswordCommandHandlerTests : IAsyncDisposable
{
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly TestDbContext _db;

    public ResetInternalUserPasswordCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase($"reset-password-tests-{Guid.NewGuid()}")
            .Options;
        _db = new TestDbContext(options);

        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(callInfo => _db.SaveChangesAsync(callInfo.Arg<CancellationToken>()));
        _passwordHasher.Hash(Arg.Any<string>()).Returns("YENI_HASH");
    }

    public async ValueTask DisposeAsync() => await _db.DisposeAsync();

    private ResetInternalUserPasswordCommandHandler CreateHandler()
        => new(_db, _passwordHasher, _unitOfWork);

    private async Task<DirectoryUser> AddInternalUserAsync()
    {
        var user = DirectoryUser.CreateInternal(
            Guid.NewGuid(), "sanal.kullanici", null, null, null, null, "ESKI_HASH");
        _db.DirectoryUsers.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task Handle_InternalUser_StoresNewHash()
    {
        var user = await AddInternalUserAsync();

        await CreateHandler().Handle(
            new ResetInternalUserPasswordCommand(user.Id, "YeniGucluSifre123"), CancellationToken.None);

        _db.DirectoryUsers.Single().PasswordHash.Should().Be("YENI_HASH");
    }

    [Fact]
    public async Task Handle_PlainPasswordIsNeverStored()
    {
        var user = await AddInternalUserAsync();

        await CreateHandler().Handle(
            new ResetInternalUserPasswordCommand(user.Id, "YeniGucluSifre123"), CancellationToken.None);

        _db.DirectoryUsers.Single().PasswordHash.Should().NotBe("YeniGucluSifre123");
        _passwordHasher.Received(1).Hash("YeniGucluSifre123");
    }

    [Fact]
    public async Task Handle_ActiveDirectoryUser_ThrowsBusinessRule()
    {
        var user = DirectoryUser.CreateFromActiveDirectory(
            Guid.NewGuid(), "serkan.gultepe", "Serkan", "Gültepe", "Serkan Gültepe", null, "guid-1");
        _db.DirectoryUsers.Add(user);
        await _db.SaveChangesAsync();

        var act = async () => await CreateHandler().Handle(
            new ResetInternalUserPasswordCommand(user.Id, "YeniGucluSifre123"), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleValidationException>();
    }

    [Fact]
    public async Task Handle_NonExistingUser_ThrowsNotFound()
    {
        var act = async () => await CreateHandler().Handle(
            new ResetInternalUserPasswordCommand(Guid.NewGuid(), "YeniGucluSifre123"),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
