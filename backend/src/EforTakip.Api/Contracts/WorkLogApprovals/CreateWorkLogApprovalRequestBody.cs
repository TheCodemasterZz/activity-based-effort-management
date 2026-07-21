using EforTakip.Domain.WorkLogApprovals;
using EforTakip.Domain.WorkLogs;

namespace EforTakip.Api.Contracts.WorkLogApprovals;

public sealed record CreateWorkLogApprovalRequestBody(
    Guid EmployeeId,
    ApprovalPeriodType PeriodType,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    string Description,
    WorkLogEntryType EntryType = WorkLogEntryType.Actual);
