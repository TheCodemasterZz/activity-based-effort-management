namespace EforTakip.Application.WorkCalendars.Dtos;

public sealed class WorkCalendarDayDto
{
    public DayOfWeek DayOfWeek { get; init; }
    public bool IsWorkingDay { get; init; }
    public TimeOnly? StartTime { get; init; }
    public TimeOnly? EndTime { get; init; }
}
