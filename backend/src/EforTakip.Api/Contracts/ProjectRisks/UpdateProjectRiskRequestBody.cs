namespace EforTakip.Api.Contracts.ProjectRisks;

public sealed record UpdateProjectRiskRequestBody(
    string Title,
    string? Description,
    int Probability,
    int Impact,
    string? MitigationPlan,
    Guid? OwnerEmployeeId,
    DateOnly IdentifiedDate);
