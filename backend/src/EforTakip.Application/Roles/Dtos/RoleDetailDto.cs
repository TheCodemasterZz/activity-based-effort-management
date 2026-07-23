namespace EforTakip.Application.Roles.Dtos;

public sealed class RoleDetailDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public string? Description { get; init; }
    public bool IsSystemAdmin { get; init; }
    public IReadOnlyCollection<string> Permissions { get; init; } = [];
    public IReadOnlyCollection<RoleAssignedUserDto> AssignedUsers { get; init; } = [];
}
