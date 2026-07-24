using MediatR;

namespace EforTakip.Application.Leaves.Commands.CreateLeave;

public sealed record CreateLeaveCommand(
    Guid UserId,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsFullDay,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    string? Description) : IRequest<Guid>;
