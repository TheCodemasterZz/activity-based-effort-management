using EforTakip.Domain.Common;
using EforTakip.Domain.Exceptions;

namespace EforTakip.Domain.Settings;

/// <summary>
/// Efor kaydı "güvenilirlik skoru" (0-100) motorunun tüm ayarlanabilir parametreleri — tek
/// satırlık (singleton) bir yapılandırma. Skorun kendisi bu kayıtlarda SAKLANMAZ; ilgili ekranlar
/// (Log Work formu, Efor Onayı) skoru bu ayarlarla birlikte GÖRÜNTÜLEME ANINDA hesaplar (bkz.
/// frontend lib/confidenceScore.ts) — böylece ağırlıklar değiştiğinde geçmiş kayıtlar için de
/// anında güncel skor gösterilir, ayrı bir yeniden hesaplama/migrasyon gerekmez.
/// </summary>
public sealed class ConfidenceScoreSettings : Entity, IAggregateRoot
{
    // Ağırlıklar (varsayılan toplamı 100, ama serbestçe değiştirilebilir — UI toplamı gösterir).
    public int WeightDescriptionLength { get; private set; } = 14; // A1
    public int WeightSpecificity { get; private set; } = 14; // A2
    public int WeightGenericPenalty { get; private set; } = 9; // A3
    public int WeightDuplicateDetection { get; private set; } = 14; // B1
    public int WeightRoundHoursSingle { get; private set; } = 7; // C1
    public int WeightDurationDescriptionRatio { get; private set; } = 11; // C2
    public int WeightDailyRoundTotal { get; private set; } = 5; // C3
    public int WeightDailyTotalReasonableness { get; private set; } = 9; // D1
    public int WeightBaselineDeviation { get; private set; } = 9; // E1
    public int WeightWeekendHoliday { get; private set; } = 8; // F1

    // 5'li skala eşikleri — skor < ThresholdVeryLow ise "Çok Düşük", ... skor >= ThresholdHigh ise "Çok Yüksek".
    public int ThresholdVeryLow { get; private set; } = 20;
    public int ThresholdLow { get; private set; } = 40;
    public int ThresholdMedium { get; private set; } = 60;
    public int ThresholdHigh { get; private set; } = 80;

    // Sinyal hesaplama parametreleri.
    public int BaselineLookbackDays { get; private set; } = 30;
    public int DuplicateLookbackDays { get; private set; } = 30;
    public decimal DuplicateSimilarityThreshold { get; private set; } = 0.85m; // 0-1 arası
    public int ShortDescriptionCharThreshold { get; private set; } = 20;
    public int LongDescriptionCharThreshold { get; private set; } = 200;
    public decimal LongDurationHoursThreshold { get; private set; } = 4m;
    public decimal ShortDurationHoursThreshold { get; private set; } = 0.5m;
    public decimal DailyTotalSuspiciousHours { get; private set; } = 12m;

    /// <summary>Virgülle ayrılmış jenerik/boilerplate ifade listesi (A3 sinyali).</summary>
    public string GenericPhrasesCsv { get; private set; } =
        "genel işler,toplantı,email,e-posta,görüşme,rutin işler,various tasks,misc,other,ofis işleri";

    private ConfidenceScoreSettings()
    {
        // EF Core
    }

    public static ConfidenceScoreSettings CreateDefault(Guid id)
        => new() { Id = id };

    public void Update(
        int weightDescriptionLength, int weightSpecificity, int weightGenericPenalty, int weightDuplicateDetection,
        int weightRoundHoursSingle, int weightDurationDescriptionRatio, int weightDailyRoundTotal,
        int weightDailyTotalReasonableness, int weightBaselineDeviation, int weightWeekendHoliday,
        int thresholdVeryLow, int thresholdLow, int thresholdMedium, int thresholdHigh,
        int baselineLookbackDays, int duplicateLookbackDays, decimal duplicateSimilarityThreshold,
        int shortDescriptionCharThreshold, int longDescriptionCharThreshold,
        decimal longDurationHoursThreshold, decimal shortDurationHoursThreshold, decimal dailyTotalSuspiciousHours,
        string genericPhrasesCsv)
    {
        if (thresholdVeryLow >= thresholdLow || thresholdLow >= thresholdMedium || thresholdMedium >= thresholdHigh)
            throw new BusinessRuleValidationException("Skala eşikleri artan sırada olmalıdır (Çok Düşük < Düşük < Orta < Yüksek).");
        if (duplicateSimilarityThreshold is < 0 or > 1)
            throw new BusinessRuleValidationException("Tekrar benzerlik eşiği 0 ile 1 arasında olmalıdır.");

        WeightDescriptionLength = weightDescriptionLength;
        WeightSpecificity = weightSpecificity;
        WeightGenericPenalty = weightGenericPenalty;
        WeightDuplicateDetection = weightDuplicateDetection;
        WeightRoundHoursSingle = weightRoundHoursSingle;
        WeightDurationDescriptionRatio = weightDurationDescriptionRatio;
        WeightDailyRoundTotal = weightDailyRoundTotal;
        WeightDailyTotalReasonableness = weightDailyTotalReasonableness;
        WeightBaselineDeviation = weightBaselineDeviation;
        WeightWeekendHoliday = weightWeekendHoliday;

        ThresholdVeryLow = thresholdVeryLow;
        ThresholdLow = thresholdLow;
        ThresholdMedium = thresholdMedium;
        ThresholdHigh = thresholdHigh;

        BaselineLookbackDays = baselineLookbackDays;
        DuplicateLookbackDays = duplicateLookbackDays;
        DuplicateSimilarityThreshold = duplicateSimilarityThreshold;
        ShortDescriptionCharThreshold = shortDescriptionCharThreshold;
        LongDescriptionCharThreshold = longDescriptionCharThreshold;
        LongDurationHoursThreshold = longDurationHoursThreshold;
        ShortDurationHoursThreshold = shortDurationHoursThreshold;
        DailyTotalSuspiciousHours = dailyTotalSuspiciousHours;
        GenericPhrasesCsv = genericPhrasesCsv;
    }
}
