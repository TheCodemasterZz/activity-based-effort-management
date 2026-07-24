using MediatR;

namespace EforTakip.Application.Projects.Commands.CreateProjectTask;

public sealed record CreateProjectTaskCommand(
    Guid ProjectId,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal EstimatedEffortHours,
    bool IsMilestone,
    Guid? ParentTaskId = null,
    Guid? DependsOnTaskId = null,
    Guid? AssignedUserId = null) : IRequest<Guid>;
