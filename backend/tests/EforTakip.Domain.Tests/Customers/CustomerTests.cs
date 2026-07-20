using EforTakip.Domain.Customers;
using EforTakip.Domain.Exceptions;
using FluentAssertions;

namespace EforTakip.Domain.Tests.Customers;

public class CustomerTests
{
    [Fact]
    public void Create_WithValidName_CreatesCustomer()
    {
        var customer = Customer.Create("Acme A.Ş.");

        customer.Name.Should().Be("Acme A.Ş.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyName_ThrowsBusinessRuleValidationException(string? name)
    {
        var act = () => Customer.Create(name!);

        act.Should().Throw<BusinessRuleValidationException>();
    }
}
