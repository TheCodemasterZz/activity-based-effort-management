using EforTakip.Domain.Directories;
using EforTakip.Domain.Exceptions;
using FluentAssertions;

namespace EforTakip.Domain.Tests.Directories;

public class DirectoryUserTests
{
    [Fact]
    public void CreateFromActiveDirectory_WithValidData_CreatesUser()
    {
        var directoryId = Guid.NewGuid();

        var user = DirectoryUser.CreateFromActiveDirectory(
            directoryId, "serkan.gultepe", "Serkan", "Gültepe",
            "Serkan Gültepe", "serkan@kizilay.org.tr", "guid-123");

        user.DirectoryId.Should().Be(directoryId);
        user.Source.Should().Be(DirectorySource.ActiveDirectory);
        user.Username.Should().Be("serkan.gultepe");
        user.ObjectGuid.Should().Be("guid-123");
        user.PasswordHash.Should().BeNull();
        user.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CreateFromActiveDirectory_WithEmptyUsername_Throws(string? username)
    {
        var act = () => DirectoryUser.CreateFromActiveDirectory(
            Guid.NewGuid(), username!, "a", "b", "c", "d", "guid");

        act.Should().Throw<BusinessRuleValidationException>();
    }

    [Fact]
    public void CreateInternal_WithValidData_CreatesUserWithPasswordHash()
    {
        var user = DirectoryUser.CreateInternal(
            Guid.NewGuid(), "sanal.kullanici", "Sanal", "Kullanıcı",
            "Sanal Kullanıcı", null, "HASHED");

        user.Source.Should().Be(DirectorySource.Internal);
        user.PasswordHash.Should().Be("HASHED");
        user.ObjectGuid.Should().BeNull();
    }

    [Fact]
    public void UpdateFromSync_UpdatesFieldsAndLastSynced()
    {
        var user = DirectoryUser.CreateFromActiveDirectory(
            Guid.NewGuid(), "serkan.gultepe", "Serkan", "Eski", "x", "eski@x.com", "guid");
        var syncTime = DateTime.UtcNow;

        user.UpdateFromSync("Serkan", "Yeni", "Serkan Yeni", "yeni@x.com", isEnabled: true, syncTime);

        user.LastName.Should().Be("Yeni");
        user.Email.Should().Be("yeni@x.com");
        user.LastSyncedUtc.Should().Be(syncTime);
    }

    [Theory]
    [InlineData("SERKAN.GULTEPE", "serkan.gultepe")]
    [InlineData("  Serkan.Gultepe  ", "serkan.gultepe")]
    [InlineData("KULLANICI", "kullanici")]
    public void CreateFromActiveDirectory_NormalizesUsernameInvariantly(string input, string expected)
    {
        var user = DirectoryUser.CreateFromActiveDirectory(
            Guid.NewGuid(), input, "Ad", "Soyad", "Ad Soyad", null, "guid");

        // Türkçe kültürde ToLower 'I' harfini noktasız 'ı'ya çevirir; invariant olmalı.
        user.Username.Should().Be(expected);
    }

    [Fact]
    public void CreateInternal_NormalizesUsernameInvariantly()
    {
        var user = DirectoryUser.CreateInternal(
            Guid.NewGuid(), "SANAL.KULLANICI", null, null, null, null, "HASHED");

        user.Username.Should().Be("sanal.kullanici");
    }

    [Fact]
    public void SetPassword_OnInternalUser_ReplacesHash()
    {
        var user = DirectoryUser.CreateInternal(
            Guid.NewGuid(), "sanal.kullanici", null, null, null, null, "ESKI_HASH");

        user.SetPassword("YENI_HASH");

        user.PasswordHash.Should().Be("YENI_HASH");
    }

    [Fact]
    public void SetPassword_OnActiveDirectoryUser_Throws()
    {
        var user = DirectoryUser.CreateFromActiveDirectory(
            Guid.NewGuid(), "serkan.gultepe", "Serkan", "Gültepe", "Serkan Gültepe", null, "guid");

        // AD kullanıcısının şifresi dizinde tutulur; sistemde şifre atanamaz.
        var act = () => user.SetPassword("YENI_HASH");

        act.Should().Throw<BusinessRuleValidationException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SetPassword_WithEmptyHash_Throws(string hash)
    {
        var user = DirectoryUser.CreateInternal(
            Guid.NewGuid(), "sanal.kullanici", null, null, null, null, "ESKI_HASH");

        var act = () => user.SetPassword(hash);

        act.Should().Throw<BusinessRuleValidationException>();
    }

    [Fact]
    public void SetAttribute_WithNewMapping_AddsAttribute()
    {
        var user = DirectoryUser.CreateFromActiveDirectory(
            Guid.NewGuid(), "serkan.gultepe", "Serkan", "Gültepe", "Serkan Gültepe", null, "guid");
        var mappingId = Guid.NewGuid();

        user.SetAttribute(mappingId, "Kızılay");

        user.Attributes.Should().ContainSingle();
        user.Attributes.Single().AttributeMappingId.Should().Be(mappingId);
        user.Attributes.Single().Value.Should().Be("Kızılay");
    }

    [Fact]
    public void SetAttribute_WithExistingMapping_UpdatesInPlace()
    {
        var user = DirectoryUser.CreateFromActiveDirectory(
            Guid.NewGuid(), "serkan.gultepe", "Serkan", "Gültepe", "Serkan Gültepe", null, "guid");
        var mappingId = Guid.NewGuid();
        user.SetAttribute(mappingId, "Eski Kurum");

        user.SetAttribute(mappingId, "Yeni Kurum");

        user.Attributes.Should().ContainSingle();
        user.Attributes.Single().Value.Should().Be("Yeni Kurum");
    }

    [Fact]
    public void SetAttribute_WithReferencedUser_StoresReference()
    {
        var user = DirectoryUser.CreateFromActiveDirectory(
            Guid.NewGuid(), "serkan.gultepe", "Serkan", "Gültepe", "Serkan Gültepe", null, "guid");
        var mappingId = Guid.NewGuid();
        var managerId = Guid.NewGuid();

        user.SetAttribute(mappingId, "Gökhan Yetkin", managerId);

        user.Attributes.Single().ReferencedDirectoryUserId.Should().Be(managerId);
    }

    [Fact]
    public void ClearAttributes_RemovesAll()
    {
        var user = DirectoryUser.CreateFromActiveDirectory(
            Guid.NewGuid(), "serkan.gultepe", "Serkan", "Gültepe", "Serkan Gültepe", null, "guid");
        user.SetAttribute(Guid.NewGuid(), "a");
        user.SetAttribute(Guid.NewGuid(), "b");

        user.ClearAttributes();

        user.Attributes.Should().BeEmpty();
    }

    [Fact]
    public void UpdateFromSync_WhenDisabledInDirectory_DeactivatesUser()
    {
        var user = DirectoryUser.CreateFromActiveDirectory(
            Guid.NewGuid(), "serkan.gultepe", "Serkan", "Gültepe", "Serkan Gültepe", null, "guid");

        user.UpdateFromSync("Serkan", "Gültepe", "Serkan Gültepe", null, isEnabled: false, DateTime.UtcNow);

        user.IsActive.Should().BeFalse();
    }

    [Fact]
    public void UpdateFromSync_WhenReEnabledInDirectory_ReactivatesUser()
    {
        var user = DirectoryUser.CreateFromActiveDirectory(
            Guid.NewGuid(), "serkan.gultepe", "Serkan", "Gültepe", "Serkan Gültepe", null, "guid");
        user.Deactivate();

        user.UpdateFromSync("Serkan", "Gültepe", "Serkan Gültepe", null, isEnabled: true, DateTime.UtcNow);

        user.IsActive.Should().BeTrue();
    }
}
