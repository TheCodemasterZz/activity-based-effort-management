using EforTakip.Application.Directories.Commands.CreateDirectory;
using EforTakip.Domain.Directories;
using FluentValidation.TestHelper;

namespace EforTakip.Application.Tests.Directories.Commands;

public class CreateDirectoryCommandValidatorTests
{
    private readonly CreateDirectoryCommandValidator _validator = new();

    private static CreateDirectoryCommand AdCommand(string name = "Ad", string? hostname = "kizilay.local", int port = 389) =>
        new(name, DirectorySource.ActiveDirectory, "Microsoft Active Directory", hostname, port, false,
            "u", "p", "DC=kizilay,DC=local", null, null, DirectoryPermission.ReadOnly,
            "user", "(x)", "sAMAccountName", "cn", "givenName", "sn", "displayName", "mail",
            "objectGUID", SyncScheduleKind.Off, 0);

    [Fact]
    public void ValidAdCommand_Passes()
    {
        _validator.TestValidate(AdCommand()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyName_Fails()
    {
        _validator.TestValidate(AdCommand(name: "")).ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void ActiveDirectoryWithoutHostname_Fails()
    {
        _validator.TestValidate(AdCommand(hostname: null)).ShouldHaveValidationErrorFor(x => x.Hostname);
    }

    [Fact]
    public void InvalidPort_Fails()
    {
        _validator.TestValidate(AdCommand(port: 0)).ShouldHaveValidationErrorFor(x => x.Port);
    }
}
