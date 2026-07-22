using MediatR;

namespace EforTakip.Application.Settings.Commands.UpdateConfidenceScoreSettings;

public sealed record UpdateConfidenceScoreSettingsCommand(
    int WeightDescriptionLength,
    int WeightSpecificity,
    int WeightGenericPenalty,
    int WeightDuplicateDetection,
    int WeightRoundHoursSingle,
    int WeightDurationDescriptionRatio,
    int WeightDailyRoundTotal,
    int WeightDailyTotalReasonableness,
    int WeightBaselineDeviation,
    int WeightWeekendHoliday,
    int ThresholdVeryLow,
    int ThresholdLow,
    int ThresholdMedium,
    int ThresholdHigh,
    int BaselineLookbackDays,
    int DuplicateLookbackDays,
    decimal DuplicateSimilarityThreshold,
    int ShortDescriptionCharThreshold,
    int LongDescriptionCharThreshold,
    decimal LongDurationHoursThreshold,
    decimal ShortDurationHoursThreshold,
    decimal DailyTotalSuspiciousHours,
    string GenericPhrasesCsv) : IRequest;
