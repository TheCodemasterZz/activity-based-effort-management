using EforTakip.Domain.Directories;

namespace EforTakip.Application.Directories.Dtos;

/// <summary>Bind şifresi bilinçli olarak yer almaz — dizin şifresi hiçbir yanıtta dönmez.</summary>
public sealed class DirectoryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public DirectorySource Source { get; init; }
    public string? DirectoryType { get; init; }
    public string? Hostname { get; init; }
    public int Port { get; init; }
    public bool UseSsl { get; init; }
    public string? BindUsername { get; init; }
    public string? BaseDn { get; init; }
    public string? AdditionalUserDn { get; init; }
    public string? AdditionalGroupDn { get; init; }
    public DirectoryPermission Permission { get; init; }
    public string? UserObjectClass { get; init; }
    public string? UserObjectFilter { get; init; }
    public string? UsernameAttribute { get; init; }
    public string? UsernameRdnAttribute { get; init; }
    public string? FirstNameAttribute { get; init; }
    public string? LastNameAttribute { get; init; }
    public string? DisplayNameAttribute { get; init; }
    public string? EmailAttribute { get; init; }
    public string? UniqueIdAttribute { get; init; }
    public SyncScheduleKind SyncSchedule { get; init; }
    public bool IsActive { get; init; }
    public int SortOrder { get; init; }
}
