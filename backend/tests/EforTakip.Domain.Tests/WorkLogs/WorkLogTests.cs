using EforTakip.Domain.Exceptions;
using EforTakip.Domain.WorkLogs;
using FluentAssertions;

namespace EforTakip.Domain.Tests.WorkLogs;

public class WorkLogTests
{
    [Fact]
    public void Create_WithValidData_CreatesWorkLog()
    {
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var activityL1Id = Guid.NewGuid();
        var activityL2Id = Guid.NewGuid();
        var workDate = DateOnly.FromDateTime(DateTime.Today);

        var log = WorkLog.Create(
            userId, projectId, activityL1Id, activityL2Id, workDate, 4m, "Kod inceleme yapıldı.");

        log.UserId.Should().Be(userId);
        log.ProjectId.Should().Be(projectId);
        log.ActivityL1Id.Should().Be(activityL1Id);
        log.ActivityL2Id.Should().Be(activityL2Id);
        log.WorkDate.Should().Be(workDate);
        log.Hours.Should().Be(4m);
        log.Description.Should().Be("Kod inceleme yapıldı.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(24.01)]
    public void Create_WithInvalidHours_ThrowsBusinessRuleValidationException(double hours)
    {
        var act = () => WorkLog.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            DateOnly.FromDateTime(DateTime.Today), (decimal)hours, "Açıklama");

        act.Should().Throw<BusinessRuleValidationException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyDescription_ThrowsBusinessRuleValidationException(string? description)
    {
        var act = () => WorkLog.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            DateOnly.FromDateTime(DateTime.Today), 4m, description!);

        act.Should().Throw<BusinessRuleValidationException>();
    }
}
