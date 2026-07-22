using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.Projects;
using MediatR;

namespace EforTakip.Application.Projects.Commands.DeleteProjectTask;

/// <summary>Görevler onaya bağlı olmadığı ve tarihsel bir bütünlük taşımadığı için (Holiday
/// gibi) doğrudan (fiziksel) silinir — Project'teki gibi soft delete gerekmez.</summary>
public sealed class DeleteProjectTaskCommandHandler(IRepository<ProjectTask> repository, IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteProjectTaskCommand>
{
    public async Task Handle(DeleteProjectTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ProjectTask), request.Id);

        repository.Remove(task);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
