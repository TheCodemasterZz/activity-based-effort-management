using MediatR;

namespace EforTakip.Application.Users.Commands.BulkAssignWorkCalendar;

public sealed record BulkAssignWorkCalendarCommand(
    IReadOnlyCollection<Guid> UserIds, Guid WorkCalendarId) : IRequest;
