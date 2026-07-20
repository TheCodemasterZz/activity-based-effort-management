using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.Projects;
using MediatR;

namespace EforTakip.Application.Projects.Commands.DeleteProject;

/// <summary>Soft delete: proje fiziksel olarak silinmez, IsActive=false yapılır ve bu sayede
/// tüm sorgulardan (liste, GetById) global query filter üzerinden otomatik hariç tutulur.</summary>
public sealed class DeleteProjectCommandHandler(IProjectRepository repository, IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteProjectCommand>
{
    public async Task Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Project), request.Id);

        project.Deactivate();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
