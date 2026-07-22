namespace EforTakip.Application.Projects.Dtos;

public sealed class ProjectTaskDto
{
    public Guid Id { get; init; }
    public Guid ProjectId { get; init; }
    public string Name { get; init; } = default!;
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public decimal EstimatedEffortHours { get; init; }
    public string Status { get; init; } = default!;
    public bool IsMilestone { get; init; }
    public decimal BaselineEffortHours { get; init; }
    public DateOnly BaselineEndDate { get; init; }
}
