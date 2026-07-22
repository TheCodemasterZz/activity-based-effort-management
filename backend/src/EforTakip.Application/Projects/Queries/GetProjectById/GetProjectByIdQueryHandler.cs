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
            .Include(p => p.EmployeeAssignments)
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken)
            ?? throw new NotFoundException(nameof(Project), request.ProjectId);

        var customerIds = project.CustomerAssignments.Select(a => a.CustomerId).ToList();
        var employeeIds = project.EmployeeAssignments.Select(a => a.EmployeeId).ToList();

        var customers = await db.Customers
            .AsNoTracking()
            .Where(c => customerIds.Contains(c.Id))
            .Select(c => new CustomerSummaryDto { Id = c.Id, Name = c.Name })
            .ToListAsync(cancellationToken);

        var employees = await db.Employees
            .AsNoTracking()
            .Where(e => employeeIds.Contains(e.Id))
            .Select(e => new EmployeeSummaryDto { Id = e.Id, Name = e.Name })
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
            ProjectManagerEmployeeId = project.ProjectManagerEmployeeId,
            Priority = project.Priority.ToString(),
            StrategicGoal = project.StrategicGoal,
            Customers = customers,
            Employees = employees
        };
    }
}
