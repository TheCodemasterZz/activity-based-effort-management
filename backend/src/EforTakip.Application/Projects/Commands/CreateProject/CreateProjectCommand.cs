using MediatR;

namespace EforTakip.Application.Projects.Commands.CreateProject;

public sealed record CreateProjectCommand(string Name, string? Description) : IRequest<Guid>;
