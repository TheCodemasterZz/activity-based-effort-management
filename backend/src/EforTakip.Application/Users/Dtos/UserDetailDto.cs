using EforTakip.Domain.Directories;

namespace EforTakip.Application.Users.Dtos;

public sealed class UserAttributeValueDto
{
    public string SystemFieldName { get; init; } = default!;
    public string AdAttributeName { get; init; } = default!;
    public string FieldType { get; init; } = default!;
    public string? Value { get; init; }

    /// <summary>FieldType "user" olup değer sistemde tanımlı bir kullanıcıyla eşleştiyse dolu olur.</summary>
    public Guid? ReferencedUserId { get; init; }
}

/// <summary>Kullanıcı kartı için tüm senkronize attribute'larla birlikte kullanıcı bilgisi.</summary>
public sealed class UserDetailDto
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
    public IReadOnlyCollection<UserAttributeValueDto> Attributes { get; init; } = [];
}
