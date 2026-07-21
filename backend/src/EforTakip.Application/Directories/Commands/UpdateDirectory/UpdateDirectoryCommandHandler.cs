using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Directories;
using EforTakip.Domain.Exceptions;
using MediatR;
using Directory = EforTakip.Domain.Directories.Directory;

namespace EforTakip.Application.Directories.Commands.UpdateDirectory;

public sealed class UpdateDirectoryCommandHandler(IRepository<Directory> repository, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateDirectoryCommand>
{
    public async Task Handle(UpdateDirectoryCommand request, CancellationToken cancellationToken)
    {
        var directory = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Directory), request.Id);

        if (request.Source == DirectorySource.Internal)
        {
            directory.Rename(request.Name);
        }
        else
        {
            directory.UpdateActiveDirectorySettings(
                request.Name, request.DirectoryType!, request.Hostname!, request.Port, request.UseSsl,
                request.BindUsername!, request.BindPassword, request.BaseDn!, request.AdditionalUserDn,
                request.AdditionalGroupDn, request.Permission, request.UserObjectClass!,
                request.UserObjectFilter!, request.UsernameAttribute!, request.UsernameRdnAttribute!,
                request.FirstNameAttribute!, request.LastNameAttribute!, request.DisplayNameAttribute!,
                request.EmailAttribute!, request.UniqueIdAttribute!, request.SyncSchedule);
        }

        repository.Update(directory);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
