using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Tests.Directories.Commands;
using EforTakip.Application.Users.Commands.BulkAssignWorkCalendar;
using EforTakip.Domain.Users;
using EforTakip.Domain.WorkCalendars;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace EforTakip.Application.Tests.Users.Commands;

public class BulkAssignWorkCalendarCommandHandlerTests : IAsyncDisposable
{
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly TestDbContext _db;

    public BulkAssignWorkCalendarCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase($"bulk-assign-work-calendar-tests-{Guid.NewGuid()}")
            .Options;
        _db = new TestDbContext(options);

        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(callInfo => _db.SaveChangesAsync(callInfo.Arg<CancellationToken>()));
    }

    public async ValueTask DisposeAsync() => await _db.DisposeAsync();

    private BulkAssignWorkCalendarCommandHandler CreateHandler() => new(_db, _unitOfWork);

    [Fact]
    public async Task Handle_WithMultipleUsers_AssignsCalendarToAll()
    {
        var directory = Domain.Directories.Directory.CreateInternal("Internal Users", 0);
        _db.Directories.Add(directory);
        var user1 = User.CreateInternal(directory.Id, "serkan", null, null, "Serkan", null, "hash");
        var user2 = User.CreateInternal(directory.Id, "ayse", null, null, "Ayşe", null, "hash");
        _db.Users.AddRange(user1, user2);
        var workCalendar = WorkCalendar.Create("Standart");
        _db.WorkCalendars.Add(workCalendar);
        await _db.SaveChangesAsync(CancellationToken.None);

        await CreateHandler().Handle(
            new BulkAssignWorkCalendarCommand([user1.Id, user2.Id], workCalendar.Id), CancellationToken.None);

        _db.Users.Count(u => u.WorkCalendarId == workCalendar.Id).Should().Be(2);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
