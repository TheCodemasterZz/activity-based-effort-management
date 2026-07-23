using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.Roles;
using MediatR;

namespace EforTakip.Application.Roles.Commands.DeleteRole;

public sealed class DeleteRoleCommandHandler(IRepository<Role> repository, IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteRoleCommand>
{
    public async Task Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Role), request.Id);

        if (role.IsSystemAdmin)
            throw new BusinessRuleValidationException("Sistem yöneticisi rolü silinemez.");

        repository.Remove(role);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
