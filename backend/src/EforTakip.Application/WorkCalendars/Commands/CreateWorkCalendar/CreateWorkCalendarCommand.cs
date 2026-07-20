using MediatR;

namespace EforTakip.Application.WorkCalendars.Commands.CreateWorkCalendar;

public sealed record CreateWorkCalendarCommand(string Name, IReadOnlyList<WorkCalendarDayInput> Days) : IRequest<Guid>;
