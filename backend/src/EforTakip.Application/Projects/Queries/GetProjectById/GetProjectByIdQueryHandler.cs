using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Projects.Dtos;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.Projects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.Projects.Queries.GetProjectById;

public sealed class GetProjectByIdQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetProjectByIdQuery, ProjectDetailDto>
{
    public async Task<ProjectDetailDto> Handle(GetProjectByIdQuery request, CancellationToken cancellationToken)
    {
        var project = await db.Projects
            .AsNoTracking()
            .Include(p => p.CustomerAssignments)
            .Include(p => p.UserAssignments)
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken)
            ?? throw new NotFoundException(nameof(Project), request.ProjectId);

        var customerIds = project.CustomerAssignments.Select(a => a.CustomerId).ToList();
        var userIds = project.UserAssignments.Select(a => a.UserId).ToList();

        var customers = await db.Customers
            .AsNoTracking()
            .Where(c => customerIds.Contains(c.Id))
            .Select(c => new CustomerSummaryDto { Id = c.Id, Name = c.Name })
            .ToListAsync(cancellationToken);

        var users = await db.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new UserSummaryDto { Id = u.Id, Name = u.DisplayName ?? u.Username })
            .ToListAsync(cancellationToken);

        return new ProjectDetailDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            Status = project.Status.ToString(),
            StartDate = project.StartDate,
            EndDate = project.EndDate,
            HealthStatus = project.HealthStatus.ToString(),
            Sponsor = project.Sponsor,
            ProjectManagerUserId = project.ProjectManagerUserId,
            Priority = project.Priority.ToString(),
            StrategicGoal = project.StrategicGoal,
            Customers = customers,
            Users = users
        };
    }
}
