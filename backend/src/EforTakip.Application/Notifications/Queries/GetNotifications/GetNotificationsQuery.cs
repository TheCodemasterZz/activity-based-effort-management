using EforTakip.Application.Common.Models;
using EforTakip.Application.Notifications.Dtos;
using MediatR;

namespace EforTakip.Application.Notifications.Queries.GetNotifications;

public sealed class GetNotificationsQuery : PaginationParams, IRequest<PagedResult<NotificationDto>>
{
}
