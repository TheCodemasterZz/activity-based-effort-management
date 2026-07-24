namespace EforTakip.Api.Contracts.WorkLogs;

public sealed record UpdateWorkLogRequestBody(
    Guid UserId,
    Guid ProjectId,
    Guid ActivityL1Id,
    Guid ActivityL2Id,
    DateOnly WorkDate,
    decimal Hours,
    string Description);
