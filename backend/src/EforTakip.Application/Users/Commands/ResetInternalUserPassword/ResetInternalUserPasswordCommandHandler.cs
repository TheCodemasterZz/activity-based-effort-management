using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Directories;
using EforTakip.Domain.Users;
using EforTakip.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.Users.Commands.ResetInternalUserPassword;

public sealed class ResetInternalUserPasswordCommandHandler(
    IApplicationDbContext db,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ResetInternalUserPasswordCommand>
{
    public async Task Handle(ResetInternalUserPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        // Kaynak kontrolü domain'de yapılır; AD kullanıcısında SetPassword iş kuralı hatası verir.
        user.SetPassword(passwordHasher.Hash(request.NewPassword));

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
