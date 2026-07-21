namespace EforTakip.Api.Contracts.EmployeeLeaves;

public sealed record CreateEmployeeLeaveRequestBody(
    Guid EmployeeId,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsFullDay,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    string? Description);
