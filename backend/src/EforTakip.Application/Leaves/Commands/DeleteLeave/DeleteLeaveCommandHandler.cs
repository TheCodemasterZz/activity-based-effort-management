using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Leaves;
using EforTakip.Domain.Exceptions;
using MediatR;

namespace EforTakip.Application.Leaves.Commands.DeleteLeave;

public sealed class DeleteLeaveCommandHandler(IRepository<Leave> repository, IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteLeaveCommand>
{
    public async Task Handle(DeleteLeaveCommand request, CancellationToken cancellationToken)
    {
        var leave = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Leave), request.Id);

        repository.Remove(leave);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
