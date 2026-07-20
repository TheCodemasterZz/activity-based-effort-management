using EforTakip.Application.Notifications.Dtos;
using EforTakip.Domain.Notifications;
using Mapster;

namespace EforTakip.Application.Notifications;

public sealed class NotificationMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Notification, NotificationDto>();
    }
}
