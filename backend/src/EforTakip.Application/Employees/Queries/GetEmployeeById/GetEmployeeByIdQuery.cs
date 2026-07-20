using EforTakip.Application.Employees.Dtos;
using MediatR;

namespace EforTakip.Application.Employees.Queries.GetEmployeeById;

public sealed record GetEmployeeByIdQuery(Guid EmployeeId) : IRequest<EmployeeDto>;
