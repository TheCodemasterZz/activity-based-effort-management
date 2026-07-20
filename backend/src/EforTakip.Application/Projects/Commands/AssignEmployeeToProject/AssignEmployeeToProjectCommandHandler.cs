using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Employees;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.Projects;
using MediatR;

namespace EforTakip.Application.Projects.Commands.AssignEmployeeToProject;

public sealed class AssignEmployeeToProjectCommandHandler(
    IProjectRepository projectRepository,
    IRepository<Employee> employeeRepository,
    IApplicationDbContext db,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AssignEmployeeToProjectCommand>
{
    public async Task Handle(AssignEmployeeToProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdAsync(request.ProjectId, cancellationToken)
            ?? throw new NotFoundException(nameof(Project), request.ProjectId);

        _ = await employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken)
            ?? throw new NotFoundException(nameof(Employee), request.EmployeeId);

        var assignment = project.AssignEmployee(request.EmployeeId);

        db.ProjectEmployeeAssignments.Add(assignment);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
