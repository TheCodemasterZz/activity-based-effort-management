using EforTakip.Domain.Activities;
using EforTakip.Domain.Exceptions;
using FluentAssertions;

namespace EforTakip.Domain.Tests.Activities;

public class ActivityTests
{
    [Fact]
    public void Create_AsTopLevelActivity_HasNoParent()
    {
        var activity = Activity.Create("Geliştirme", null, null);

        activity.ParentActivityId.Should().BeNull();
    }

    [Fact]
    public void Create_AsSubActivity_HasParent()
    {
        var parentId = Guid.NewGuid();

        var activity = Activity.Create("Kod İnceleme", null, parentId);

        activity.ParentActivityId.Should().Be(parentId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyName_ThrowsBusinessRuleValidationException(string? name)
    {
        var act = () => Activity.Create(name!, null, null);

        act.Should().Throw<BusinessRuleValidationException>();
    }
}
