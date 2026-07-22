using EforTakip.Application.Common.Interfaces;

namespace EforTakip.Infrastructure.Security;

public sealed class BCryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // Bozuk/eksik hash doğrulama başarısızlığıdır, çökme sebebi değil.
            return false;
        }
    }
}
