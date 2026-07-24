using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Users.Commands.CreateInternalUser;
using EforTakip.Application.Tests.Directories.Commands;
using EforTakip.Domain.Directories;
using EforTakip.Domain.Users;
using EforTakip.Domain.Exceptions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Directory = EforTakip.Domain.Directories.Directory;

namespace EforTakip.Application.Tests.Users.Commands;

public class CreateInternalUserCommandHandlerTests : IAsyncDisposable
{
    private readonly IRepository<Directory> _directoryRepository = Substitute.For<IRepository<Directory>>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly TestDbContext _db;

    public CreateInternalUserCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase($"internal-user-tests-{Guid.NewGuid()}")
            .Options;
        _db = new TestDbContext(options);

        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(callInfo => _db.SaveChangesAsync(callInfo.Arg<CancellationToken>()));
        _passwordHasher.Hash(Arg.Any<string>()).Returns("HASHED");
    }

    public async ValueTask DisposeAsync() => await _db.DisposeAsync();

    private CreateInternalUserCommandHandler CreateHandler()
        => new(_db, _directoryRepository, _passwordHasher, _unitOfWork);

    private static CreateInternalUserCommand Command(Guid directoryId, string username = "sanal.kullanici") =>
        new(directoryId, username, "GucluSifre123", "Sanal", "Kullanıcı", "Sanal Kullanıcı", null);

    [Fact]
    public async Task Handle_ValidCommand_CreatesUserWithHashedPassword()
    {
        var directory = Directory.CreateInternal("Internal Users", 0);
        _directoryRepository.GetByIdAsync(directory.Id, Arg.Any<CancellationToken>()).Returns(directory);

        var result = await CreateHandler().Handle(Command(directory.Id), CancellationToken.None);

        result.Should().NotBeEmpty();
        var user = _db.Users.Single();
        user.Username.Should().Be("sanal.kullanici");
        user.Source.Should().Be(DirectorySource.Internal);
        user.PasswordHash.Should().Be("HASHED");
    }

    [Fact]
    public async Task Handle_PlainPasswordIsNeverStored()
    {
        var directory = Directory.CreateInternal("Internal Users", 0);
        _directoryRepository.GetByIdAsync(directory.Id, Arg.Any<CancellationToken>()).Returns(directory);

        await CreateHandler().Handle(Command(directory.Id), CancellationToken.None);

        _db.Users.Single().PasswordHash.Should().NotBe("GucluSifre123");
        _passwordHasher.Received(1).Hash("GucluSifre123");
    }

    [Fact]
    public async Task Handle_ActiveDirectoryDirectory_ThrowsBusinessRule()
    {
        var directory = Directory.CreateActiveDirectory(
            "AD", "Microsoft Active Directory", "kizilay.local", 389, false, "u", "p",
            "DC=kizilay,DC=local", null, null, DirectoryPermission.ReadOnly, "user", "(x)",
            "sAMAccountName", "cn", "givenName", "sn", "displayName", "mail", "objectGUID",
            SyncScheduleKind.Off, 0);
        _directoryRepository.GetByIdAsync(directory.Id, Arg.Any<CancellationToken>()).Returns(directory);

        var act = async () => await CreateHandler().Handle(Command(directory.Id), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleValidationException>();
    }

    [Fact]
    public async Task Handle_DuplicateUsername_ThrowsBusinessRule()
    {
        var directory = Directory.CreateInternal("Internal Users", 0);
        _directoryRepository.GetByIdAsync(directory.Id, Arg.Any<CancellationToken>()).Returns(directory);
        _db.Users.Add(User.CreateInternal(
            directory.Id, "sanal.kullanici", null, null, null, null, "HASHED"));
        await _db.SaveChangesAsync();

        var act = async () => await CreateHandler().Handle(Command(directory.Id), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleValidationException>();
    }

    [Fact]
    public async Task Handle_NonExistingDirectory_ThrowsNotFound()
    {
        _directoryRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Directory?)null);

        var act = async () => await CreateHandler().Handle(Command(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
