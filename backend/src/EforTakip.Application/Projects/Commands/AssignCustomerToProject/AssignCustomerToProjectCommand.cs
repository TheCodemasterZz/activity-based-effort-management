using MediatR;

namespace EforTakip.Application.Projects.Commands.AssignCustomerToProject;

public sealed record AssignCustomerToProjectCommand(Guid ProjectId, Guid CustomerId) : IRequest;
