using System.DirectoryServices.Protocols;
using System.Net;
using System.Text;
using EforTakip.Application.Common.Exceptions;
using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Directories.Ldap;
using Directory = EforTakip.Domain.Directories.Directory;

namespace EforTakip.Infrastructure.Ldap;

/// <summary>
/// System.DirectoryServices.Protocols üzerinden LDAP erişimi. Kütüphanenin API'si senkron
/// olduğundan çağrılar Task.Run ile arka plana alınır.
/// </summary>
public sealed class LdapService(ISettingsEncryptor settingsEncryptor) : ILdapService
{
    private const int PageSize = 500;

    /// <summary>Microsoft AD userAccountControl bayrağı: hesap devre dışı.</summary>
    private const int AccountDisabledFlag = 0x2;

    private const string UserAccountControlAttribute = "userAccountControl";

    private static readonly TimeSpan ConnectionTimeout = TimeSpan.FromSeconds(30);

    public Task<LdapConnectionTestResult> TestConnectionAsync(
        Directory directory, CancellationToken cancellationToken)
        => Task.Run(() =>
        {
            try
            {
                using var connection = CreateConnection(directory);
                connection.Bind();
                return new LdapConnectionTestResult(true, "Bağlantı başarılı.");
            }
            catch (LdapException ex)
            {
                return new LdapConnectionTestResult(false, DescribeLdapError(ex));
            }
            catch (Exception)
            {
                return new LdapConnectionTestResult(false, "Sunucuya bağlanılamadı. Ayarları kontrol edin.");
            }
        }, cancellationToken);

    public Task<IReadOnlyList<LdapUser>> SearchUsersAsync(
        Directory directory, IReadOnlyCollection<string> extraAttributeNames, CancellationToken cancellationToken)
        => Task.Run<IReadOnlyList<LdapUser>>(() =>
        {
            try
            {
                return SearchUsers(directory, extraAttributeNames, cancellationToken);
            }
            catch (LdapException ex)
            {
                // Ham LDAP hatası dışarı sızmaz; yönetici için anlamlı bir mesaja çevrilir.
                throw new DirectoryConnectionException(DescribeLdapError(ex));
            }
        }, cancellationToken);

    private List<LdapUser> SearchUsers(
        Directory directory, IReadOnlyCollection<string> extraAttributeNames, CancellationToken cancellationToken)
    {
        using var connection = CreateConnection(directory);
        connection.Bind();

        var attributesToLoad = BuildAttributeList(directory, extraAttributeNames);
        var searchBase = BuildSearchBase(directory);
        var filter = string.IsNullOrWhiteSpace(directory.UserObjectFilter)
            ? "(objectClass=*)"
            : directory.UserObjectFilter;

        var request = new SearchRequest(searchBase, filter, SearchScope.Subtree, attributesToLoad);
        var pageControl = new PageResultRequestControl(PageSize);
        request.Controls.Add(pageControl);

        var users = new List<LdapUser>();

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var response = (SearchResponse)connection.SendRequest(request);

            foreach (SearchResultEntry entry in response.Entries)
            {
                var user = MapUser(entry, directory, extraAttributeNames);
                if (user is not null)
                    users.Add(user);
            }

            var pageResponse = response.Controls
                .OfType<PageResultResponseControl>()
                .FirstOrDefault();

            if (pageResponse is null || pageResponse.Cookie.Length == 0)
                break;

            pageControl.Cookie = pageResponse.Cookie;
        }

        return users;
    }

    public Task<bool> AuthenticateAsync(
        Directory directory, string username, string password, CancellationToken cancellationToken)
        => Task.Run(() =>
        {
            // Boş şifre ile simple bind, sunucuda anonim bind'e dönüşüp "başarılı" sayılabilir.
            // Bu bir kimlik doğrulama atlatmasıdır — bind denemeden önce reddedilir.
            if (string.IsNullOrWhiteSpace(password))
                return false;

            try
            {
                var userDn = FindUserDistinguishedName(directory, username);
                if (userDn is null)
                    return false;

                var identifier = new LdapDirectoryIdentifier(
                    directory.Hostname, directory.Port, fullyQualifiedDnsHostName: false, connectionless: false);

                using var connection = new LdapConnection(
                    identifier, new NetworkCredential(userDn, password), AuthType.Basic)
                {
                    Timeout = ConnectionTimeout
                };
                connection.SessionOptions.ProtocolVersion = 3;
                connection.SessionOptions.SecureSocketLayer = directory.UseSsl;

                connection.Bind();
                return true;
            }
            catch (LdapException)
            {
                // Hatalı şifre de dahil tüm bind hataları "doğrulanamadı" demektir.
                return false;
            }
        }, cancellationToken);

    /// <summary>Servis hesabıyla bağlanıp kullanıcının DN'ini bulur.</summary>
    private string? FindUserDistinguishedName(Directory directory, string username)
    {
        using var connection = CreateConnection(directory);
        connection.Bind();

        var searchBase = BuildSearchBase(directory);
        var usernameAttribute = string.IsNullOrWhiteSpace(directory.UsernameAttribute)
            ? "sAMAccountName"
            : directory.UsernameAttribute;

        var filter = $"({usernameAttribute}={EscapeLdapFilterValue(username)})";
        var request = new SearchRequest(searchBase, filter, SearchScope.Subtree, usernameAttribute);

        var response = (SearchResponse)connection.SendRequest(request);
        if (response.Entries.Count != 1)
            return null;

        return response.Entries[0].DistinguishedName;
    }

    /// <summary>RFC 4515 — kullanıcı girdisinin LDAP filtresini bozmasını/enjeksiyonu önler.</summary>
    private static string EscapeLdapFilterValue(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            switch (c)
            {
                case '\\': builder.Append("\\5c"); break;
                case '*': builder.Append("\\2a"); break;
                case '(': builder.Append("\\28"); break;
                case ')': builder.Append("\\29"); break;
                case '\0': builder.Append("\\00"); break;
                case '/': builder.Append("\\2f"); break;
                default: builder.Append(c); break;
            }
        }
        return builder.ToString();
    }

    private LdapConnection CreateConnection(Directory directory)
    {
        var identifier = new LdapDirectoryIdentifier(
            directory.Hostname, directory.Port, fullyQualifiedDnsHostName: false, connectionless: false);

        var bindPassword = string.IsNullOrEmpty(directory.BindPasswordEncrypted)
            ? string.Empty
            : settingsEncryptor.Decrypt(directory.BindPasswordEncrypted);

        var credential = new NetworkCredential(directory.BindUsername, bindPassword);

        // AuthType.Basic = simple bind. Microsoft AD'ye simple bind ile bağlanırken şifre,
        // SSL kapalıysa ağ üzerinde düz metin gider — üretimde LDAPS (636) kullanılmalıdır.
        var connection = new LdapConnection(identifier, credential, AuthType.Basic)
        {
            Timeout = ConnectionTimeout
        };
        connection.SessionOptions.ProtocolVersion = 3;
        connection.SessionOptions.SecureSocketLayer = directory.UseSsl;
        return connection;
    }

    private static string BuildSearchBase(Directory directory)
        => string.IsNullOrWhiteSpace(directory.AdditionalUserDn)
            ? directory.BaseDn ?? string.Empty
            : $"{directory.AdditionalUserDn},{directory.BaseDn}";

    private static string[] BuildAttributeList(Directory directory, IReadOnlyCollection<string> extraAttributeNames)
    {
        var names = new List<string?>
        {
            directory.UsernameAttribute,
            directory.FirstNameAttribute,
            directory.LastNameAttribute,
            directory.DisplayNameAttribute,
            directory.EmailAttribute,
            directory.UniqueIdAttribute,
            UserAccountControlAttribute
        };
        names.AddRange(extraAttributeNames);

        return names
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static LdapUser? MapUser(
        SearchResultEntry entry, Directory directory, IReadOnlyCollection<string> extraAttributeNames)
    {
        var username = ReadString(entry, directory.UsernameAttribute);
        var objectGuid = ReadGuid(entry, directory.UniqueIdAttribute);

        // Kullanıcı adı veya benzersiz kimliği olmayan kayıtlar (ör. konteyner nesneleri) atlanır.
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(objectGuid))
            return null;

        var attributes = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in extraAttributeNames)
            attributes[name] = ReadString(entry, name);

        return new LdapUser(
            username,
            ReadString(entry, directory.FirstNameAttribute),
            ReadString(entry, directory.LastNameAttribute),
            ReadString(entry, directory.DisplayNameAttribute),
            ReadString(entry, directory.EmailAttribute),
            objectGuid,
            ReadIsEnabled(entry),
            attributes);
    }

    /// <summary>
    /// Microsoft AD'de devre dışı bırakılan hesap dizinden silinmez; userAccountControl alanının
    /// ACCOUNTDISABLE biti işaretlenir. Alan okunamazsa hesap etkin kabul edilir.
    /// </summary>
    private static bool ReadIsEnabled(SearchResultEntry entry)
    {
        var raw = ReadString(entry, UserAccountControlAttribute);
        if (!int.TryParse(raw, out var flags))
            return true;

        return (flags & AccountDisabledFlag) == 0;
    }

    private static string? ReadString(SearchResultEntry entry, string? attributeName)
    {
        var raw = ReadRaw(entry, attributeName);
        return raw switch
        {
            null => null,
            string s => s,
            byte[] bytes => Encoding.UTF8.GetString(bytes),
            _ => raw.ToString()
        };
    }

    private static string? ReadGuid(SearchResultEntry entry, string? attributeName)
    {
        var raw = ReadRaw(entry, attributeName);
        return raw switch
        {
            null => null,
            byte[] { Length: 16 } bytes => new Guid(bytes).ToString(),
            byte[] bytes => Convert.ToHexString(bytes),
            string s => s,
            _ => raw.ToString()
        };
    }

    private static object? ReadRaw(SearchResultEntry entry, string? attributeName)
    {
        if (string.IsNullOrWhiteSpace(attributeName) || !entry.Attributes.Contains(attributeName))
            return null;

        var attribute = entry.Attributes[attributeName];
        return attribute is null || attribute.Count == 0 ? null : attribute[0];
    }

    /// <summary>İç sistem detayı sızdırmadan, yöneticinin ayarı düzeltmesine yarayan mesaj üretir.</summary>
    private static string DescribeLdapError(LdapException ex) => ex.ErrorCode switch
    {
        49 => "Kullanıcı adı veya şifre hatalı.",
        81 => "Sunucuya ulaşılamıyor. Adres ve port bilgisini kontrol edin.",
        _ => "Bağlantı kurulamadı. Sunucu ayarlarını kontrol edin."
    };
}
