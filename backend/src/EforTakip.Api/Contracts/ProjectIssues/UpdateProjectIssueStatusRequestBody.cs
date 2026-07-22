using EforTakip.Domain.Projects;

namespace EforTakip.Api.Contracts.ProjectIssues;

public sealed record UpdateProjectIssueStatusRequestBody(ProjectIssueStatus Status);
