using MediatR;

namespace EforTakip.Application.Projects.Commands.AssignUserToProject;

public sealed record AssignUserToProjectCommand(Guid ProjectId, Guid UserId) : IRequest;
