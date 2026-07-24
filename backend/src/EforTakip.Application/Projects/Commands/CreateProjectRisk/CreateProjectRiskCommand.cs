using MediatR;

namespace EforTakip.Application.Projects.Commands.CreateProjectRisk;

public sealed record CreateProjectRiskCommand(
    Guid ProjectId,
    string Title,
    string? Description,
    int Probability,
    int Impact,
    string? MitigationPlan,
    Guid? OwnerUserId,
    DateOnly IdentifiedDate) : IRequest<Guid>;
