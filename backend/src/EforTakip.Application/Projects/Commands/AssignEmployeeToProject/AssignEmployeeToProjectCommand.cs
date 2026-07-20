using MediatR;

namespace EforTakip.Application.Projects.Commands.AssignEmployeeToProject;

public sealed record AssignEmployeeToProjectCommand(Guid ProjectId, Guid EmployeeId) : IRequest;
