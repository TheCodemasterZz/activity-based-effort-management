using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.EmployeeLeaves;
using EforTakip.Domain.Exceptions;
using MediatR;

namespace EforTakip.Application.EmployeeLeaves.Commands.DeleteEmployeeLeave;

public sealed class DeleteEmployeeLeaveCommandHandler(IRepository<EmployeeLeave> repository, IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteEmployeeLeaveCommand>
{
    public async Task Handle(DeleteEmployeeLeaveCommand request, CancellationToken cancellationToken)
    {
        var leave = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(EmployeeLeave), request.Id);

        repository.Remove(leave);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
