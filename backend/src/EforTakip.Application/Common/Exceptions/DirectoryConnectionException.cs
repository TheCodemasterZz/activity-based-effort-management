namespace EforTakip.Application.Common.Exceptions;

/// <summary>
/// Dizin sunucusuna (LDAP/AD) ulaşılamadığında fırlatılır. Mesaj doğrudan kullanıcıya
/// gösterilebilir olmalıdır — iç sistem detayı (yığın izi, sunucu yanıtı) içermez.
/// </summary>
public sealed class DirectoryConnectionException(string message) : Exception(message);
