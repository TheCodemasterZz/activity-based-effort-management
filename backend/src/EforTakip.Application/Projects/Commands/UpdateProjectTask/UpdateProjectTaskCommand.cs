using MediatR;

namespace EforTakip.Application.Projects.Commands.UpdateProjectTask;

public sealed record UpdateProjectTaskCommand(
    Guid Id,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal EstimatedEffortHours,
    bool IsMilestone) : IRequest;
