using EforTakip.Domain.Projects;

namespace EforTakip.Api.Contracts.ProjectTasks;

public sealed record UpdateProjectTaskStatusRequestBody(ProjectTaskStatus Status);
