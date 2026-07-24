using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Projects;
using EforTakip.Application.Tests.Directories.Commands;
using EforTakip.Application.WorkLogs.Commands.LogWork;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.Projects;
using EforTakip.Domain.Users;
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
    private readonly TestDbContext _db;
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    public LogWorkCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase($"log-work-tests-{Guid.NewGuid()}")
            .Options;
        _db = new TestDbContext(options);
    }

    private User CreateUser(bool withCalendar = true)
    {
        var user = User.CreateInternal(
            Guid.NewGuid(), $"testuser-{Guid.NewGuid():N}", "Test", "User", "Test User", null, "hash");
        if (withCalendar)
            user.AssignWorkCalendar(Guid.NewGuid());
        _db.Users.Add(user);
        _db.SaveChanges();
        return user;
    }

    private (Project project, Guid userId) CreateAssignedProject(bool withCalendar = true)
    {
        var project = Project.Create("Efor Takip Platformu", null);
        var userId = CreateUser(withCalendar).Id;
        project.AssignUser(userId);
        return (project, userId);
    }

    [Fact]
    public async Task Handle_WithSingleDay_CreatesOneWorkLog()
    {
        var (project, userId) = CreateAssignedProject();
        var activityL1 = DomainActivity.Create("Geliştirme", null, null);
        var activityL2 = DomainActivity.Create("Kod İnceleme", null, activityL1.Id);

        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        _activityRepository.GetByIdAsync(activityL1.Id, Arg.Any<CancellationToken>()).Returns(activityL1);
        _activityRepository.GetByIdAsync(activityL2.Id, Arg.Any<CancellationToken>()).Returns(activityL2);

        var handler = new LogWorkCommandHandler(_projectRepository, _activityRepository, _db, _unitOfWork);
        var date = DateOnly.FromDateTime(DateTime.Today);
        var command = new LogWorkCommand(
            userId, project.Id, activityL1.Id, activityL2.Id, date, date, 3m, "Açıklama");

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().HaveCount(1);
        _db.WorkLogs.Local.Should().HaveCount(1);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithDateRange_CreatesOneWorkLogPerDay()
    {
        var (project, userId) = CreateAssignedProject();
        var activityL1 = DomainActivity.Create("Geliştirme", null, null);
        var activityL2 = DomainActivity.Create("Kod İnceleme", null, activityL1.Id);

        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        _activityRepository.GetByIdAsync(activityL1.Id, Arg.Any<CancellationToken>()).Returns(activityL1);
        _activityRepository.GetByIdAsync(activityL2.Id, Arg.Any<CancellationToken>()).Returns(activityL2);

        var handler = new LogWorkCommandHandler(_projectRepository, _activityRepository, _db, _unitOfWork);
        var startDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-4));
        var endDate = DateOnly.FromDateTime(DateTime.Today);
        var command = new LogWorkCommand(
            userId, project.Id, activityL1.Id, activityL2.Id, startDate, endDate, 2m, "Açıklama");

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().HaveCount(5);
    }

    [Fact]
    public async Task Handle_WithUnassignedUser_ThrowsBusinessRuleValidationException()
    {
        var project = Project.Create("Efor Takip Platformu", null);
        var userId = CreateUser().Id;
        var activityL1 = DomainActivity.Create("Geliştirme", null, null);
        var activityL2 = DomainActivity.Create("Kod İnceleme", null, activityL1.Id);

        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        _activityRepository.GetByIdAsync(activityL1.Id, Arg.Any<CancellationToken>()).Returns(activityL1);
        _activityRepository.GetByIdAsync(activityL2.Id, Arg.Any<CancellationToken>()).Returns(activityL2);

        var handler = new LogWorkCommandHandler(_projectRepository, _activityRepository, _db, _unitOfWork);
        var date = DateOnly.FromDateTime(DateTime.Today);
        var command = new LogWorkCommand(
            userId, project.Id, activityL1.Id, activityL2.Id, date, date, 3m, "Açıklama");

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleValidationException>()
            .WithMessage("*projeye atanmamış*");
    }

    [Fact]
    public async Task Handle_WithUserWithoutWorkCalendar_ThrowsBusinessRuleValidationException()
    {
        var (project, userId) = CreateAssignedProject(withCalendar: false);
        var activityL1 = DomainActivity.Create("Geliştirme", null, null);
        var activityL2 = DomainActivity.Create("Kod İnceleme", null, activityL1.Id);

        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        _activityRepository.GetByIdAsync(activityL1.Id, Arg.Any<CancellationToken>()).Returns(activityL1);
        _activityRepository.GetByIdAsync(activityL2.Id, Arg.Any<CancellationToken>()).Returns(activityL2);

        var handler = new LogWorkCommandHandler(_projectRepository, _activityRepository, _db, _unitOfWork);
        var date = DateOnly.FromDateTime(DateTime.Today);
        var command = new LogWorkCommand(
            userId, project.Id, activityL1.Id, activityL2.Id, date, date, 3m, "Açıklama");

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleValidationException>()
            .WithMessage("*Mesai takvimi atanmamış*");
    }

    [Fact]
    public async Task Handle_WithNonTopLevelActivityL1_ThrowsBusinessRuleValidationException()
    {
        var (project, userId) = CreateAssignedProject();
        var parent = DomainActivity.Create("Geliştirme", null, null);
        var subActivity = DomainActivity.Create("Kod İnceleme", null, parent.Id);
        var grandchild = DomainActivity.Create("Detay", null, subActivity.Id);

        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        _activityRepository.GetByIdAsync(subActivity.Id, Arg.Any<CancellationToken>()).Returns(subActivity);
        _activityRepository.GetByIdAsync(grandchild.Id, Arg.Any<CancellationToken>()).Returns(grandchild);

        var handler = new LogWorkCommandHandler(_projectRepository, _activityRepository, _db, _unitOfWork);
        var date = DateOnly.FromDateTime(DateTime.Today);
        var command = new LogWorkCommand(
            userId, project.Id, subActivity.Id, grandchild.Id, date, date, 3m, "Açıklama");

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleValidationException>();
    }

    [Fact]
    public async Task Handle_WithActivityL2NotChildOfActivityL1_ThrowsBusinessRuleValidationException()
    {
        var (project, userId) = CreateAssignedProject();
        var activityL1 = DomainActivity.Create("Geliştirme", null, null);
        var unrelatedL1 = DomainActivity.Create("Test", null, null);
        var activityL2 = DomainActivity.Create("Test Senaryosu Yazma", null, unrelatedL1.Id);

        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        _activityRepository.GetByIdAsync(activityL1.Id, Arg.Any<CancellationToken>()).Returns(activityL1);
        _activityRepository.GetByIdAsync(activityL2.Id, Arg.Any<CancellationToken>()).Returns(activityL2);

        var handler = new LogWorkCommandHandler(_projectRepository, _activityRepository, _db, _unitOfWork);
        var date = DateOnly.FromDateTime(DateTime.Today);
        var command = new LogWorkCommand(
            userId, project.Id, activityL1.Id, activityL2.Id, date, date, 3m, "Açıklama");

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleValidationException>();
    }
}
