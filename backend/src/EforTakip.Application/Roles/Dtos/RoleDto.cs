namespace EforTakip.Application.Roles.Dtos;

public sealed class RoleDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public string? Description { get; init; }
    public bool IsSystemAdmin { get; init; }
    public int PermissionCount { get; init; }
}
