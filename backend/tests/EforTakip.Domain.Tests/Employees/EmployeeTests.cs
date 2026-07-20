using EforTakip.Domain.Employees;
using EforTakip.Domain.Exceptions;
using FluentAssertions;

namespace EforTakip.Domain.Tests.Employees;

public class EmployeeTests
{
    [Fact]
    public void Create_WithValidName_CreatesEmployee()
    {
        var workCalendarId = Guid.NewGuid();

        var employee = Employee.Create("Ayşe Yılmaz", "ayse.yilmaz@example.com", workCalendarId);

        employee.Name.Should().Be("Ayşe Yılmaz");
        employee.Email.Should().Be("ayse.yilmaz@example.com");
        employee.WorkCalendarId.Should().Be(workCalendarId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyName_ThrowsBusinessRuleValidationException(string? name)
    {
        var act = () => Employee.Create(name!, null, Guid.NewGuid());

        act.Should().Throw<BusinessRuleValidationException>();
    }

    [Fact]
    public void Create_WithEmptyWorkCalendarId_ThrowsBusinessRuleValidationException()
    {
        var act = () => Employee.Create("Ayşe Yılmaz", null, Guid.Empty);

        act.Should().Throw<BusinessRuleValidationException>();
    }
}
