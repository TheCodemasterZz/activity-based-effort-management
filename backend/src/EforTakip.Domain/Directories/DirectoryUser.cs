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

    private readonly List<DirectoryUserAttribute> _attributes = [];
    public IReadOnlyCollection<DirectoryUserAttribute> Attributes => _attributes.AsReadOnly();

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
            Username = NormalizeUsername(username),
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
            Username = NormalizeUsername(username),
            FirstName = firstName,
            LastName = lastName,
            DisplayName = displayName,
            Email = email,
            PasswordHash = passwordHash,
            IsActive = true
        };
    }

    public void UpdateFromSync(
        string? firstName, string? lastName, string? displayName, string? email,
        bool isEnabled, DateTime syncedUtc)
    {
        FirstName = firstName;
        LastName = lastName;
        DisplayName = displayName;
        Email = email;
        LastSyncedUtc = syncedUtc;
        // Dizinde devre dışı bırakılan hesap sistemde de pasife alınır.
        IsActive = isEnabled;
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    /// <summary>
    /// Internal kullanıcının şifresini değiştirir. AD kullanıcılarının şifresi dizinde tutulur,
    /// sistemde saklanmaz — bu yüzden onlar için şifre atanamaz.
    /// </summary>
    public void SetPassword(string passwordHash)
    {
        if (Source != DirectorySource.Internal)
            throw new BusinessRuleValidationException(
                "Active Directory kullanıcısının şifresi sistemden değiştirilemez; şifre dizinde yönetilir.");

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new BusinessRuleValidationException("Şifre boş olamaz.");

        PasswordHash = passwordHash;
    }

    /// <summary>
    /// Attribute değerini ayarlar. Yeni oluşturulan bir attribute varsa geri döner — EF Core,
    /// içeriği boş bir koleksiyona (ör. bu senkronizasyondan önce hiç attribute'u olmayan bir
    /// kullanıcıya) eklenen yeni öğeleri DetectChanges ile her zaman fark etmeyebilir; çağıran
    /// taraf bu durumda dönen varlığı context'e açıkça eklemelidir.
    /// </summary>
    public DirectoryUserAttribute? SetAttribute(
        Guid attributeMappingId, string? value, Guid? referencedDirectoryUserId = null)
    {
        var existing = _attributes.FirstOrDefault(a => a.AttributeMappingId == attributeMappingId);
        if (existing is not null)
        {
            existing.SetValue(value, referencedDirectoryUserId);
            return null;
        }

        var created = DirectoryUserAttribute.Create(Id, attributeMappingId, value, referencedDirectoryUserId);
        _attributes.Add(created);
        return created;
    }

    public void ClearAttributes() => _attributes.Clear();

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

    /// <summary>
    /// Kullanıcı adları dizinlerde büyük/küçük harf duyarsızdır; tek biçimde saklanır.
    /// Kültüre duyarlı ToLower kullanılmaz — Türkçe kültürde 'I' harfi noktasız 'ı'ya
    /// dönüşür ve "SERKAN" ile "serkan" eşleşmez olur.
    /// </summary>
    private static string NormalizeUsername(string username) => username.Trim().ToLowerInvariant();
}
