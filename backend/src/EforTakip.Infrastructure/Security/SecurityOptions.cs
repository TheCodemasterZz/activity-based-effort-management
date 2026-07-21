namespace EforTakip.Infrastructure.Security;

public sealed class SecurityOptions
{
    public const string SectionName = "Security";

    /// <summary>Base64 kodlu 32 baytlık AES anahtarı. Environment variable / secret üzerinden gelir.</summary>
    public string SettingsEncryptionKey { get; set; } = string.Empty;
}
