using MediatR;

namespace EforTakip.Application.Projects.Commands.DeleteProject;

public sealed record DeleteProjectCommand(Guid Id) : IRequest;
