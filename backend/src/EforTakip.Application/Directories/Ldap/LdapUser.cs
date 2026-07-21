namespace EforTakip.Application.Directories.Ldap;

/// <summary>LDAP dizininden okunan tek bir kullanıcı. Attributes anahtarları AD attribute adlarıdır.</summary>
/// <param name="IsEnabled">
/// Hesabın dizinde etkin olup olmadığı. Microsoft AD'de devre dışı bırakılan hesaplar dizinde
/// kalmaya devam eder, yalnızca userAccountControl alanının ACCOUNTDISABLE biti işaretlenir.
/// </param>
public sealed record LdapUser(
    string Username,
    string? FirstName,
    string? LastName,
    string? DisplayName,
    string? Email,
    string ObjectGuid,
    bool IsEnabled,
    IReadOnlyDictionary<string, string?> Attributes);
