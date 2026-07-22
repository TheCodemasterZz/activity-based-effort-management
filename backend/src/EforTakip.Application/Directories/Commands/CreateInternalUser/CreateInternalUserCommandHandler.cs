using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Directories;
using EforTakip.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Directory = EforTakip.Domain.Directories.Directory;

namespace EforTakip.Application.Directories.Commands.CreateInternalUser;

public sealed class CreateInternalUserCommandHandler(
    IApplicationDbContext db,
    IRepository<Directory> directoryRepository,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateInternalUserCommand, Guid>
{
    public async Task<Guid> Handle(CreateInternalUserCommand request, CancellationToken cancellationToken)
    {
        var directory = await directoryRepository.GetByIdAsync(request.DirectoryId, cancellationToken)
            ?? throw new NotFoundException(nameof(Directory), request.DirectoryId);

        if (directory.Source != DirectorySource.Internal)
            throw new BusinessRuleValidationException(
                "Kullanıcı yalnızca internal dizinlerde elle oluşturulabilir. AD kullanıcıları senkronizasyonla gelir.");

        // Domain kullanıcı adını invariant küçük harfe normalize eder; tekillik kontrolü de
        // aynı biçim üzerinden yapılmalı, aksi halde "Serkan" ve "serkan" birlikte kaydedilebilir.
        var username = request.Username.Trim().ToLowerInvariant();

        var usernameTaken = await db.DirectoryUsers
            .AnyAsync(u => u.Username == username, cancellationToken);

        if (usernameTaken)
            throw new BusinessRuleValidationException($"'{username}' kullanıcı adı zaten kullanılıyor.");

        var user = DirectoryUser.CreateInternal(
            directory.Id, username, request.FirstName, request.LastName,
            request.DisplayName, request.Email, passwordHasher.Hash(request.Password));

        db.DirectoryUsers.Add(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return user.Id;
    }
}
