using EforTakip.Domain.Exceptions;
using EforTakip.Domain.Projects;
using FluentAssertions;

namespace EforTakip.Domain.Tests.Projects;

public class ProjectTests
{
    [Fact]
    public void Create_WithValidName_CreatesActiveProject()
    {
        var project = Project.Create("Efor Takip Platformu", "Kurumsal efor takip yazılımı");

        project.Name.Should().Be("Efor Takip Platformu");
        project.Status.Should().Be(ProjectStatus.Active);
        project.CustomerIds.Should().BeEmpty();
        project.EmployeeIds.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyName_ThrowsBusinessRuleValidationException(string? name)
    {
        var act = () => Project.Create(name!, null);

        act.Should().Throw<BusinessRuleValidationException>();
    }

    [Fact]
    public void AssignCustomer_WithNewCustomer_AddsAssignment()
    {
        var project = Project.Create("Efor Takip Platformu", null);
        var customerId = Guid.NewGuid();

        var assignment = project.AssignCustomer(customerId);

        project.CustomerIds.Should().ContainSingle().Which.Should().Be(customerId);
        assignment.ProjectId.Should().Be(project.Id);
        assignment.CustomerId.Should().Be(customerId);
    }

    [Fact]
    public void AssignCustomer_WithAlreadyAssignedCustomer_ThrowsBusinessRuleValidationException()
    {
        var project = Project.Create("Efor Takip Platformu", null);
        var customerId = Guid.NewGuid();
        project.AssignCustomer(customerId);

        var act = () => project.AssignCustomer(customerId);

        act.Should().Throw<BusinessRuleValidationException>();
    }

    [Fact]
    public void AssignEmployee_WithNewEmployee_AddsAssignment()
    {
        var project = Project.Create("Efor Takip Platformu", null);
        var employeeId = Guid.NewGuid();

        var assignment = project.AssignEmployee(employeeId);

        project.EmployeeIds.Should().ContainSingle().Which.Should().Be(employeeId);
        assignment.ProjectId.Should().Be(project.Id);
        assignment.EmployeeId.Should().Be(employeeId);
    }

    [Fact]
    public void AssignEmployee_WithAlreadyAssignedEmployee_ThrowsBusinessRuleValidationException()
    {
        var project = Project.Create("Efor Takip Platformu", null);
        var employeeId = Guid.NewGuid();
        project.AssignEmployee(employeeId);

        var act = () => project.AssignEmployee(employeeId);

        act.Should().Throw<BusinessRuleValidationException>();
    }

    [Fact]
    public void Complete_OnActiveProject_SetsStatusToCompleted()
    {
        var project = Project.Create("Efor Takip Platformu", null);

        project.Complete();

        project.Status.Should().Be(ProjectStatus.Completed);
    }

    [Fact]
    public void Complete_OnAlreadyCompletedProject_ThrowsBusinessRuleValidationException()
    {
        var project = Project.Create("Efor Takip Platformu", null);
        project.Complete();

        var act = project.Complete;

        act.Should().Throw<BusinessRuleValidationException>();
    }

    [Fact]
    public void Cancel_OnCompletedProject_ThrowsBusinessRuleValidationException()
    {
        var project = Project.Create("Efor Takip Platformu", null);
        project.Complete();

        var act = project.Cancel;

        act.Should().Throw<BusinessRuleValidationException>();
    }
}
