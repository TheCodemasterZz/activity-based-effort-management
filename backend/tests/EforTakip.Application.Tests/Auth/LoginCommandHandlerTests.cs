using EforTakip.Application.Auth.Commands.Login;
using EforTakip.Application.Common.Exceptions;
using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Common.Models;
using EforTakip.Application.Directories.Ldap;
using EforTakip.Application.Tests.Directories.Commands;
using EforTakip.Domain.Directories;
using EforTakip.Domain.Roles;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Directory = EforTakip.Domain.Directories.Directory;

namespace EforTakip.Application.Tests.Auth;

public class LoginCommandHandlerTests : IAsyncDisposable
{
    private readonly IRepository<Directory> _directoryRepository = Substitute.For<IRepository<Directory>>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly ILdapService _ldapService = Substitute.For<ILdapService>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly TestDbContext _db;

    public LoginCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase($"login-tests-{Guid.NewGuid()}")
            .Options;
        _db = new TestDbContext(options);

        _tokenService.CreateToken(Arg.Any<AuthenticatedUser>())
            .Returns(("TOKEN", DateTime.UtcNow.AddHours(8)));
    }

    public async ValueTask DisposeAsync() => await _db.DisposeAsync();

    private LoginCommandHandler CreateHandler()
        => new(_db, _directoryRepository, _passwordHasher, _ldapService, _tokenService);

    private static Directory InternalDirectory() => Directory.CreateInternal("Internal Users", 0);

    private static Directory AdDirectory() =>
        Directory.CreateActiveDirectory(
            "Kızılay AD", "Microsoft Active Directory", "kizilay.local", 389, false, "u", "p",
            "DC=kizilay,DC=local", null, null, DirectoryPermission.ReadOnly, "user", "(x)",
            "sAMAccountName", "cn", "givenName", "sn", "displayName", "mail", "objectGUID",
            SyncScheduleKind.Off, 0);

    /// <summary>Kullanıcıyı ekler ve dizinini repository mock'una tanıtır (handler dizini yükler).</summary>
    private async Task<DirectoryUser> AddInternalUserAsync(Directory directory, string username = "sanal.kullanici")
    {
        var user = DirectoryUser.CreateInternal(
            directory.Id, username, "Sanal", "Kullanıcı", "Sanal Kullanıcı", null, "HASHED");
        _db.DirectoryUsers.Add(user);
        await _db.SaveChangesAsync();
        _directoryRepository.GetByIdAsync(directory.Id, Arg.Any<CancellationToken>()).Returns(directory);
        return user;
    }

    private async Task<DirectoryUser> AddAdUserAsync(Directory directory, string username = "serkan.gultepe")
    {
        var user = DirectoryUser.CreateFromActiveDirectory(
            directory.Id, username, "Serkan", "Gültepe", "Serkan Gültepe", null, "guid-1");
        _db.DirectoryUsers.Add(user);
        await _db.SaveChangesAsync();
        _directoryRepository.GetByIdAsync(directory.Id, Arg.Any<CancellationToken>()).Returns(directory);
        return user;
    }

    [Fact]
    public async Task Handle_InternalUserWithCorrectPassword_ReturnsToken()
    {
        var user = await AddInternalUserAsync(InternalDirectory());
        _passwordHasher.Verify("dogru-sifre", "HASHED").Returns(true);

        var result = await CreateHandler().Handle(
            new LoginCommand("sanal.kullanici", "dogru-sifre"), CancellationToken.None);

        result.Token.Should().Be("TOKEN");
        result.UserId.Should().Be(user.Id);
        result.Source.Should().Be(DirectorySource.Internal);
    }

    [Fact]
    public async Task Handle_InternalUserWithWrongPassword_ThrowsAuthenticationFailed()
    {
        await AddInternalUserAsync(InternalDirectory());
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var act = async () => await CreateHandler().Handle(
            new LoginCommand("sanal.kullanici", "yanlis"), CancellationToken.None);

        await act.Should().ThrowAsync<AuthenticationFailedException>();
    }

    [Fact]
    public async Task Handle_AdUserWithValidCredentials_ReturnsToken()
    {
        var directory = AdDirectory();
        var user = await AddAdUserAsync(directory);
        _ldapService.AuthenticateAsync(directory, "serkan.gultepe", "ad-sifre", Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await CreateHandler().Handle(
            new LoginCommand("serkan.gultepe", "ad-sifre"), CancellationToken.None);

        result.Token.Should().Be("TOKEN");
        result.UserId.Should().Be(user.Id);
        result.Source.Should().Be(DirectorySource.ActiveDirectory);
    }

    [Fact]
    public async Task Handle_AdUserRejectedByDirectory_ThrowsAuthenticationFailed()
    {
        await AddAdUserAsync(AdDirectory());
        _ldapService.AuthenticateAsync(
                Arg.Any<Directory>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var act = async () => await CreateHandler().Handle(
            new LoginCommand("serkan.gultepe", "yanlis"), CancellationToken.None);

        await act.Should().ThrowAsync<AuthenticationFailedException>();
    }

    [Fact]
    public async Task Handle_AdUserPasswordIsNeverCheckedLocally()
    {
        await AddAdUserAsync(AdDirectory());
        _ldapService.AuthenticateAsync(
                Arg.Any<Directory>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);

        await CreateHandler().Handle(new LoginCommand("serkan.gultepe", "ad-sifre"), CancellationToken.None);

        _passwordHasher.DidNotReceive().Verify(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_InactiveUser_ThrowsAuthenticationFailed()
    {
        var user = await AddInternalUserAsync(InternalDirectory());
        user.Deactivate();
        await _db.SaveChangesAsync();
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        var act = async () => await CreateHandler().Handle(
            new LoginCommand("sanal.kullanici", "dogru-sifre"), CancellationToken.None);

        await act.Should().ThrowAsync<AuthenticationFailedException>();
    }

    [Fact]
    public async Task Handle_InactiveDirectory_ThrowsAuthenticationFailed()
    {
        var directory = InternalDirectory();
        await AddInternalUserAsync(directory);
        directory.Deactivate();
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        var act = async () => await CreateHandler().Handle(
            new LoginCommand("sanal.kullanici", "dogru-sifre"), CancellationToken.None);

        await act.Should().ThrowAsync<AuthenticationFailedException>();
    }

    [Fact]
    public async Task Handle_UnknownUsername_ThrowsAuthenticationFailedWithSameMessage()
    {
        var act = async () => await CreateHandler().Handle(
            new LoginCommand("olmayan.kullanici", "herhangi"), CancellationToken.None);

        var assertion = await act.Should().ThrowAsync<AuthenticationFailedException>();
        assertion.Which.Message.Should().Be("Kullanıcı adı veya şifre hatalı.");
    }

    [Theory]
    [InlineData("Sanal.Kullanici")]
    [InlineData("SANAL.KULLANICI")]
    [InlineData("  sanal.kullanici  ")]
    public async Task Handle_UsernameIsMatchedCaseInsensitively(string typedUsername)
    {
        await AddInternalUserAsync(InternalDirectory());
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        var result = await CreateHandler().Handle(
            new LoginCommand(typedUsername, "dogru-sifre"), CancellationToken.None);

        result.Username.Should().Be("sanal.kullanici");
    }

    [Fact]
    public async Task Handle_UserWithRole_PassesGrantedPermissionsToTokenService()
    {
        var directory = InternalDirectory();
        var user = await AddInternalUserAsync(directory);
        var role = Role.Create("Proje Yöneticisi", null, false);
        role.GrantPermission("project:read");
        var assignment = user.AssignRole(role.Id);
        _db.Roles.Add(role);
        _db.DirectoryUserRoles.Add(assignment!);
        await _db.SaveChangesAsync();
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        await CreateHandler().Handle(new LoginCommand("sanal.kullanici", "dogru-sifre"), CancellationToken.None);

        _tokenService.Received(1).CreateToken(Arg.Is<AuthenticatedUser>(u =>
            !u.IsSystemAdmin && u.PermissionKeys.Contains("project:read")));
    }
}
