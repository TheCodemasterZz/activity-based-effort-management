using EforTakip.Domain.Directories;
using EforTakip.Domain.Exceptions;
using FluentAssertions;

namespace EforTakip.Domain.Tests.Directories;

public class DirectoryAttributeMappingTests
{
    private static readonly Guid DirectoryId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_CreatesMapping()
    {
        var mapping = DirectoryAttributeMapping.Create(DirectoryId, "company", "Kurum", "text", true, 0);

        mapping.DirectoryId.Should().Be(DirectoryId);
        mapping.AdAttributeName.Should().Be("company");
        mapping.SystemFieldName.Should().Be("Kurum");
        mapping.FieldType.Should().Be("text");
        mapping.IsSynced.Should().BeTrue();
    }

    [Fact]
    public void Create_WithEmptyDirectoryId_Throws()
    {
        var act = () => DirectoryAttributeMapping.Create(Guid.Empty, "company", "Kurum", "text", true, 0);

        act.Should().Throw<BusinessRuleValidationException>();
    }

    [Theory]
    [InlineData("", "Kurum")]
    [InlineData("company", "")]
    [InlineData("   ", "Kurum")]
    public void Create_WithEmptyNames_Throws(string adName, string systemName)
    {
        var act = () => DirectoryAttributeMapping.Create(DirectoryId, adName, systemName, "text", true, 0);

        act.Should().Throw<BusinessRuleValidationException>();
    }

    [Fact]
    public void Update_ChangesValues()
    {
        var mapping = DirectoryAttributeMapping.Create(DirectoryId, "company", "Kurum", "text", true, 0);

        mapping.Update("department", "Departman", "text", false, 1);

        mapping.AdAttributeName.Should().Be("department");
        mapping.SystemFieldName.Should().Be("Departman");
        mapping.IsSynced.Should().BeFalse();
        mapping.SortOrder.Should().Be(1);
    }
}
