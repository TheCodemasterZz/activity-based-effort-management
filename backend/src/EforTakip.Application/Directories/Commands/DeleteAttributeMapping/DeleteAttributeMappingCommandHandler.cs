using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Directories;
using EforTakip.Domain.Exceptions;
using MediatR;

namespace EforTakip.Application.Directories.Commands.DeleteAttributeMapping;

public sealed class DeleteAttributeMappingCommandHandler(
    IRepository<DirectoryAttributeMapping> repository, IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteAttributeMappingCommand>
{
    public async Task Handle(DeleteAttributeMappingCommand request, CancellationToken cancellationToken)
    {
        var mapping = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(DirectoryAttributeMapping), request.Id);

        repository.Remove(mapping);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
