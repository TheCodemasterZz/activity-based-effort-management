using EforTakip.Domain.Projects;
using MediatR;

namespace EforTakip.Application.Projects.Commands.CreateProject;

public sealed record CreateProjectCommand(
    string Name,
    string? Description,
    DateOnly? StartDate = null,
    DateOnly? EndDate = null,
    string? Sponsor = null,
    Guid? ProjectManagerUserId = null,
    ProjectPriority Priority = ProjectPriority.Medium,
    string? StrategicGoal = null) : IRequest<Guid>;
