using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.WorkCalendars;
using MediatR;

namespace EforTakip.Application.WorkCalendars.Commands.CreateWorkCalendar;

public sealed class CreateWorkCalendarCommandHandler(IWorkCalendarRepository repository, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateWorkCalendarCommand, Guid>
{
    public async Task<Guid> Handle(CreateWorkCalendarCommand request, CancellationToken cancellationToken)
    {
        var calendar = WorkCalendar.Create(request.Name);

        foreach (var day in request.Days)
            calendar.SetDay(day.DayOfWeek, day.IsWorkingDay, day.StartTime, day.EndTime);

        await repository.AddAsync(calendar, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return calendar.Id;
    }
}
