namespace EforTakip.Api.Contracts.Leaves;

public sealed record CreateLeaveRequestBody(
    Guid UserId,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsFullDay,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    string? Description);
