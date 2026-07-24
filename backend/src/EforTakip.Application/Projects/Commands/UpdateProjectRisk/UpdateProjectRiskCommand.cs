using MediatR;

namespace EforTakip.Application.Projects.Commands.UpdateProjectRisk;

public sealed record UpdateProjectRiskCommand(
    Guid Id,
    string Title,
    string? Description,
    int Probability,
    int Impact,
    string? MitigationPlan,
    Guid? OwnerUserId,
    DateOnly IdentifiedDate) : IRequest;
