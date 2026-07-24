using MediatR;

namespace EforTakip.Application.Users.Commands.AssignWorkCalendar;

public sealed record AssignWorkCalendarCommand(Guid UserId, Guid WorkCalendarId) : IRequest;
