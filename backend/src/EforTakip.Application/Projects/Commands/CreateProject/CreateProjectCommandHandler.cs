using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Projects;
using MediatR;

namespace EforTakip.Application.Projects.Commands.CreateProject;

public sealed class CreateProjectCommandHandler(IProjectRepository repository, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateProjectCommand, Guid>
{
    public async Task<Guid> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        var project = Project.Create(
            request.Name,
            request.Description,
            request.StartDate,
            request.EndDate,
            request.Sponsor,
            request.ProjectManagerUserId,
            request.Priority,
            request.StrategicGoal);

        await repository.AddAsync(project, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return project.Id;
    }
}
