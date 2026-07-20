namespace EforTakip.Api.Contracts.WorkLogs;

public sealed record UpdateWorkLogRequestBody(
    Guid EmployeeId,
    Guid ProjectId,
    Guid CustomerId,
    Guid ActivityL1Id,
    Guid ActivityL2Id,
    DateOnly WorkDate,
    decimal Hours,
    string Description);
