namespace EforTakip.Persistence.Seed;

/// <summary>Güvenilirlik skoru ayarları tek satırlık (singleton) bir yapılandırma olduğu için
/// sabit, bilinen bir Id kullanılır — GetConfidenceScoreSettingsQuery her zaman bu satırı arar.</summary>
public static class ConfidenceScoreSettingsSeedData
{
    public static readonly Guid SettingsId = new("00000000-0000-0000-0009-000000000001");
}
