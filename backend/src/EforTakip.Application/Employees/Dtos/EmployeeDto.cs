namespace EforTakip.Application.Employees.Dtos;

public sealed class EmployeeDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public string? Email { get; init; }
    public Guid WorkCalendarId { get; init; }
}
