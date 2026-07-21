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
