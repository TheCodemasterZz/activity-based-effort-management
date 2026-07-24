namespace EforTakip.Application.Projects.Dtos;

public sealed class ProjectRiskDto
{
    public Guid Id { get; init; }
    public Guid ProjectId { get; init; }
    public string Title { get; init; } = default!;
    public string? Description { get; init; }
    public int Probability { get; init; }
    public int Impact { get; init; }
    public string Status { get; init; } = default!;
    public string? MitigationPlan { get; init; }
    public Guid? OwnerUserId { get; init; }
    public DateOnly IdentifiedDate { get; init; }
}
