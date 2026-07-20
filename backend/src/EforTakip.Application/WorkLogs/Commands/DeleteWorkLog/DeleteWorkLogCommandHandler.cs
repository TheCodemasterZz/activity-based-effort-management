using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.WorkLogs;
using MediatR;

namespace EforTakip.Application.WorkLogs.Commands.DeleteWorkLog;

public sealed class DeleteWorkLogCommandHandler(IRepository<EmployeeWorkLog> repository, IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteWorkLogCommand>
{
    public async Task Handle(DeleteWorkLogCommand request, CancellationToken cancellationToken)
    {
        var log = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(EmployeeWorkLog), request.Id);

        log.EnsureDeletable();
        repository.Remove(log);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
