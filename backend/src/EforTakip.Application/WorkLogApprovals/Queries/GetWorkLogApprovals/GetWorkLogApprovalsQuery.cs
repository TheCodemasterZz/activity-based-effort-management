using EforTakip.Application.Common.Models;
using EforTakip.Application.WorkLogApprovals.Dtos;
using EforTakip.Domain.WorkLogs;
using MediatR;

namespace EforTakip.Application.WorkLogApprovals.Queries.GetWorkLogApprovals;

public sealed class GetWorkLogApprovalsQuery : PaginationParams, IRequest<PagedResult<WorkLogApprovalDto>>
{
    public Guid? EmployeeId { get; set; }
    public WorkLogEntryType EntryType { get; set; } = WorkLogEntryType.Actual;
}
