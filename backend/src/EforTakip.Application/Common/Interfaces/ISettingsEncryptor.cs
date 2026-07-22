namespace EforTakip.Application.Common.Interfaces;

/// <summary>Dizin bind şifresi gibi ayar sırlarını veritabanında şifreli saklamak için.</summary>
public interface ISettingsEncryptor
{
    string Encrypt(string plainText);

    string Decrypt(string cipherText);
}
