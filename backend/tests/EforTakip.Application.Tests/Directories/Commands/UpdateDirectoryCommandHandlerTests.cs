using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Directories.Commands.UpdateDirectory;
using EforTakip.Domain.Directories;
using EforTakip.Domain.Exceptions;
using FluentAssertions;
using NSubstitute;
using Directory = EforTakip.Domain.Directories.Directory;

namespace EforTakip.Application.Tests.Directories.Commands;

public class UpdateDirectoryCommandHandlerTests
{
    private readonly IRepository<Directory> _repository = Substitute.For<IRepository<Directory>>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private static Directory ExistingAd() =>
        Directory.CreateActiveDirectory(
            "Eski", "Microsoft Active Directory", "eski.local", 389, false, "u", "ENC(x)",
            "DC=eski,DC=local", null, null, DirectoryPermission.ReadOnly, "user", "(x)",
            "sAMAccountName", "cn", "givenName", "sn", "displayName", "mail", "objectGUID",
            SyncScheduleKind.Off, 0);

    private static UpdateDirectoryCommand Command(Guid id) =>
        new(id, "Yeni Ad", DirectorySource.ActiveDirectory, "Microsoft Active Directory",
            "yeni.local", 636, true, "u2", null, "DC=yeni,DC=local", null, null,
            DirectoryPermission.ReadWrite, "user", "(x)", "sAMAccountName", "cn", "givenName",
            "sn", "displayName", "mail", "objectGUID", SyncScheduleKind.Hourly);

    [Fact]
    public async Task Handle_ExistingDirectory_Updates()
    {
        var directory = ExistingAd();
        _repository.GetByIdAsync(directory.Id, Arg.Any<CancellationToken>()).Returns(directory);
        var handler = new UpdateDirectoryCommandHandler(_repository, _unitOfWork);

        await handler.Handle(Command(directory.Id), CancellationToken.None);

        directory.Name.Should().Be("Yeni Ad");
        directory.Port.Should().Be(636);
        _repository.Received(1).Update(directory);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NonExisting_ThrowsNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Directory?)null);
        var handler = new UpdateDirectoryCommandHandler(_repository, _unitOfWork);

        var act = async () => await handler.Handle(Command(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
