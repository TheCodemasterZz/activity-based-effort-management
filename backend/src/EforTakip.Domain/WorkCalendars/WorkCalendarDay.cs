using EforTakip.Domain.Common;

namespace EforTakip.Domain.WorkCalendars;

public sealed class WorkCalendarDay : Entity
{
    public Guid WorkCalendarId { get; private set; }
    public DayOfWeek DayOfWeek { get; private set; }
    public bool IsWorkingDay { get; private set; }
    public TimeOnly? StartTime { get; private set; }
    public TimeOnly? EndTime { get; private set; }

    private WorkCalendarDay()
    {
        // EF Core
    }

    internal static WorkCalendarDay Create(
        Guid workCalendarId, DayOfWeek dayOfWeek, bool isWorkingDay, TimeOnly? startTime, TimeOnly? endTime)
        => new()
        {
            WorkCalendarId = workCalendarId,
            DayOfWeek = dayOfWeek,
            IsWorkingDay = isWorkingDay,
            StartTime = isWorkingDay ? startTime : null,
            EndTime = isWorkingDay ? endTime : null
        };

    internal void Set(bool isWorkingDay, TimeOnly? startTime, TimeOnly? endTime)
    {
        IsWorkingDay = isWorkingDay;
        StartTime = isWorkingDay ? startTime : null;
        EndTime = isWorkingDay ? endTime : null;
    }
}
