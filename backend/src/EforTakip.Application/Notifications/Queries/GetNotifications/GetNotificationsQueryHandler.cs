using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Common.Models;
using EforTakip.Application.Notifications.Dtos;
using EforTakip.Domain.Notifications;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.Notifications.Queries.GetNotifications;

public sealed class GetNotificationsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetNotificationsQuery, PagedResult<NotificationDto>>
{
    public async Task<PagedResult<NotificationDto>> Handle(
        GetNotificationsQuery request, CancellationToken cancellationToken)
    {
        IQueryable<Notification> query = db.Notifications.AsNoTracking().OrderByDescending(n => n.CreatedAtUtc);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ProjectToType<NotificationDto>()
            .ToListAsync(cancellationToken);

        return new PagedResult<NotificationDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
