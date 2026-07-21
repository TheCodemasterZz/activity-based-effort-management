using EforTakip.Domain.Directories;
using EforTakip.Domain.Exceptions;
using FluentAssertions;
using Directory = EforTakip.Domain.Directories.Directory;

namespace EforTakip.Domain.Tests.Directories;

public class DirectoryTests
{
    private static Directory CreateValidAd() =>
        Directory.CreateActiveDirectory(
            name: "Active Directory server",
            directoryType: "Microsoft Active Directory",
            hostname: "kizilay.local",
            port: 389,
            useSsl: false,
            bindUsername: "jira_user@kizilay.org.tr",
            bindPasswordEncrypted: "ENC(secret)",
            baseDn: "DC=kizilay,DC=local",
            additionalUserDn: null,
            additionalGroupDn: null,
            permission: DirectoryPermission.ReadOnlyLocalGroups,
            userObjectClass: "user",
            userObjectFilter: "(&(objectCategory=Person)(sAMAccountName=*))",
            usernameAttribute: "sAMAccountName",
            usernameRdnAttribute: "cn",
            firstNameAttribute: "givenName",
            lastNameAttribute: "sn",
            displayNameAttribute: "displayName",
            emailAttribute: "mail",
            uniqueIdAttribute: "objectGUID",
            syncSchedule: SyncScheduleKind.Daily,
            sortOrder: 0);

    [Fact]
    public void CreateActiveDirectory_WithValidData_CreatesDirectory()
    {
        var directory = CreateValidAd();

        directory.Name.Should().Be("Active Directory server");
        directory.Source.Should().Be(DirectorySource.ActiveDirectory);
        directory.Hostname.Should().Be("kizilay.local");
        directory.Port.Should().Be(389);
        directory.Permission.Should().Be(DirectoryPermission.ReadOnlyLocalGroups);
        directory.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CreateActiveDirectory_WithEmptyName_Throws(string? name)
    {
        var act = () => Directory.CreateActiveDirectory(
            name!, "Microsoft Active Directory", "kizilay.local", 389, false,
            "u", "ENC(x)", "DC=kizilay,DC=local", null, null,
            DirectoryPermission.ReadOnly, "user", "(x)", "sAMAccountName", "cn",
            "givenName", "sn", "displayName", "mail", "objectGUID", SyncScheduleKind.Off, 0);

        act.Should().Throw<BusinessRuleValidationException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(70000)]
    public void CreateActiveDirectory_WithInvalidPort_Throws(int port)
    {
        var act = () => Directory.CreateActiveDirectory(
            "Ad", "Microsoft Active Directory", "kizilay.local", port, false,
            "u", "ENC(x)", "DC=kizilay,DC=local", null, null,
            DirectoryPermission.ReadOnly, "user", "(x)", "sAMAccountName", "cn",
            "givenName", "sn", "displayName", "mail", "objectGUID", SyncScheduleKind.Off, 0);

        act.Should().Throw<BusinessRuleValidationException>();
    }

    [Fact]
    public void CreateInternal_CreatesInternalDirectory()
    {
        var directory = Directory.CreateInternal("Internal Users", 0);

        directory.Source.Should().Be(DirectorySource.Internal);
        directory.Name.Should().Be("Internal Users");
        directory.IsActive.Should().BeTrue();
    }

    [Fact]
    public void UpdateActiveDirectorySettings_WithNullPassword_KeepsExistingPassword()
    {
        var directory = CreateValidAd();

        directory.UpdateActiveDirectorySettings(
            "Yeni Ad", "Microsoft Active Directory", "yeni.local", 636, true,
            "u2", null, "DC=yeni,DC=local", null, null,
            DirectoryPermission.ReadWrite, "user", "(x)", "sAMAccountName", "cn",
            "givenName", "sn", "displayName", "mail", "objectGUID", SyncScheduleKind.Hourly);

        directory.Name.Should().Be("Yeni Ad");
        directory.Port.Should().Be(636);
        directory.BindPasswordEncrypted.Should().Be("ENC(secret)");
    }

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        var directory = CreateValidAd();

        directory.Deactivate();

        directory.IsActive.Should().BeFalse();
    }

    [Fact]
    public void IsSyncDue_WithScheduleOff_ReturnsFalse()
    {
        var directory = Directory.CreateActiveDirectory(
            "Ad", "Microsoft Active Directory", "kizilay.local", 389, false, "u", "ENC(x)",
            "DC=kizilay,DC=local", null, null, DirectoryPermission.ReadOnly, "user", "(x)",
            "sAMAccountName", "cn", "givenName", "sn", "displayName", "mail", "objectGUID",
            SyncScheduleKind.Off, 0);

        directory.IsSyncDue(DateTime.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void IsSyncDue_NeverSynced_ReturnsTrue()
    {
        var directory = CreateValidAd();

        directory.IsSyncDue(DateTime.UtcNow).Should().BeTrue();
    }

    [Fact]
    public void IsSyncDue_DailyAndSyncedRecently_ReturnsFalse()
    {
        var directory = CreateValidAd();
        var now = DateTime.UtcNow;
        directory.MarkSynced(now.AddHours(-2));

        directory.IsSyncDue(now).Should().BeFalse();
    }

    [Fact]
    public void IsSyncDue_DailyAndSyncedLongAgo_ReturnsTrue()
    {
        var directory = CreateValidAd();
        var now = DateTime.UtcNow;
        directory.MarkSynced(now.AddDays(-2));

        directory.IsSyncDue(now).Should().BeTrue();
    }

    [Fact]
    public void IsSyncDue_InactiveDirectory_ReturnsFalse()
    {
        var directory = CreateValidAd();
        directory.Deactivate();

        directory.IsSyncDue(DateTime.UtcNow).Should().BeFalse();
    }
}
