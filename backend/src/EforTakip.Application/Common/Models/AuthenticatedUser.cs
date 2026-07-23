using EforTakip.Domain.Directories;

namespace EforTakip.Application.Common.Models;

public sealed record AuthenticatedUser(
    Guid Id,
    string Username,
    string? DisplayName,
    Guid DirectoryId,
    DirectorySource Source,
    bool IsSystemAdmin,
    IReadOnlyCollection<string> PermissionKeys);
