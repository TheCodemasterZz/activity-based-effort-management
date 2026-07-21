using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Directories;
using EforTakip.Domain.Exceptions;
using MediatR;

namespace EforTakip.Application.Directories.Commands.UpdateAttributeMapping;

public sealed class UpdateAttributeMappingCommandHandler(
    IRepository<DirectoryAttributeMapping> repository, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateAttributeMappingCommand>
{
    public async Task Handle(UpdateAttributeMappingCommand request, CancellationToken cancellationToken)
    {
        var mapping = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(DirectoryAttributeMapping), request.Id);

        mapping.Update(request.AdAttributeName, request.SystemFieldName, request.FieldType,
            request.IsSynced, request.SortOrder);

        repository.Update(mapping);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
