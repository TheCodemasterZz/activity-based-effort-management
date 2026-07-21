using EforTakip.Domain.Directories;
using EforTakip.Domain.Exceptions;
using FluentAssertions;

namespace EforTakip.Domain.Tests.Directories;

public class DirectoryAttributeMappingTests
{
    [Fact]
    public void Create_WithValidData_CreatesMapping()
    {
        var mapping = DirectoryAttributeMapping.Create("company", "Kurum", "text", true, 0);

        mapping.AdAttributeName.Should().Be("company");
        mapping.SystemFieldName.Should().Be("Kurum");
        mapping.FieldType.Should().Be("text");
        mapping.IsSynced.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "Kurum")]
    [InlineData("company", "")]
    [InlineData("   ", "Kurum")]
    public void Create_WithEmptyNames_Throws(string adName, string systemName)
    {
        var act = () => DirectoryAttributeMapping.Create(adName, systemName, "text", true, 0);

        act.Should().Throw<BusinessRuleValidationException>();
    }

    [Fact]
    public void Update_ChangesValues()
    {
        var mapping = DirectoryAttributeMapping.Create("company", "Kurum", "text", true, 0);

        mapping.Update("department", "Departman", "text", false, 1);

        mapping.AdAttributeName.Should().Be("department");
        mapping.SystemFieldName.Should().Be("Departman");
        mapping.IsSynced.Should().BeFalse();
        mapping.SortOrder.Should().Be(1);
    }
}
