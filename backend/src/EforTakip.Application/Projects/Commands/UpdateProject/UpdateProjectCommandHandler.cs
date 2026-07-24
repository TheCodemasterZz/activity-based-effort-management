using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.Projects;
using MediatR;

namespace EforTakip.Application.Projects.Commands.UpdateProject;

public sealed class UpdateProjectCommandHandler(IProjectRepository repository, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateProjectCommand>
{
    public async Task Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Project), request.Id);

        project.Update(
            request.Name,
            request.Description,
            request.StartDate,
            request.EndDate,
            request.Sponsor,
            request.ProjectManagerUserId,
            request.Priority,
            request.StrategicGoal);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
