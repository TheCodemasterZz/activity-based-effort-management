using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.Projects;
using MediatR;

namespace EforTakip.Application.Projects.Commands.UpdateProjectHealth;

public sealed class UpdateProjectHealthCommandHandler(IProjectRepository repository, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateProjectHealthCommand>
{
    public async Task Handle(UpdateProjectHealthCommand request, CancellationToken cancellationToken)
    {
        var project = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Project), request.Id);

        project.SetHealthStatus(request.HealthStatus);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
