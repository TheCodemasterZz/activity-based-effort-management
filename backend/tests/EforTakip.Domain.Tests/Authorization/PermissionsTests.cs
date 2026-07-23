using EforTakip.Domain.Authorization;
using FluentAssertions;

namespace EforTakip.Domain.Tests.Authorization;

public class PermissionsTests
{
    [Fact]
    public void All_ContainsKnownPermissions()
    {
        Permissions.All.Should().Contain(Permissions.Role.Manage);
        Permissions.All.Should().Contain(Permissions.Project.Delete);
        Permissions.All.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void IsValidGrant_ExactCatalogKey_ReturnsTrue()
    {
        Permissions.IsValidGrant(Permissions.Project.Read).Should().BeTrue();
    }

    [Fact]
    public void IsValidGrant_ValidModuleWildcard_ReturnsTrue()
    {
        Permissions.IsValidGrant("project:*").Should().BeTrue();
    }

    [Theory]
    [InlineData("project:uçmak")]
    [InlineData("olmayanmodul:*")]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValidGrant_UnknownOrEmptyKey_ReturnsFalse(string key)
    {
        Permissions.IsValidGrant(key).Should().BeFalse();
    }
}
