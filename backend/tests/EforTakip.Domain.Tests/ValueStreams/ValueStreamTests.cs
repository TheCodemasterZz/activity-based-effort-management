using EforTakip.Domain.Exceptions;
using EforTakip.Domain.ValueStreams;
using FluentAssertions;

namespace EforTakip.Domain.Tests.ValueStreams;

public class ValueStreamTests
{
    [Fact]
    public void Create_WithValidName_CreatesValueStream()
    {
        var valueStream = ValueStream.Create("Sipariş Yönetimi", null);

        valueStream.Name.Should().Be("Sipariş Yönetimi");
        valueStream.Stages.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyName_ThrowsBusinessRuleValidationException(string? name)
    {
        var act = () => ValueStream.Create(name!, null);

        act.Should().Throw<BusinessRuleValidationException>();
    }

    [Fact]
    public void AddStage_WithValidName_AddsStage()
    {
        var valueStream = ValueStream.Create("Sipariş Yönetimi", null);

        var stage = valueStream.AddStage("Talep Alma", 1);

        valueStream.Stages.Should().ContainSingle().Which.Should().BeSameAs(stage);
        stage.ValueStreamId.Should().Be(valueStream.Id);
    }

    [Fact]
    public void AddStage_WithDuplicateName_ThrowsBusinessRuleValidationException()
    {
        var valueStream = ValueStream.Create("Sipariş Yönetimi", null);
        valueStream.AddStage("Talep Alma", 1);

        var act = () => valueStream.AddStage("talep alma", 2);

        act.Should().Throw<BusinessRuleValidationException>();
    }
}
