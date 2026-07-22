namespace EforTakip.Application.Settings.Dtos;

public sealed class ConfidenceScoreSettingsDto
{
    public Guid Id { get; init; }

    public int WeightDescriptionLength { get; init; }
    public int WeightSpecificity { get; init; }
    public int WeightGenericPenalty { get; init; }
    public int WeightDuplicateDetection { get; init; }
    public int WeightRoundHoursSingle { get; init; }
    public int WeightDurationDescriptionRatio { get; init; }
    public int WeightDailyRoundTotal { get; init; }
    public int WeightDailyTotalReasonableness { get; init; }
    public int WeightBaselineDeviation { get; init; }
    public int WeightWeekendHoliday { get; init; }

    public int ThresholdVeryLow { get; init; }
    public int ThresholdLow { get; init; }
    public int ThresholdMedium { get; init; }
    public int ThresholdHigh { get; init; }

    public int BaselineLookbackDays { get; init; }
    public int DuplicateLookbackDays { get; init; }
    public decimal DuplicateSimilarityThreshold { get; init; }
    public int ShortDescriptionCharThreshold { get; init; }
    public int LongDescriptionCharThreshold { get; init; }
    public decimal LongDurationHoursThreshold { get; init; }
    public decimal ShortDurationHoursThreshold { get; init; }
    public decimal DailyTotalSuspiciousHours { get; init; }
    public string GenericPhrasesCsv { get; init; } = default!;
}
