using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.Projects;
using MediatR;

namespace EforTakip.Application.Projects.Commands.UpdateProjectRiskStatus;

public sealed class UpdateProjectRiskStatusCommandHandler(IRepository<ProjectRisk> repository, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateProjectRiskStatusCommand>
{
    public async Task Handle(UpdateProjectRiskStatusCommand request, CancellationToken cancellationToken)
    {
        var risk = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ProjectRisk), request.Id);

        risk.SetStatus(request.Status);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
