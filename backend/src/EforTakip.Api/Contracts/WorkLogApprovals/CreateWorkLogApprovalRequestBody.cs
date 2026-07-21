using EforTakip.Domain.WorkLogApprovals;

namespace EforTakip.Api.Contracts.WorkLogApprovals;

public sealed record CreateWorkLogApprovalRequestBody(
    Guid EmployeeId,
    ApprovalPeriodType PeriodType,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    string Description);
