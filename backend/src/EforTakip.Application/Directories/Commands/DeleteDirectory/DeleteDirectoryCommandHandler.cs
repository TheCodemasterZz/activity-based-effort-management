using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Exceptions;
using MediatR;
using Directory = EforTakip.Domain.Directories.Directory;

namespace EforTakip.Application.Directories.Commands.DeleteDirectory;

public sealed class DeleteDirectoryCommandHandler(IRepository<Directory> repository, IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteDirectoryCommand>
{
    public async Task Handle(DeleteDirectoryCommand request, CancellationToken cancellationToken)
    {
        var directory = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Directory), request.Id);

        repository.Remove(directory);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
