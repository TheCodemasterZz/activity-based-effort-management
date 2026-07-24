using EforTakip.Domain.Projects;

namespace EforTakip.Api.Contracts.Projects;

public sealed record UpdateProjectRequestBody(
    string Name,
    string? Description,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string? Sponsor,
    Guid? ProjectManagerUserId,
    ProjectPriority Priority,
    string? StrategicGoal);
