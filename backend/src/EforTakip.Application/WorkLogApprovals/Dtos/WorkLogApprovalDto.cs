using EforTakip.Domain.WorkLogs;

namespace EforTakip.Application.WorkLogApprovals.Dtos;

public sealed class WorkLogApprovalDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public DateOnly PeriodStart { get; init; }
    public DateOnly PeriodEnd { get; init; }
    public string? Description { get; init; }
    public WorkLogEntryType EntryType { get; init; }
}
