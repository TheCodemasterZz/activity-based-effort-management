using EforTakip.Domain.Projects;
using MediatR;

namespace EforTakip.Application.Projects.Commands.UpdateProjectTaskStatus;

public sealed record UpdateProjectTaskStatusCommand(Guid Id, ProjectTaskStatus Status) : IRequest;
