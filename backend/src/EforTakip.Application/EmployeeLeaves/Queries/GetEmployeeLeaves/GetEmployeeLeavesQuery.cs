using EforTakip.Application.Common.Models;
using EforTakip.Application.EmployeeLeaves.Dtos;
using MediatR;

namespace EforTakip.Application.EmployeeLeaves.Queries.GetEmployeeLeaves;

public sealed class GetEmployeeLeavesQuery : PaginationParams, IRequest<PagedResult<EmployeeLeaveDto>>
{
    public Guid? EmployeeId { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
}
