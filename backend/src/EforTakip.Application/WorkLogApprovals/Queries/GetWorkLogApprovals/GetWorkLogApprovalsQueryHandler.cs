using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Common.Models;
using EforTakip.Application.WorkLogApprovals.Dtos;
using EforTakip.Domain.WorkLogApprovals;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.WorkLogApprovals.Queries.GetWorkLogApprovals;

public sealed class GetWorkLogApprovalsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetWorkLogApprovalsQuery, PagedResult<WorkLogApprovalDto>>
{
    public async Task<PagedResult<WorkLogApprovalDto>> Handle(
        GetWorkLogApprovalsQuery request, CancellationToken cancellationToken)
    {
        IQueryable<WorkLogApproval> query = db.WorkLogApprovals.AsNoTracking()
            .Where(a => a.EntryType == request.EntryType);

        if (request.EmployeeId is { } employeeId)
            query = query.Where(a => a.EmployeeId == employeeId);

        query = query.OrderByDescending(a => a.PeriodStart);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ProjectToType<WorkLogApprovalDto>()
            .ToListAsync(cancellationToken);

        return new PagedResult<WorkLogApprovalDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
