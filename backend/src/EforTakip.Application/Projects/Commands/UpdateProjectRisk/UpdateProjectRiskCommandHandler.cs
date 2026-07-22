using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.Projects;
using MediatR;

namespace EforTakip.Application.Projects.Commands.UpdateProjectRisk;

public sealed class UpdateProjectRiskCommandHandler(IRepository<ProjectRisk> repository, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateProjectRiskCommand>
{
    public async Task Handle(UpdateProjectRiskCommand request, CancellationToken cancellationToken)
    {
        var risk = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ProjectRisk), request.Id);

        risk.Update(
            request.Title, request.Description, request.Probability, request.Impact,
            request.MitigationPlan, request.OwnerEmployeeId, request.IdentifiedDate);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
