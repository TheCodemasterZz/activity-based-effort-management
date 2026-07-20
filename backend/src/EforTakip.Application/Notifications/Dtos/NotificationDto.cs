namespace EforTakip.Application.Notifications.Dtos;

public sealed class NotificationDto
{
    public Guid Id { get; init; }
    public string Message { get; init; } = default!;
    public DateTime CreatedAtUtc { get; init; }
    public bool IsRead { get; init; }
}
