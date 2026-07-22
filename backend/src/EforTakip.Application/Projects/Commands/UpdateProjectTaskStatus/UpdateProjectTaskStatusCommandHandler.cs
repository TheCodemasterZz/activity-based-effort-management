using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.Projects;
using MediatR;

namespace EforTakip.Application.Projects.Commands.UpdateProjectTaskStatus;

public sealed class UpdateProjectTaskStatusCommandHandler(IRepository<ProjectTask> repository, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateProjectTaskStatusCommand>
{
    public async Task Handle(UpdateProjectTaskStatusCommand request, CancellationToken cancellationToken)
    {
        var task = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ProjectTask), request.Id);

        task.SetStatus(request.Status);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
