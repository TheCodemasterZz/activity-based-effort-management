using MediatR;

namespace EforTakip.Application.WorkLogs.Commands.LogWork;

public sealed record LogWorkCommand(
    Guid EmployeeId,
    Guid ProjectId,
    Guid CustomerId,
    Guid ActivityL1Id,
    Guid ActivityL2Id,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal Hours,
    string Description) : IRequest<IReadOnlyCollection<Guid>>;
