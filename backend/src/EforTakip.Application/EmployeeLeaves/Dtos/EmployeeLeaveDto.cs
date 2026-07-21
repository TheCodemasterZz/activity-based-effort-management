namespace EforTakip.Application.EmployeeLeaves.Dtos;

public sealed class EmployeeLeaveDto
{
    public Guid Id { get; init; }
    public Guid EmployeeId { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public bool IsFullDay { get; init; }
    public TimeOnly? StartTime { get; init; }
    public TimeOnly? EndTime { get; init; }
    public string? Description { get; init; }
}
