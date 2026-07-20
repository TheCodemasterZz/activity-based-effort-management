using MediatR;

namespace EforTakip.Application.Notifications.Commands.MarkNotificationAsRead;

public sealed record MarkNotificationAsReadCommand(Guid Id) : IRequest;
