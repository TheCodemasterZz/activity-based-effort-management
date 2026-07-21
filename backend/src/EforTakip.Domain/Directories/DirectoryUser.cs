using EforTakip.Domain.Common;
using EforTakip.Domain.Exceptions;

namespace EforTakip.Domain.Directories;

public sealed class DirectoryUser : Entity, IAggregateRoot
{
    public Guid DirectoryId { get; private set; }
    public DirectorySource Source { get; private set; }
    public string Username { get; private set; } = default!;
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public string? DisplayName { get; private set; }
    public string? Email { get; private set; }
    public string? ObjectGuid { get; private set; }
    public string? PasswordHash { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime? LastSyncedUtc { get; private set; }

    private DirectoryUser()
    {
        // EF Core
    }

    public static DirectoryUser CreateFromActiveDirectory(
        Guid directoryId, string username, string? firstName, string? lastName,
        string? displayName, string? email, string objectGuid)
    {
        ValidateDirectoryId(directoryId);
        ValidateUsername(username);
        if (string.IsNullOrWhiteSpace(objectGuid))
            throw new BusinessRuleValidationException("AD kullanıcısının benzersiz kimliği (ObjectGuid) zorunludur.");

        return new DirectoryUser
        {
            DirectoryId = directoryId,
            Source = DirectorySource.ActiveDirectory,
            Username = username.Trim(),
            FirstName = firstName,
            LastName = lastName,
            DisplayName = displayName,
            Email = email,
            ObjectGuid = objectGuid,
            IsActive = true,
            LastSyncedUtc = DateTime.UtcNow
        };
    }

    public static DirectoryUser CreateInternal(
        Guid directoryId, string username, string? firstName, string? lastName,
        string? displayName, string? email, string passwordHash)
    {
        ValidateDirectoryId(directoryId);
        ValidateUsername(username);
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new BusinessRuleValidationException("Internal kullanıcı için şifre zorunludur.");

        return new DirectoryUser
        {
            DirectoryId = directoryId,
            Source = DirectorySource.Internal,
            Username = username.Trim(),
            FirstName = firstName,
            LastName = lastName,
            DisplayName = displayName,
            Email = email,
            PasswordHash = passwordHash,
            IsActive = true
        };
    }

    public void UpdateFromSync(
        string? firstName, string? lastName, string? displayName, string? email, DateTime syncedUtc)
    {
        FirstName = firstName;
        LastName = lastName;
        DisplayName = displayName;
        Email = email;
        LastSyncedUtc = syncedUtc;
        IsActive = true;
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    private static void ValidateDirectoryId(Guid directoryId)
    {
        if (directoryId == Guid.Empty)
            throw new BusinessRuleValidationException("Kullanıcı bir dizine bağlı olmalıdır.");
    }

    private static void ValidateUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new BusinessRuleValidationException("Kullanıcı adı boş olamaz.");
    }
}
