using MediatR;

namespace EforTakip.Application.Projects.Commands.CreateProject;

public sealed record CreateProjectCommand(
    string Name,
    string? Description,
    DateOnly? StartDate = null,
    DateOnly? EndDate = null) : IRequest<Guid>;
