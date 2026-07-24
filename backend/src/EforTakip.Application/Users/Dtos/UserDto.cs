using EforTakip.Domain.Directories;

namespace EforTakip.Application.Users.Dtos;

/// <summary>Şifre hash'i bilinçli olarak yer almaz.</summary>
public sealed class UserDto
{
    public Guid Id { get; init; }
    public Guid DirectoryId { get; init; }
    public string DirectoryName { get; init; } = default!;
    public DirectorySource Source { get; init; }
    public string Username { get; init; } = default!;
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? DisplayName { get; init; }
    public string? Email { get; init; }
    public bool IsActive { get; init; }
    public DateTime? LastSyncedUtc { get; init; }
    public Guid? WorkCalendarId { get; init; }
    public string? WorkCalendarName { get; init; }
}
