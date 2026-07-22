using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.Projects;
using MediatR;

namespace EforTakip.Application.Projects.Commands.CreateProjectRisk;

public sealed class CreateProjectRiskCommandHandler(
    IProjectRepository projectRepository, IRepository<ProjectRisk> repository, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateProjectRiskCommand, Guid>
{
    public async Task<Guid> Handle(CreateProjectRiskCommand request, CancellationToken cancellationToken)
    {
        _ = await projectRepository.GetByIdAsync(request.ProjectId, cancellationToken)
            ?? throw new NotFoundException(nameof(Project), request.ProjectId);

        var risk = ProjectRisk.Create(
            request.ProjectId, request.Title, request.Description, request.Probability, request.Impact,
            request.MitigationPlan, request.OwnerEmployeeId, request.IdentifiedDate);

        await repository.AddAsync(risk, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return risk.Id;
    }
}
