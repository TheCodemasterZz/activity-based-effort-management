using EforTakip.Domain.WorkLogApprovals;
using MediatR;

namespace EforTakip.Application.WorkLogApprovals.Commands.CreateWorkLogApproval;

public sealed record CreateWorkLogApprovalCommand(
    Guid EmployeeId,
    ApprovalPeriodType PeriodType,
    DateOnly PeriodStart,
    DateOnly PeriodEnd) : IRequest<Guid>;
