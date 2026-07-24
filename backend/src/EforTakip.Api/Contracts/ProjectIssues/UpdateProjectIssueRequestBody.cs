using EforTakip.Domain.Projects;

namespace EforTakip.Api.Contracts.ProjectIssues;

public sealed record UpdateProjectIssueRequestBody(
    string Title,
    string? Description,
    ProjectIssuePriority Priority,
    Guid? OwnerUserId,
    DateOnly? DueDate,
    string? Resolution);
