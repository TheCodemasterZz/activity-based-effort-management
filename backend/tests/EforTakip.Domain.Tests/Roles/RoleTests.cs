using EforTakip.Domain.Exceptions;
using EforTakip.Domain.Roles;
using FluentAssertions;

namespace EforTakip.Domain.Tests.Roles;

public class RoleTests
{
    [Fact]
    public void Create_WithValidData_CreatesRole()
    {
        var role = Role.Create("Proje Yöneticisi", "Proje CRUD yetkisi", isSystemAdmin: false);

        role.Name.Should().Be("Proje Yöneticisi");
        role.Description.Should().Be("Proje CRUD yetkisi");
        role.IsSystemAdmin.Should().BeFalse();
        role.Permissions.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyName_Throws(string name)
    {
        var act = () => Role.Create(name, null, false);

        act.Should().Throw<BusinessRuleValidationException>();
    }

    [Fact]
    public void GrantPermission_NewKey_AddsAndReturnsPermission()
    {
        var role = Role.Create("Proje Yöneticisi", null, false);

        var created = role.GrantPermission("project:read");

        created.Should().NotBeNull();
        role.Permissions.Should().ContainSingle(p => p.PermissionKey == "project:read");
    }

    [Fact]
    public void GrantPermission_AlreadyGranted_ReturnsNullAndDoesNotDuplicate()
    {
        var role = Role.Create("Proje Yöneticisi", null, false);
        role.GrantPermission("project:read");

        var second = role.GrantPermission("project:read");

        second.Should().BeNull();
        role.Permissions.Should().HaveCount(1);
    }

    [Fact]
    public void RevokePermission_RemovesGrantedPermission()
    {
        var role = Role.Create("Proje Yöneticisi", null, false);
        role.GrantPermission("project:read");

        role.RevokePermission("project:read");

        role.Permissions.Should().BeEmpty();
    }

    [Fact]
    public void HasPermission_ExactMatch_ReturnsTrue()
    {
        var role = Role.Create("Proje Yöneticisi", null, false);
        role.GrantPermission("project:read");

        role.HasPermission("project:read").Should().BeTrue();
        role.HasPermission("project:delete").Should().BeFalse();
    }

    [Fact]
    public void HasPermission_ModuleWildcard_MatchesAnyKeyInModule()
    {
        var role = Role.Create("Proje Yöneticisi", null, false);
        role.GrantPermission("project:*");

        role.HasPermission("project:delete").Should().BeTrue();
        role.HasPermission("worklog:delete").Should().BeFalse();
    }

    [Fact]
    public void HasPermission_SystemAdmin_AlwaysReturnsTrue()
    {
        var role = Role.Create("Sistem Yöneticisi", null, isSystemAdmin: true);

        role.HasPermission("herhangi:birsey").Should().BeTrue();
    }

    [Fact]
    public void Rename_UpdatesName()
    {
        var role = Role.Create("Eski Ad", null, false);

        role.Rename("Yeni Ad");

        role.Name.Should().Be("Yeni Ad");
    }
}
