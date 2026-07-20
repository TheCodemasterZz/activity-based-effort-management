using EforTakip.Domain.Common;
using EforTakip.Domain.Exceptions;

namespace EforTakip.Domain.Notifications;

public sealed class Notification : Entity, IAggregateRoot
{
    public string Message { get; private set; } = default!;
    public DateTime CreatedAtUtc { get; private set; }
    public bool IsRead { get; private set; }

    private Notification()
    {
        // EF Core
    }

    public static Notification Create(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new BusinessRuleValidationException("Bildirim mesajı boş olamaz.");

        return new Notification
        {
            Message = message.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
            IsRead = false
        };
    }

    public void MarkAsRead() => IsRead = true;
}
