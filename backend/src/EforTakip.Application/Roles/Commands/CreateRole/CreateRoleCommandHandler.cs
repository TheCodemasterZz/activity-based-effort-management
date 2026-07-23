using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.Roles;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.Roles.Commands.CreateRole;

public sealed class CreateRoleCommandHandler(
    IApplicationDbContext db, IRepository<Role> repository, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateRoleCommand, Guid>
{
    public async Task<Guid> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var nameTaken = await db.Roles.AnyAsync(r => r.Name == request.Name.Trim(), cancellationToken);
        if (nameTaken)
            throw new BusinessRuleValidationException($"'{request.Name}' adında bir rol zaten var.");

        // Sistem yöneticisi rolü yalnızca BootstrapAdminSeeder tarafından tohumlanır — API
        // üzerinden bir kullanıcının kendine sınırsız yetki tanımlamasının önüne geçilir.
        var role = Role.Create(request.Name, request.Description, isSystemAdmin: false);

        await repository.AddAsync(role, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return role.Id;
    }
}
