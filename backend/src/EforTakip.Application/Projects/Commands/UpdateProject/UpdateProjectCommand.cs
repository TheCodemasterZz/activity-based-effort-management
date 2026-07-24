using EforTakip.Domain.Projects;
using MediatR;

namespace EforTakip.Application.Projects.Commands.UpdateProject;

public sealed record UpdateProjectCommand(
    Guid Id,
    string Name,
    string? Description,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string? Sponsor,
    Guid? ProjectManagerUserId,
    ProjectPriority Priority,
    string? StrategicGoal) : IRequest;
