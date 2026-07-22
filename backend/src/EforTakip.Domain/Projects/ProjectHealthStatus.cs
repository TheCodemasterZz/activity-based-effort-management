namespace EforTakip.Domain.Projects;

/// <summary>Proje yöneticisinin elle belirlediği genel sağlık durumu — Clarity PPM'deki
/// "ON TRACK / AT RISK / NEEDS HELP" rozetinin karşılığı. İleride (Faz 3+) SPI'dan otomatik
/// türetilebilir hale gelebilir; şu an manuel bir alan.</summary>
public enum ProjectHealthStatus
{
    OnTrack = 1,
    AtRisk = 2,
    NeedsHelp = 3
}
