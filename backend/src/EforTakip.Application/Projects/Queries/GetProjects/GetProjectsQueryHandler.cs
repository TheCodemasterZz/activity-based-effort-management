using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Common.Models;
using EforTakip.Application.Projects.Dtos;
using EforTakip.Domain.Projects;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.Projects.Queries.GetProjects;

public sealed class GetProjectsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetProjectsQuery, PagedResult<ProjectDto>>
{
    public async Task<PagedResult<ProjectDto>> Handle(GetProjectsQuery request, CancellationToken cancellationToken)
    {
        IQueryable<Project> query = db.Projects.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.NameFilter))
        {
            var nameFilter = request.NameFilter.ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(nameFilter));
        }

        if (request.EmployeeId is { } employeeId)
        {
            var assignedProjectIds = db.ProjectEmployeeAssignments
                .Where(a => a.EmployeeId == employeeId)
                .Select(a => a.ProjectId);
            query = query.Where(p => assignedProjectIds.Contains(p.Id));
        }

        query = request.SortBy switch
        {
            "name" => request.Descending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
            _ => query.OrderByDescending(p => p.Id)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ProjectToType<ProjectDto>()
            .ToListAsync(cancellationToken);

        return new PagedResult<ProjectDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
