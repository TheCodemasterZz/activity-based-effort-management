namespace EforTakip.Application.Projects.Dtos;

public sealed class ProjectDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public string? Description { get; init; }
    public string Status { get; init; } = default!;
}
