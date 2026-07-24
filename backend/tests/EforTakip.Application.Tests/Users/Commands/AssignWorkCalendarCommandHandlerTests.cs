using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Tests.Directories.Commands;
using EforTakip.Application.Users.Commands.AssignWorkCalendar;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.Users;
using EforTakip.Domain.WorkCalendars;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace EforTakip.Application.Tests.Users.Commands;

public class AssignWorkCalendarCommandHandlerTests : IAsyncDisposable
{
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly TestDbContext _db;

    public AssignWorkCalendarCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase($"assign-work-calendar-tests-{Guid.NewGuid()}")
            .Options;
        _db = new TestDbContext(options);

        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(callInfo => _db.SaveChangesAsync(callInfo.Arg<CancellationToken>()));
    }

    public async ValueTask DisposeAsync() => await _db.DisposeAsync();

    private AssignWorkCalendarCommandHandler CreateHandler() => new(_db, _unitOfWork);

    [Fact]
    public async Task Handle_WithValidUserAndCalendar_AssignsCalendar()
    {
        var directory = Domain.Directories.Directory.CreateInternal("Internal Users", 0);
        _db.Directories.Add(directory);
        var user = User.CreateInternal(directory.Id, "serkan", "Serkan", "Gültepe", "Serkan Gültepe", "a@b.com", "hash");
        _db.Users.Add(user);
        var workCalendar = WorkCalendar.Create("Standart");
        _db.WorkCalendars.Add(workCalendar);
        await _db.SaveChangesAsync(CancellationToken.None);

        await CreateHandler().Handle(
            new AssignWorkCalendarCommand(user.Id, workCalendar.Id), CancellationToken.None);

        _db.Users.Single().WorkCalendarId.Should().Be(workCalendar.Id);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithUnknownUser_ThrowsNotFoundException()
    {
        var act = async () => await CreateHandler().Handle(
            new AssignWorkCalendarCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
