using Directory = EforTakip.Domain.Directories.Directory;

namespace EforTakip.Application.Directories.Ldap;

public interface ILdapService
{
    /// <summary>Dizin ayarlarıyla bağlanmayı dener; başarısızlıkta kullanıcıya gösterilebilir bir mesaj döner.</summary>
    Task<LdapConnectionTestResult> TestConnectionAsync(Directory directory, CancellationToken cancellationToken);

    /// <summary>
    /// Dizindeki kullanıcıları arar. <paramref name="extraAttributeNames"/> senkronize edilecek
    /// ek AD attribute adlarıdır (alan eşlemelerinden gelir). <paramref name="binaryAttributeNames"/>
    /// bunlardan ikili (binary) olanlardır (ör. thumbnailPhoto) — metin gibi UTF-8 çözülmeye
    /// çalışılmaz, ham baytlar Base64'e çevrilip döner.
    /// </summary>
    Task<IReadOnlyList<LdapUser>> SearchUsersAsync(
        Directory directory,
        IReadOnlyCollection<string> extraAttributeNames,
        IReadOnlyCollection<string> binaryAttributeNames,
        CancellationToken cancellationToken);

    /// <summary>
    /// Kullanıcının dizindeki şifresini doğrular. Şifre hiçbir yerde saklanmaz veya loglanmaz.
    /// </summary>
    Task<bool> AuthenticateAsync(
        Directory directory, string username, string password, CancellationToken cancellationToken);
}
