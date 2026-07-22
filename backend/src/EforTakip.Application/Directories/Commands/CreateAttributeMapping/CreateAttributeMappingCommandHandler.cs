using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Directories;
using MediatR;

namespace EforTakip.Application.Directories.Commands.CreateAttributeMapping;

public sealed class CreateAttributeMappingCommandHandler(
    IRepository<DirectoryAttributeMapping> repository, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateAttributeMappingCommand, Guid>
{
    public async Task<Guid> Handle(CreateAttributeMappingCommand request, CancellationToken cancellationToken)
    {
        var mapping = DirectoryAttributeMapping.Create(
            request.AdAttributeName, request.SystemFieldName, request.FieldType,
            request.IsSynced, request.SortOrder);

        await repository.AddAsync(mapping, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return mapping.Id;
    }
}
