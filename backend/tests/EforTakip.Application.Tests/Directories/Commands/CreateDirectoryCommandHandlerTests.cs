using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Directories.Commands.CreateDirectory;
using EforTakip.Domain.Directories;
using FluentAssertions;
using NSubstitute;
using Directory = EforTakip.Domain.Directories.Directory;

namespace EforTakip.Application.Tests.Directories.Commands;

public class CreateDirectoryCommandHandlerTests
{
    private readonly IRepository<Directory> _repository = Substitute.For<IRepository<Directory>>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ISettingsEncryptor _settingsEncryptor = Substitute.For<ISettingsEncryptor>();

    [Fact]
    public async Task Handle_ActiveDirectory_CreatesAndPersists()
    {
        var handler = new CreateDirectoryCommandHandler(_repository, _unitOfWork, _settingsEncryptor);
        var command = new CreateDirectoryCommand(
            "Active Directory server", DirectorySource.ActiveDirectory, "Microsoft Active Directory",
            "kizilay.local", 389, false, "u", "p", "DC=kizilay,DC=local", null, null,
            DirectoryPermission.ReadOnlyLocalGroups, "user", "(x)", "sAMAccountName", "cn",
            "givenName", "sn", "displayName", "mail", "objectGUID", SyncScheduleKind.Daily, 0);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeEmpty();
        await _repository.Received(1).AddAsync(
            Arg.Is<Directory>(d => d.Name == "Active Directory server" && d.Source == DirectorySource.ActiveDirectory),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Internal_CreatesInternalDirectory()
    {
        var handler = new CreateDirectoryCommandHandler(_repository, _unitOfWork, _settingsEncryptor);
        var command = new CreateDirectoryCommand(
            "Internal Users", DirectorySource.Internal, null, null, 0, false, null, null, null, null, null,
            DirectoryPermission.ReadWrite, null, null, null, null, null, null, null, null, null,
            SyncScheduleKind.Off, 0);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeEmpty();
        await _repository.Received(1).AddAsync(
            Arg.Is<Directory>(d => d.Source == DirectorySource.Internal), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ActiveDirectory_EncryptsBindPasswordBeforeStoring()
    {
        _settingsEncryptor.Encrypt("gizli").Returns("ENCRYPTED");
        var handler = new CreateDirectoryCommandHandler(_repository, _unitOfWork, _settingsEncryptor);
        var command = new CreateDirectoryCommand(
            "AD", DirectorySource.ActiveDirectory, "Microsoft Active Directory",
            "kizilay.local", 389, false, "u", "gizli", "DC=kizilay,DC=local", null, null,
            DirectoryPermission.ReadOnly, "user", "(x)", "sAMAccountName", "cn",
            "givenName", "sn", "displayName", "mail", "objectGUID", SyncScheduleKind.Off, 0);

        await handler.Handle(command, CancellationToken.None);

        await _repository.Received(1).AddAsync(
            Arg.Is<Directory>(d => d.BindPasswordEncrypted == "ENCRYPTED"), Arg.Any<CancellationToken>());
    }
}
