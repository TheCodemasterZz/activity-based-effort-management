using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.Notifications;
using MediatR;

namespace EforTakip.Application.Notifications.Commands.MarkNotificationAsRead;

public sealed class MarkNotificationAsReadCommandHandler(
    IRepository<Notification> repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<MarkNotificationAsReadCommand>
{
    public async Task Handle(MarkNotificationAsReadCommand request, CancellationToken cancellationToken)
    {
        var notification = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Notification), request.Id);

        notification.MarkAsRead();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
