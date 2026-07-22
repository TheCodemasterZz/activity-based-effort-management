using EforTakip.Domain.Projects;
using MediatR;

namespace EforTakip.Application.Projects.Commands.CreateProjectIssue;

public sealed record CreateProjectIssueCommand(
    Guid ProjectId,
    string Title,
    string? Description,
    ProjectIssuePriority Priority,
    Guid? OwnerEmployeeId,
    DateOnly? DueDate) : IRequest<Guid>;
