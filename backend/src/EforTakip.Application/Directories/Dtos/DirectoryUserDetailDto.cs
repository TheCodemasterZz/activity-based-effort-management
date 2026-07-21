using EforTakip.Domain.Directories;

namespace EforTakip.Application.Directories.Dtos;

public sealed class DirectoryUserAttributeValueDto
{
    public string SystemFieldName { get; init; } = default!;
    public string AdAttributeName { get; init; } = default!;
    public string FieldType { get; init; } = default!;
    public string? Value { get; init; }
}

/// <summary>Kullanıcı kartı için tüm senkronize attribute'larla birlikte kullanıcı bilgisi.</summary>
public sealed class DirectoryUserDetailDto
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
    public IReadOnlyCollection<DirectoryUserAttributeValueDto> Attributes { get; init; } = [];
}
