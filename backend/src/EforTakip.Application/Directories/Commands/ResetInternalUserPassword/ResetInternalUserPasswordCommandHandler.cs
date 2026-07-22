using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Directories;
using EforTakip.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.Directories.Commands.ResetInternalUserPassword;

public sealed class ResetInternalUserPasswordCommandHandler(
    IApplicationDbContext db,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ResetInternalUserPasswordCommand>
{
    public async Task Handle(ResetInternalUserPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await db.DirectoryUsers
            .FirstOrDefaultAsync(u => u.Id == request.DirectoryUserId, cancellationToken)
            ?? throw new NotFoundException(nameof(DirectoryUser), request.DirectoryUserId);

        // Kaynak kontrolü domain'de yapılır; AD kullanıcısında SetPassword iş kuralı hatası verir.
        user.SetPassword(passwordHasher.Hash(request.NewPassword));

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
