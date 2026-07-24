using EforTakip.Domain.WorkLogs;

namespace EforTakip.Application.WorkLogs.Dtos;

public sealed class WorkLogDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public Guid ProjectId { get; init; }
    public Guid ActivityL1Id { get; init; }
    public Guid ActivityL2Id { get; init; }
    public DateOnly WorkDate { get; init; }
    public decimal Hours { get; init; }
    public string Description { get; init; } = default!;
    public bool IsApproved { get; init; }
    public WorkLogEntryType EntryType { get; init; }
}
