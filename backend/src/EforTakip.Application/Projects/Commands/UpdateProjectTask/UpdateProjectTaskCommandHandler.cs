using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.Projects;
using MediatR;

namespace EforTakip.Application.Projects.Commands.UpdateProjectTask;

public sealed class UpdateProjectTaskCommandHandler(IRepository<ProjectTask> repository, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateProjectTaskCommand>
{
    public async Task Handle(UpdateProjectTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ProjectTask), request.Id);

        task.UpdatePlan(
            request.Name, request.StartDate, request.EndDate, request.EstimatedEffortHours, request.IsMilestone,
            request.ParentTaskId, request.DependsOnTaskId, request.AssignedEmployeeId);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
