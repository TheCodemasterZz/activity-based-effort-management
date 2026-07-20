namespace EforTakip.Api.Contracts.WorkLogs;

public sealed record LogWorkRequestBody(
    Guid EmployeeId,
    Guid ProjectId,
    Guid CustomerId,
    Guid ActivityL1Id,
    Guid ActivityL2Id,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal Hours,
    string Description);
