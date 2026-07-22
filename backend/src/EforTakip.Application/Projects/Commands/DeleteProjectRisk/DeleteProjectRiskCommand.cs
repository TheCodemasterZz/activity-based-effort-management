using MediatR;

namespace EforTakip.Application.Projects.Commands.DeleteProjectRisk;

public sealed record DeleteProjectRiskCommand(Guid Id) : IRequest;
