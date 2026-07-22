using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.Projects;
using MediatR;

namespace EforTakip.Application.Projects.Commands.DeleteProjectRisk;

public sealed class DeleteProjectRiskCommandHandler(IRepository<ProjectRisk> repository, IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteProjectRiskCommand>
{
    public async Task Handle(DeleteProjectRiskCommand request, CancellationToken cancellationToken)
    {
        var risk = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ProjectRisk), request.Id);

        repository.Remove(risk);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
