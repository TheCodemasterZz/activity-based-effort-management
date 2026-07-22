namespace EforTakip.Api.Contracts.ProjectTasks;

public sealed record UpdateProjectTaskRequestBody(
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal EstimatedEffortHours,
    bool IsMilestone);
