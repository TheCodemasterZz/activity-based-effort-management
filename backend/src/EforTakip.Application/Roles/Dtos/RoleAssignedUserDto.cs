namespace EforTakip.Application.Roles.Dtos;

public sealed class RoleAssignedUserDto
{
    public Guid Id { get; init; }
    public string Username { get; init; } = default!;
    public string? DisplayName { get; init; }
}
