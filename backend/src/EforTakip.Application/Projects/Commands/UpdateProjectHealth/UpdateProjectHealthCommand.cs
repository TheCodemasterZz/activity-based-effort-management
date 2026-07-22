using EforTakip.Domain.Projects;
using MediatR;

namespace EforTakip.Application.Projects.Commands.UpdateProjectHealth;

public sealed record UpdateProjectHealthCommand(Guid Id, ProjectHealthStatus HealthStatus) : IRequest;
