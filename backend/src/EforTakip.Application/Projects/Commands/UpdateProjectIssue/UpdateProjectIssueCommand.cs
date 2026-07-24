using EforTakip.Domain.Projects;
using MediatR;

namespace EforTakip.Application.Projects.Commands.UpdateProjectIssue;

public sealed record UpdateProjectIssueCommand(
    Guid Id,
    string Title,
    string? Description,
    ProjectIssuePriority Priority,
    Guid? OwnerUserId,
    DateOnly? DueDate,
    string? Resolution) : IRequest;
