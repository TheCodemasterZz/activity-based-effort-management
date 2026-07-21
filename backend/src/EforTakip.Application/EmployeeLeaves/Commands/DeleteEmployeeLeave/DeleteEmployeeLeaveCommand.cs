using MediatR;

namespace EforTakip.Application.EmployeeLeaves.Commands.DeleteEmployeeLeave;

public sealed record DeleteEmployeeLeaveCommand(Guid Id) : IRequest;
