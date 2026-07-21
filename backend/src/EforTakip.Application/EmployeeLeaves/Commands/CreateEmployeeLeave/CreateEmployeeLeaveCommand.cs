using MediatR;

namespace EforTakip.Application.EmployeeLeaves.Commands.CreateEmployeeLeave;

public sealed record CreateEmployeeLeaveCommand(
    Guid EmployeeId,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsFullDay,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    string? Description) : IRequest<Guid>;
