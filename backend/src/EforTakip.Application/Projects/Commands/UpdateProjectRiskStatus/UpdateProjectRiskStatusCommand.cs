using EforTakip.Domain.Projects;
using MediatR;

namespace EforTakip.Application.Projects.Commands.UpdateProjectRiskStatus;

public sealed record UpdateProjectRiskStatusCommand(Guid Id, ProjectRiskStatus Status) : IRequest;
