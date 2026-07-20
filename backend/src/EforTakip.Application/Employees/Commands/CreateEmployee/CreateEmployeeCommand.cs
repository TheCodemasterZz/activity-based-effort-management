using MediatR;

namespace EforTakip.Application.Employees.Commands.CreateEmployee;

public sealed record CreateEmployeeCommand(string Name, string? Email, Guid WorkCalendarId) : IRequest<Guid>;
