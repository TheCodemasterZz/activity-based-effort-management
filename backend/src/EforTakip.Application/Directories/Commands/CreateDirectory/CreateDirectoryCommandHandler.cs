using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Directories;
using MediatR;
using Directory = EforTakip.Domain.Directories.Directory;

namespace EforTakip.Application.Directories.Commands.CreateDirectory;

public sealed class CreateDirectoryCommandHandler(
    IRepository<Directory> repository, IUnitOfWork unitOfWork, ISettingsEncryptor settingsEncryptor)
    : IRequestHandler<CreateDirectoryCommand, Guid>
{
    public async Task<Guid> Handle(CreateDirectoryCommand request, CancellationToken cancellationToken)
    {
        var directory = request.Source == DirectorySource.Internal
            ? Directory.CreateInternal(request.Name, request.SortOrder)
            : Directory.CreateActiveDirectory(
                request.Name, request.DirectoryType!, request.Hostname!, request.Port, request.UseSsl,
                request.BindUsername!, EncryptBindPassword(request.BindPassword), request.BaseDn!,
                request.AdditionalUserDn, request.AdditionalGroupDn, request.Permission,
                request.UserObjectClass!, request.UserObjectFilter!, request.UsernameAttribute!,
                request.UsernameRdnAttribute!, request.FirstNameAttribute!, request.LastNameAttribute!,
                request.DisplayNameAttribute!, request.EmailAttribute!, request.UniqueIdAttribute!,
                request.SyncSchedule, request.SortOrder);

        await repository.AddAsync(directory, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return directory.Id;
    }

    private string EncryptBindPassword(string? bindPassword)
        => string.IsNullOrEmpty(bindPassword) ? string.Empty : settingsEncryptor.Encrypt(bindPassword);
}
