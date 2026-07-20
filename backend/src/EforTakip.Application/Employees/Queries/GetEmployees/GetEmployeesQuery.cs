using EforTakip.Application.Common.Models;
using EforTakip.Application.Employees.Dtos;
using MediatR;

namespace EforTakip.Application.Employees.Queries.GetEmployees;

public sealed class GetEmployeesQuery : PaginationParams, IRequest<PagedResult<EmployeeDto>>
{
    public string? NameFilter { get; set; }
}
