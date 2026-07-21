using Directory = EforTakip.Domain.Directories.Directory;

namespace EforTakip.Application.Directories.Ldap;

public interface ILdapService
{
    /// <summary>Dizin ayarlarıyla bağlanmayı dener; başarısızlıkta kullanıcıya gösterilebilir bir mesaj döner.</summary>
    Task<LdapConnectionTestResult> TestConnectionAsync(Directory directory, CancellationToken cancellationToken);

    /// <summary>
    /// Dizindeki kullanıcıları arar. <paramref name="extraAttributeNames"/> senkronize edilecek
    /// ek AD attribute adlarıdır (alan eşlemelerinden gelir).
    /// </summary>
    Task<IReadOnlyList<LdapUser>> SearchUsersAsync(
        Directory directory,
        IReadOnlyCollection<string> extraAttributeNames,
        CancellationToken cancellationToken);
}
