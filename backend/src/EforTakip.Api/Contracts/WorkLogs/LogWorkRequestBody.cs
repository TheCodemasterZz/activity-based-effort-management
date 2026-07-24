using EforTakip.Domain.WorkLogs;

namespace EforTakip.Api.Contracts.WorkLogs;

public sealed record LogWorkRequestBody(
    Guid UserId,
    Guid ProjectId,
    Guid ActivityL1Id,
    Guid ActivityL2Id,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal Hours,
    string Description,
    WorkLogEntryType EntryType = WorkLogEntryType.Actual);
