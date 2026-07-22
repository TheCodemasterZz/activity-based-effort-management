namespace EforTakip.Application.Common.Exceptions;

/// <summary>
/// Kimlik doğrulama başarısız. Mesaj bilinçli olarak geneldir — kullanıcının var olup
/// olmadığı, pasif olduğu veya hangi adımda başarısız olunduğu sızdırılmaz.
/// </summary>
public sealed class AuthenticationFailedException()
    : Exception("Kullanıcı adı veya şifre hatalı.");
