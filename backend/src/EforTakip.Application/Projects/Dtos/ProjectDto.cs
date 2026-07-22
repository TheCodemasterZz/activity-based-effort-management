namespace EforTakip.Application.Projects.Dtos;

public sealed class ProjectDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public string? Description { get; init; }
    public string Status { get; init; } = default!;
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public string HealthStatus { get; init; } = default!;
    public string? Sponsor { get; init; }
    public Guid? ProjectManagerEmployeeId { get; init; }
    public string Priority { get; init; } = default!;
    public string? StrategicGoal { get; init; }
}
