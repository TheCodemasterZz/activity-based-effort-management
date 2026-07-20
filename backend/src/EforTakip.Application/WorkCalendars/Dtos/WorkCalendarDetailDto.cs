namespace EforTakip.Application.WorkCalendars.Dtos;

public sealed class WorkCalendarDetailDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public IReadOnlyCollection<WorkCalendarDayDto> Days { get; init; } = [];
}
