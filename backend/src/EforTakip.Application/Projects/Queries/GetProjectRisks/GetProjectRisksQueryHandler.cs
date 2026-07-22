using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Common.Models;
using EforTakip.Application.Projects.Dtos;
using EforTakip.Domain.Projects;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.Projects.Queries.GetProjectRisks;

public sealed class GetProjectRisksQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetProjectRisksQuery, PagedResult<ProjectRiskDto>>
{
    public async Task<PagedResult<ProjectRiskDto>> Handle(GetProjectRisksQuery request, CancellationToken cancellationToken)
    {
        IQueryable<ProjectRisk> query = db.ProjectRisks.AsNoTracking();

        if (request.ProjectId is { } projectId)
            query = query.Where(r => r.ProjectId == projectId);

        query = query.OrderByDescending(r => r.Probability * r.Impact);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ProjectToType<ProjectRiskDto>()
            .ToListAsync(cancellationToken);

        return new PagedResult<ProjectRiskDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
