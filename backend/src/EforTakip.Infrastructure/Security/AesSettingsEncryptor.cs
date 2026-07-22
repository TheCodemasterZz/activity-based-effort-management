using System.Security.Cryptography;
using System.Text;
using EforTakip.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace EforTakip.Infrastructure.Security;

/// <summary>
/// AES-GCM ile ayar sırlarını şifreler. Saklanan biçim: base64(nonce || tag || ciphertext).
/// GCM aynı zamanda bütünlük doğrular — kurcalanan değer çözülmeye çalışıldığında hata verir.
/// </summary>
public sealed class AesSettingsEncryptor : ISettingsEncryptor
{
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int KeySize = 32;

    private readonly byte[] _key;

    public AesSettingsEncryptor(IOptions<SecurityOptions> options)
    {
        var configuredKey = options.Value.SettingsEncryptionKey;

        if (string.IsNullOrWhiteSpace(configuredKey))
            throw new InvalidOperationException(
                "Security:SettingsEncryptionKey tanımlı değil. Dizin şifreleri şifrelenemez.");

        byte[] key;
        try
        {
            key = Convert.FromBase64String(configuredKey);
        }
        catch (FormatException)
        {
            throw new InvalidOperationException(
                "Security:SettingsEncryptionKey geçerli bir base64 değeri değil.");
        }

        if (key.Length != KeySize)
            throw new InvalidOperationException(
                $"Security:SettingsEncryptionKey {KeySize} bayt olmalıdır (base64 çözülmüş hâli).");

        _key = key;
    }

    public string Encrypt(string plainText)
    {
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

        var payload = new byte[NonceSize + TagSize + cipherBytes.Length];
        nonce.CopyTo(payload, 0);
        tag.CopyTo(payload, NonceSize);
        cipherBytes.CopyTo(payload, NonceSize + TagSize);

        return Convert.ToBase64String(payload);
    }

    public string Decrypt(string cipherText)
    {
        byte[] payload;
        try
        {
            payload = Convert.FromBase64String(cipherText);
        }
        catch (FormatException)
        {
            throw new InvalidOperationException("Saklanan dizin şifresi çözülemedi.");
        }

        if (payload.Length < NonceSize + TagSize)
            throw new InvalidOperationException("Saklanan dizin şifresi çözülemedi.");

        var nonce = payload.AsSpan(0, NonceSize);
        var tag = payload.AsSpan(NonceSize, TagSize);
        var cipherBytes = payload.AsSpan(NonceSize + TagSize);
        var plainBytes = new byte[cipherBytes.Length];

        try
        {
            using var aes = new AesGcm(_key, TagSize);
            aes.Decrypt(nonce, cipherBytes, tag, plainBytes);
        }
        catch (CryptographicException)
        {
            // Anahtar değişmiş veya değer kurcalanmış olabilir; ham kripto hatası dışarı sızmaz.
            throw new InvalidOperationException("Saklanan dizin şifresi çözülemedi.");
        }

        return Encoding.UTF8.GetString(plainBytes);
    }
}
