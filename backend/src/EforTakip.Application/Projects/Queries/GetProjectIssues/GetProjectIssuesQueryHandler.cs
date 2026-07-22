using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Common.Models;
using EforTakip.Application.Projects.Dtos;
using EforTakip.Domain.Projects;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.Projects.Queries.GetProjectIssues;

public sealed class GetProjectIssuesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetProjectIssuesQuery, PagedResult<ProjectIssueDto>>
{
    public async Task<PagedResult<ProjectIssueDto>> Handle(GetProjectIssuesQuery request, CancellationToken cancellationToken)
    {
        IQueryable<ProjectIssue> query = db.ProjectIssues.AsNoTracking();

        if (request.ProjectId is { } projectId)
            query = query.Where(i => i.ProjectId == projectId);

        // Süresi en yakın (veya geçmiş) olanlar önce — tarihsiz olanlar en sona düşer.
        query = query.OrderBy(i => i.DueDate ?? DateOnly.MaxValue);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ProjectToType<ProjectIssueDto>()
            .ToListAsync(cancellationToken);

        return new PagedResult<ProjectIssueDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
