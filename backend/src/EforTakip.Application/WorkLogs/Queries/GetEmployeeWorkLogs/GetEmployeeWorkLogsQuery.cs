using EforTakip.Application.Common.Models;
using EforTakip.Application.WorkLogs.Dtos;
using EforTakip.Domain.WorkLogs;
using MediatR;

namespace EforTakip.Application.WorkLogs.Queries.GetEmployeeWorkLogs;

public sealed class GetEmployeeWorkLogsQuery : PaginationParams, IRequest<PagedResult<EmployeeWorkLogDto>>
{
    public Guid? EmployeeId { get; set; }
    public Guid? ProjectId { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    public WorkLogEntryType EntryType { get; set; } = WorkLogEntryType.Actual;
}
