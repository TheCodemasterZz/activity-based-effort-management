using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.Projects;
using MediatR;

namespace EforTakip.Application.Projects.Commands.CreateProjectTask;

public sealed class CreateProjectTaskCommandHandler(
    IProjectRepository projectRepository, IRepository<ProjectTask> repository, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateProjectTaskCommand, Guid>
{
    public async Task<Guid> Handle(CreateProjectTaskCommand request, CancellationToken cancellationToken)
    {
        _ = await projectRepository.GetByIdAsync(request.ProjectId, cancellationToken)
            ?? throw new NotFoundException(nameof(Project), request.ProjectId);

        var task = ProjectTask.Create(
            request.ProjectId, request.Name, request.StartDate, request.EndDate,
            request.EstimatedEffortHours, request.IsMilestone,
            request.ParentTaskId, request.DependsOnTaskId, request.AssignedUserId);

        await repository.AddAsync(task, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return task.Id;
    }
}
