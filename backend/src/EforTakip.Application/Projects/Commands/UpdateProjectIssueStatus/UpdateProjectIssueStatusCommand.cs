using EforTakip.Domain.Projects;
using MediatR;

namespace EforTakip.Application.Projects.Commands.UpdateProjectIssueStatus;

public sealed record UpdateProjectIssueStatusCommand(Guid Id, ProjectIssueStatus Status) : IRequest;
