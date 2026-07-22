using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Common.Models;
using EforTakip.Application.Projects.Dtos;
using EforTakip.Domain.Projects;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.Projects.Queries.GetProjectTasks;

public sealed class GetProjectTasksQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetProjectTasksQuery, PagedResult<ProjectTaskDto>>
{
    public async Task<PagedResult<ProjectTaskDto>> Handle(GetProjectTasksQuery request, CancellationToken cancellationToken)
    {
        IQueryable<ProjectTask> query = db.ProjectTasks.AsNoTracking();

        if (request.ProjectId is { } projectId)
            query = query.Where(t => t.ProjectId == projectId);

        query = query.OrderBy(t => t.StartDate);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ProjectToType<ProjectTaskDto>()
            .ToListAsync(cancellationToken);

        return new PagedResult<ProjectTaskDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
