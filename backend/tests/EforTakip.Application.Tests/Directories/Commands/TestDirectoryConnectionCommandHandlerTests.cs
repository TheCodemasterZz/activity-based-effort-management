using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Directories.Commands.TestDirectoryConnection;
using EforTakip.Application.Directories.Ldap;
using EforTakip.Domain.Directories;
using EforTakip.Domain.Exceptions;
using FluentAssertions;
using NSubstitute;
using Directory = EforTakip.Domain.Directories.Directory;

namespace EforTakip.Application.Tests.Directories.Commands;

public class TestDirectoryConnectionCommandHandlerTests
{
    private readonly IRepository<Directory> _repository = Substitute.For<IRepository<Directory>>();
    private readonly ILdapService _ldapService = Substitute.For<ILdapService>();

    private static Directory ValidAd() =>
        Directory.CreateActiveDirectory(
            "Ad", "Microsoft Active Directory", "kizilay.local", 389, false, "u", "p",
            "DC=kizilay,DC=local", null, null, DirectoryPermission.ReadOnly, "user", "(x)",
            "sAMAccountName", "cn", "givenName", "sn", "displayName", "mail", "objectGUID",
            SyncScheduleKind.Off, 0);

    [Fact]
    public async Task Handle_SuccessfulConnection_ReturnsSuccess()
    {
        var directory = ValidAd();
        _repository.GetByIdAsync(directory.Id, Arg.Any<CancellationToken>()).Returns(directory);
        _ldapService.TestConnectionAsync(directory, Arg.Any<CancellationToken>())
            .Returns(new LdapConnectionTestResult(true, "Bağlantı başarılı."));
        var handler = new TestDirectoryConnectionCommandHandler(_repository, _ldapService);

        var result = await handler.Handle(new TestDirectoryConnectionCommand(directory.Id), CancellationToken.None);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_FailedConnection_ReturnsFailureMessage()
    {
        var directory = ValidAd();
        _repository.GetByIdAsync(directory.Id, Arg.Any<CancellationToken>()).Returns(directory);
        _ldapService.TestConnectionAsync(directory, Arg.Any<CancellationToken>())
            .Returns(new LdapConnectionTestResult(false, "Kullanıcı adı veya şifre hatalı."));
        var handler = new TestDirectoryConnectionCommandHandler(_repository, _ldapService);

        var result = await handler.Handle(new TestDirectoryConnectionCommand(directory.Id), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Kullanıcı adı veya şifre hatalı.");
    }

    [Fact]
    public async Task Handle_NonExistingDirectory_ThrowsNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Directory?)null);
        var handler = new TestDirectoryConnectionCommandHandler(_repository, _ldapService);

        var act = async () => await handler.Handle(
            new TestDirectoryConnectionCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_InternalDirectory_ThrowsBusinessRule()
    {
        var directory = Directory.CreateInternal("Internal Users", 0);
        _repository.GetByIdAsync(directory.Id, Arg.Any<CancellationToken>()).Returns(directory);
        var handler = new TestDirectoryConnectionCommandHandler(_repository, _ldapService);

        var act = async () => await handler.Handle(
            new TestDirectoryConnectionCommand(directory.Id), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleValidationException>();
    }
}
