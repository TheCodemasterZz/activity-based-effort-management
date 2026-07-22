using MediatR;

namespace EforTakip.Application.Projects.Commands.DeleteProjectIssue;

public sealed record DeleteProjectIssueCommand(Guid Id) : IRequest;
