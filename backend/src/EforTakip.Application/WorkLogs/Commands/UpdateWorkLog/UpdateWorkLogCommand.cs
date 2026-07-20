using MediatR;

namespace EforTakip.Application.WorkLogs.Commands.UpdateWorkLog;

public sealed record UpdateWorkLogCommand(
    Guid Id,
    Guid EmployeeId,
    Guid ProjectId,
    Guid CustomerId,
    Guid ActivityL1Id,
    Guid ActivityL2Id,
    DateOnly WorkDate,
    decimal Hours,
    string Description) : IRequest;
