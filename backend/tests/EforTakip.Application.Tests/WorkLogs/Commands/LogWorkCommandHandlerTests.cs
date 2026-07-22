using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Projects;
using EforTakip.Application.WorkLogs.Commands.LogWork;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.Projects;
using EforTakip.Domain.WorkLogs;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using DomainActivity = EforTakip.Domain.Activities.Activity;

namespace EforTakip.Application.Tests.WorkLogs.Commands;

public class LogWorkCommandHandlerTests
{
    private readonly IProjectRepository _projectRepository = Substitute.For<IProjectRepository>();
    private readonly IRepository<DomainActivity> _activityRepository = Substitute.For<IRepository<DomainActivity>>();
    private readonly IApplicationDbContext _db = Substitute.For<IApplicationDbContext>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    public LogWorkCommandHandlerTests()
    {
        _db.EmployeeWorkLogs.Returns(Substitute.For<DbSet<EmployeeWorkLog>>());
    }

    private (Project project, Guid employeeId) CreateAssignedProject()
    {
        var project = Project.Create("Efor Takip Platformu", null);
        var employeeId = Guid.NewGuid();
        project.AssignEmployee(employeeId);
        return (project, employeeId);
    }

    [Fact]
    public async Task Handle_WithSingleDay_CreatesOneWorkLog()
    {
        var (project, employeeId) = CreateAssignedProject();
        var activityL1 = DomainActivity.Create("Geliştirme", null, null);
        var activityL2 = DomainActivity.Create("Kod İnceleme", null, activityL1.Id);

        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        _activityRepository.GetByIdAsync(activityL1.Id, Arg.Any<CancellationToken>()).Returns(activityL1);
        _activityRepository.GetByIdAsync(activityL2.Id, Arg.Any<CancellationToken>()).Returns(activityL2);

        var handler = new LogWorkCommandHandler(_projectRepository, _activityRepository, _db, _unitOfWork);
        var date = DateOnly.FromDateTime(DateTime.Today);
        var command = new LogWorkCommand(
            employeeId, project.Id, activityL1.Id, activityL2.Id, date, date, 3m, "Açıklama");

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().HaveCount(1);
        _db.EmployeeWorkLogs.Received(1).AddRange(Arg.Is<IEnumerable<EmployeeWorkLog>>(logs => logs.Count() == 1));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithDateRange_CreatesOneWorkLogPerDay()
    {
        var (project, employeeId) = CreateAssignedProject();
        var activityL1 = DomainActivity.Create("Geliştirme", null, null);
        var activityL2 = DomainActivity.Create("Kod İnceleme", null, activityL1.Id);

        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        _activityRepository.GetByIdAsync(activityL1.Id, Arg.Any<CancellationToken>()).Returns(activityL1);
        _activityRepository.GetByIdAsync(activityL2.Id, Arg.Any<CancellationToken>()).Returns(activityL2);

        var handler = new LogWorkCommandHandler(_projectRepository, _activityRepository, _db, _unitOfWork);
        var startDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-4));
        var endDate = DateOnly.FromDateTime(DateTime.Today);
        var command = new LogWorkCommand(
            employeeId, project.Id, activityL1.Id, activityL2.Id, startDate, endDate, 2m, "Açıklama");

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().HaveCount(5);
    }

    [Fact]
    public async Task Handle_WithUnassignedEmployee_ThrowsBusinessRuleValidationException()
    {
        var project = Project.Create("Efor Takip Platformu", null);
        var activityL1 = DomainActivity.Create("Geliştirme", null, null);
        var activityL2 = DomainActivity.Create("Kod İnceleme", null, activityL1.Id);

        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        _activityRepository.GetByIdAsync(activityL1.Id, Arg.Any<CancellationToken>()).Returns(activityL1);
        _activityRepository.GetByIdAsync(activityL2.Id, Arg.Any<CancellationToken>()).Returns(activityL2);

        var handler = new LogWorkCommandHandler(_projectRepository, _activityRepository, _db, _unitOfWork);
        var date = DateOnly.FromDateTime(DateTime.Today);
        var command = new LogWorkCommand(
            Guid.NewGuid(), project.Id, activityL1.Id, activityL2.Id, date, date, 3m, "Açıklama");

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleValidationException>();
    }

    [Fact]
    public async Task Handle_WithNonTopLevelActivityL1_ThrowsBusinessRuleValidationException()
    {
        var (project, employeeId) = CreateAssignedProject();
        var parent = DomainActivity.Create("Geliştirme", null, null);
        var subActivity = DomainActivity.Create("Kod İnceleme", null, parent.Id);
        var grandchild = DomainActivity.Create("Detay", null, subActivity.Id);

        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        _activityRepository.GetByIdAsync(subActivity.Id, Arg.Any<CancellationToken>()).Returns(subActivity);
        _activityRepository.GetByIdAsync(grandchild.Id, Arg.Any<CancellationToken>()).Returns(grandchild);

        var handler = new LogWorkCommandHandler(_projectRepository, _activityRepository, _db, _unitOfWork);
        var date = DateOnly.FromDateTime(DateTime.Today);
        var command = new LogWorkCommand(
            employeeId, project.Id, subActivity.Id, grandchild.Id, date, date, 3m, "Açıklama");

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleValidationException>();
    }

    [Fact]
    public async Task Handle_WithActivityL2NotChildOfActivityL1_ThrowsBusinessRuleValidationException()
    {
        var (project, employeeId) = CreateAssignedProject();
        var activityL1 = DomainActivity.Create("Geliştirme", null, null);
        var unrelatedL1 = DomainActivity.Create("Test", null, null);
        var activityL2 = DomainActivity.Create("Test Senaryosu Yazma", null, unrelatedL1.Id);

        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        _activityRepository.GetByIdAsync(activityL1.Id, Arg.Any<CancellationToken>()).Returns(activityL1);
        _activityRepository.GetByIdAsync(activityL2.Id, Arg.Any<CancellationToken>()).Returns(activityL2);

        var handler = new LogWorkCommandHandler(_projectRepository, _activityRepository, _db, _unitOfWork);
        var date = DateOnly.FromDateTime(DateTime.Today);
        var command = new LogWorkCommand(
            employeeId, project.Id, activityL1.Id, activityL2.Id, date, date, 3m, "Açıklama");

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleValidationException>();
    }
}
