using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.Roles;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.Roles.Commands.UpdateRole;

public sealed class UpdateRoleCommandHandler(
    IApplicationDbContext db, IRepository<Role> repository, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateRoleCommand>
{
    public async Task Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Role), request.Id);

        var nameTaken = await db.Roles
            .AnyAsync(r => r.Id != request.Id && r.Name == request.Name.Trim(), cancellationToken);
        if (nameTaken)
            throw new BusinessRuleValidationException($"'{request.Name}' adında bir rol zaten var.");

        role.Rename(request.Name);
        role.UpdateDescription(request.Description);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
