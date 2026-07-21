namespace EforTakip.Infrastructure.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>En az 32 karakter. Environment variable / secret üzerinden gelir.</summary>
    public string SigningKey { get; set; } = string.Empty;

    public string Issuer { get; set; } = "Mesainame";

    public string Audience { get; set; } = "Mesainame";

    public int ExpiryMinutes { get; set; } = 480;
}
