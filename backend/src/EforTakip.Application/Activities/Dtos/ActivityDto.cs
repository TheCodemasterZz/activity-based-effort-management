namespace EforTakip.Application.Activities.Dtos;

public sealed class ActivityDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public string? Description { get; init; }
    public Guid? ParentActivityId { get; init; }
}
