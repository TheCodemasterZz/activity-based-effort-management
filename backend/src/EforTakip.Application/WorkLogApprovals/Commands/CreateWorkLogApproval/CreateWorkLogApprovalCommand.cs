using EforTakip.Domain.WorkLogApprovals;
using EforTakip.Domain.WorkLogs;
using MediatR;

namespace EforTakip.Application.WorkLogApprovals.Commands.CreateWorkLogApproval;

public sealed record CreateWorkLogApprovalCommand(
    Guid UserId,
    ApprovalPeriodType PeriodType,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    string Description,
    WorkLogEntryType EntryType = WorkLogEntryType.Actual) : IRequest<Guid>;
