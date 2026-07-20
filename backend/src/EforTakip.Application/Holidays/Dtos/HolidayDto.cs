namespace EforTakip.Application.Holidays.Dtos;

public sealed class HolidayDto
{
    public Guid Id { get; init; }
    public DateOnly Date { get; init; }
    public string Name { get; init; } = default!;
}
