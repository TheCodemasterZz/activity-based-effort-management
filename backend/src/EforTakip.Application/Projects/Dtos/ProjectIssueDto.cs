namespace EforTakip.Application.Projects.Dtos;

public sealed class ProjectIssueDto
{
    public Guid Id { get; init; }
    public Guid ProjectId { get; init; }
    public string Title { get; init; } = default!;
    public string? Description { get; init; }
    public string Priority { get; init; } = default!;
    public string Status { get; init; } = default!;
    public Guid? OwnerUserId { get; init; }
    public DateOnly? DueDate { get; init; }
    public string? Resolution { get; init; }
}
