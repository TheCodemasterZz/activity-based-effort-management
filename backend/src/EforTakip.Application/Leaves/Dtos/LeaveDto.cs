namespace EforTakip.Application.Leaves.Dtos;

public sealed class LeaveDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public bool IsFullDay { get; init; }
    public TimeOnly? StartTime { get; init; }
    public TimeOnly? EndTime { get; init; }
    public string? Description { get; init; }
}
