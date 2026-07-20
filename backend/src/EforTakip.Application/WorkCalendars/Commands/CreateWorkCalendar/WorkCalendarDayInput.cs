namespace EforTakip.Application.WorkCalendars.Commands.CreateWorkCalendar;

public sealed record WorkCalendarDayInput(
    DayOfWeek DayOfWeek,
    bool IsWorkingDay,
    TimeOnly? StartTime,
    TimeOnly? EndTime);
