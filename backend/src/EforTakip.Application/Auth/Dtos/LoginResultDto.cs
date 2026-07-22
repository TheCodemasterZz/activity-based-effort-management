using EforTakip.Domain.Directories;

namespace EforTakip.Application.Auth.Dtos;

public sealed class LoginResultDto
{
    public string Token { get; init; } = default!;
    public DateTime ExpiresAtUtc { get; init; }
    public Guid UserId { get; init; }
    public string Username { get; init; } = default!;
    public string? DisplayName { get; init; }
    public DirectorySource Source { get; init; }
}
