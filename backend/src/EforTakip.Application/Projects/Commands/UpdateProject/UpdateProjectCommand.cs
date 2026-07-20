using MediatR;

namespace EforTakip.Application.Projects.Commands.UpdateProject;

public sealed record UpdateProjectCommand(Guid Id, string Name, string? Description) : IRequest;
