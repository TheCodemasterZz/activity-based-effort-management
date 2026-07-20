using EforTakip.Application.Common.Models;
using EforTakip.Application.WorkLogApprovals.Dtos;
using MediatR;

namespace EforTakip.Application.WorkLogApprovals.Queries.GetWorkLogApprovals;

public sealed class GetWorkLogApprovalsQuery : PaginationParams, IRequest<PagedResult<WorkLogApprovalDto>>
{
    public Guid? EmployeeId { get; set; }
}
